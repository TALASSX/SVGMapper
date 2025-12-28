import React, { useRef, useState, useEffect, useCallback } from 'react'
import UndoRedo from '../utils/undoRedo'

type Point = { x: number; y: number }

type Room = { id: string; name: string; points: Point[] }

export default function ImageCanvas() {
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const [imgSrc, setImgSrc] = useState<string | null>(null)
  const [imgSize, setImgSize] = useState<{ w: number; h: number } | null>(null)
  const [rooms, setRooms] = useState<Room[]>([])
  const [current, setCurrent] = useState<Point[]>([])
  const svgRef = useRef<SVGSVGElement | null>(null)
  const imgRef = useRef<HTMLImageElement | null>(null)
  const [startMarker, setStartMarker] = useState<Point | null>(null)
  const [startRegistered, setStartRegistered] = useState<boolean>(false)

  // extended state: grid, snapping, undo/redo, selection and preview
  const [gridVisible, setGridVisible] = useState<boolean>(true)
  const [gridSize, setGridSize] = useState<number>(40)
  const [snapToGrid, setSnapToGrid] = useState<boolean>(true)
  const [mousePos, setMousePos] = useState<Point | null>(null)
  const [selectedRoom, setSelectedRoom] = useState<number | null>(null)
  const draggingRef = useRef<{ polyIdx: number; vertexIdx: number } | null>(null)
  const undoRef = useRef<UndoRedo>(new UndoRedo())

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        // If drawing and start marker registered, cancel via undo entry; otherwise clear draft
        if (current.length > 0 && startRegistered) {
          undoRef.current.undoOne()
        } else {
          setCurrent([])
        }
        setSelectedRoom(null)
      }
      // Ctrl+Z undo: when drawing, revoke last draft point; otherwise undo stack
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'z') {
        if (current.length > 0) {
          revokeDraftPoint()
        } else {
          undoRef.current.undoOne()
        }
      }
      // Ctrl+Y redo
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'y') {
        undoRef.current.redoOne()
      }
      // Backspace removes last draft point when drawing
      if (e.key === 'Backspace') {
        if (current.length > 0) {
          revokeDraftPoint()
          e.preventDefault()
        }
      }
      // Delete: remove selected room
      if (e.key === 'Delete') {
        if (selectedRoom != null) {
          const idx = selectedRoom
          const before = rooms
          undoRef.current.execute(() => setRooms(prev => prev.filter((_, i) => i !== idx)), () => setRooms(before))
          setSelectedRoom(null)
        }
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [rooms, selectedRoom, current, startRegistered])

  const onFile = (file?: File) => {
    const f = file || fileInputRef.current?.files?.[0]
    if (!f) return
    const reader = new FileReader()
    reader.onload = () => {
      setImgSrc(reader.result as string)
    }
    reader.readAsDataURL(f)
  }

  const onImageLoad = () => {
    if (!imgRef.current) return
    setImgSize({ w: imgRef.current.naturalWidth, h: imgRef.current.naturalHeight })
  }

  const distancePointToSegment = (px: number, py: number, x1: number, y1: number, x2: number, y2: number) => {
    const dx = x2 - x1; const dy = y2 - y1;
    if (dx === 0 && dy === 0) return Math.hypot(px - x1, py - y1);
    let t = ((px - x1) * dx + (py - y1) * dy) / (dx*dx + dy*dy);
    t = Math.max(0, Math.min(1, t));
    const projx = x1 + t*dx; const projy = y1 + t*dy;
    return Math.hypot(px - projx, py - projy);
  }

  // map client mouse to image pixel coords
  const clientToImagePoint = (clientX: number, clientY: number, doSnap = true): Point | null => {
    if (!imgRef.current) return null
    const rect = imgRef.current.getBoundingClientRect()
    const sx = (clientX - rect.left) / rect.width
    const sy = (clientY - rect.top) / rect.height
    // clamp
    const nx = Math.max(0, Math.min(1, sx))
    const ny = Math.max(0, Math.min(1, sy))
    if (!imgSize) return null
    let pt = { x: nx * imgSize.w, y: ny * imgSize.h }
    if (doSnap && snapToGrid) {
      pt = { x: Math.round(pt.x / gridSize) * gridSize, y: Math.round(pt.y / gridSize) * gridSize }
    }
    return pt
  }

  const onSvgClick = (e: React.MouseEvent) => {
    if (!imgSize) return
    const p = clientToImagePoint(e.clientX, e.clientY, true)
    if (!p) return

    // If this is the first point, register a start marker and an undo entry so Escape can cancel it
    if (current.length === 0) {
      const pointCopy = { x: p.x, y: p.y }
      undoRef.current.execute(
        () => { setCurrent(prev => [...prev, pointCopy]); setStartMarker(pointCopy); setStartRegistered(true); },
        () => { setCurrent([]); setStartMarker(null); setStartRegistered(false); }
      )
    } else {
      setCurrent(prev => [...prev, p])
    }
  }

  const onSvgMouseMove = (e: React.MouseEvent) => {
    const p = clientToImagePoint(e.clientX, e.clientY, snapToGrid)
    setMousePos(p)
  }

  const dragOriginalPolyRef = useRef<Point[] | null>(null)

  const revokeDraftPoint = () => {
    if (current.length === 0) return
    const newCurrent = current.slice(0, -1)
    setCurrent(newCurrent)
    if (newCurrent.length === 0 && startRegistered) {
      // undo the start marker registration (undoAction clears startMarker and current)
      undoRef.current.undoOne()
    }
  }

  const onSvgDoubleClick = (e: React.MouseEvent) => {
    // finish polygon on double-click (if current has >=3)
    if (current.length >= 3) {
      finishPolygon()
    }
  }

  // vertex drag helpers
  const startVertexDrag = (polyIdx: number, vertexIdx: number) => {
    draggingRef.current = { polyIdx, vertexIdx }
    dragOriginalPolyRef.current = rooms[polyIdx].points.map(p => ({ x: p.x, y: p.y }))
    setSelectedRoom(polyIdx)
  }

  useEffect(() => {
    const onDocMove = (ev: MouseEvent) => {
      if (!draggingRef.current) return
      const p = clientToImagePoint(ev.clientX, ev.clientY, snapToGrid)
      if (!p) return
      const { polyIdx, vertexIdx } = draggingRef.current
      setRooms(prev => prev.map((room, i) => i === polyIdx ? { ...room, points: room.points.map((pt, j) => j === vertexIdx ? p : pt) } : room))
    }
    const onDocUp = (ev: MouseEvent) => {
      if (!draggingRef.current) return
      const { polyIdx, vertexIdx } = draggingRef.current
      const before = dragOriginalPolyRef.current ? dragOriginalPolyRef.current : rooms[polyIdx].points
      const after = rooms[polyIdx].points.map(p => ({ x: p.x, y: p.y }))
      undoRef.current.execute(() => setRooms(prev => prev.map((room, i) => i === polyIdx ? { ...room, points: after } : room)), () => setRooms(prev => prev.map((room, i) => i === polyIdx ? { ...room, points: before } : room)))
      draggingRef.current = null
      dragOriginalPolyRef.current = null
    }
    document.addEventListener('mousemove', onDocMove)
    document.addEventListener('mouseup', onDocUp)
    return () => {
      document.removeEventListener('mousemove', onDocMove)
      document.removeEventListener('mouseup', onDocUp)
    }
  }, [rooms, snapToGrid])

  const finishPolygon = () => {
    if (current.length >= 3) {
      const name = window.prompt('Enter room name:', 'Room') || 'Room'
      const room: Room = { id: String(Date.now()) + Math.random().toString(36).slice(2,8), name, points: current.map(p => ({ x: p.x, y: p.y })) }
      const before = rooms
      undoRef.current.execute(() => { setRooms(prev => [...prev, room]); setCurrent([]); setStartMarker(null); setStartRegistered(false); }, () => setRooms(before))
    }
  }

  const exportSvg = () => {
    if (!imgSrc || !imgSize) return
    const svgNS = 'http://www.w3.org/2000/svg'
    const svg = document.createElementNS(svgNS, 'svg')
    svg.setAttribute('xmlns', svgNS)
    svg.setAttribute('viewBox', `0 0 ${imgSize.w} ${imgSize.h}`)
    svg.setAttribute('width', String(imgSize.w))
    svg.setAttribute('height', String(imgSize.h))

    const imageEl = document.createElementNS(svgNS, 'image')
    imageEl.setAttribute('href', imgSrc)
    imageEl.setAttribute('x', '0')
    imageEl.setAttribute('y', '0')
    imageEl.setAttribute('width', String(imgSize.w))
    imageEl.setAttribute('height', String(imgSize.h))
    svg.appendChild(imageEl)

    rooms.forEach((room, idx) => {
      const p = document.createElementNS(svgNS, 'polygon')
      // use hex fill + opacity attributes for broader SVG compatibility
      p.setAttribute('fill', '#D3D3D3')
      p.setAttribute('fill-opacity', '0.5')
      p.setAttribute('stroke', 'black')
      p.setAttribute('stroke-width', '2')
      p.setAttribute('points', room.points.map(pt => `${pt.x},${pt.y}`).join(' '))
      // label
      if (room.name) {
        const cx = Math.round(room.points.reduce((s,p)=>s+p.x,0)/room.points.length)
        const cy = Math.round(room.points.reduce((s,p)=>s+p.y,0)/room.points.length)
        const t = document.createElementNS(svgNS, 'text')
        t.setAttribute('x', String(cx))
        t.setAttribute('y', String(cy))
        t.setAttribute('font-size', '14')
        t.setAttribute('fill', 'black')
        t.setAttribute('text-anchor', 'middle')
        t.textContent = room.name
        svg.appendChild(t)
      }
      svg.appendChild(p)
    })

    // current polygon (if any)
    if (current.length > 0) {
      const p = document.createElementNS(svgNS, 'polygon')
      p.setAttribute('fill', '#FFA500')
      p.setAttribute('fill-opacity', '0.4')
      p.setAttribute('stroke', 'orange')
      p.setAttribute('stroke-width', '2')
      p.setAttribute('points', current.map(pt => `${pt.x},${pt.y}`).join(' '))
      svg.appendChild(p)
    }

    const s = new XMLSerializer().serializeToString(svg)
    const blob = new Blob([s], { type: 'image/svg+xml' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'export.svg'
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="image-canvas-root">
      <div className="toolbar">
        <input ref={fileInputRef} type="file" accept="image/*" style={{ display: 'none' }} onChange={() => onFile()} />
        <button onClick={() => fileInputRef.current?.click()}>Load Image</button>
        <button onClick={finishPolygon}>Finish Polygon</button>
        <button onClick={() => { const before = rooms; undoRef.current.execute(() => setRooms([]), () => setRooms(before)) }}>Clear Rooms</button>
        <button onClick={exportSvg} disabled={!imgSrc}>Export SVG</button>
        <button onClick={() => undoRef.current.undoOne()}>Undo</button>
        <button onClick={() => undoRef.current.redoOne()}>Redo</button>
        <label style={{ marginLeft: 8 }}>
          <input type="checkbox" checked={gridVisible} onChange={(e) => setGridVisible(e.target.checked)} /> Show Grid
        </label>
        <label style={{ marginLeft: 8 }}>
          Grid size: <input type="number" value={gridSize} onChange={(e) => setGridSize(Math.max(1, Number(e.target.value) || 1))} style={{ width: 60 }} />
        </label>
        <label style={{ marginLeft: 8 }}>
          <input type="checkbox" checked={snapToGrid} onChange={(e) => setSnapToGrid(e.target.checked)} /> Snap to grid
        </label>

        <div style={{ marginTop: 8 }}>
          <b>Rooms</b>
          <div style={{ maxHeight: 200, overflowY: 'auto' }}>
            {rooms.map((r, i) => (
              <div key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
                <button onClick={() => setSelectedRoom(i)} style={{ background: selectedRoom === i ? 'orange' : '#eee' }}>{i + 1}</button>
                <span style={{ flex: 1 }}>{r.name}</span>
                <button onClick={() => {
                  const name = window.prompt('Rename room:', r.name) || r.name
                  const before = rooms
                  const newRooms = rooms.map((rr, ii) => ii === i ? { ...rr, name } : rr)
                  undoRef.current.execute(() => setRooms(newRooms), () => setRooms(before))
                }}>Rename</button>
                <button onClick={() => {
                  const before = rooms
                  undoRef.current.execute(() => setRooms(prev => prev.filter((_, ii) => ii !== i)), () => setRooms(before))
                }}>Delete</button>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="viewer">
        <div className="image-wrapper">
          {imgSrc ? (
            <>
              <img ref={imgRef} src={imgSrc} alt="background" onLoad={onImageLoad} draggable={false} />
              {imgSize && (
                <svg
                  ref={svgRef}
                  className="overlay"
                  viewBox={`0 0 ${imgSize.w} ${imgSize.h}`}
                  preserveAspectRatio="xMidYMid meet"
                  onClick={onSvgClick}
                  onMouseMove={onSvgMouseMove}
                  onDoubleClick={onSvgDoubleClick}
                >
                  {/* start marker */}
                  {startMarker && (
                    <circle cx={startMarker.x} cy={startMarker.y} r={6} fill="white" stroke="darkred" strokeWidth={2} />
                  )}
                  {/* grid */}
                  {gridVisible && imgSize && (
                    <g>
                      {Array.from({ length: Math.ceil(imgSize.w / gridSize) + 1 }).map((_, i) => (
                        <line key={`gv-${i}`} x1={i * gridSize} y1={0} x2={i * gridSize} y2={imgSize.h} stroke="lightgray" strokeWidth={1} />
                      ))}
                      {Array.from({ length: Math.ceil(imgSize.h / gridSize) + 1 }).map((_, i) => (
                        <line key={`gh-${i}`} x1={0} y1={i * gridSize} x2={imgSize.w} y2={i * gridSize} stroke="lightgray" strokeWidth={1} />
                      ))}
                    </g>
                  )}

                  {/* existing polygons */}
                  {rooms.map((room, i) => (
                    <polygon
                      key={room.id}
                      points={room.points.map(pt => `${pt.x},${pt.y}`).join(' ')}
                      fill={selectedRoom === i ? 'rgba(255,165,0,0.4)' : 'rgba(211,211,211,0.5)'}
                      stroke={selectedRoom === i ? 'orange' : 'black'}
                      strokeWidth={selectedRoom === i ? 3 : 2}
                      onClick={(e) => { e.stopPropagation(); setSelectedRoom(i); }}
                      onDoubleClick={(e) => { e.stopPropagation(); /* insert vertex on nearest segment */
                        if (!imgSize) return;
                        const clientX = (e as any).clientX; const clientY = (e as any).clientY;
                        const p = clientToImagePoint(clientX, clientY, true);
                        if (!p) return;
                        // find closest segment
                        let bestIdx = -1; let bestDist = Infinity;
                        for (let j=0;j<room.points.length;j++){
                          const a = room.points[j]; const b = room.points[(j+1)%room.points.length];
                          const dist = distancePointToSegment(p.x, p.y, a.x, a.y, b.x, b.y);
                          if (dist < bestDist) { bestDist = dist; bestIdx = j; }
                        }
                        if (bestIdx >= 0) {
                          const insertIdx = bestIdx + 1;
                          const newRooms = rooms.map((rr, ii) => ii===i ? { ...rr, points: [...rr.points.slice(0, insertIdx), p, ...rr.points.slice(insertIdx)] } : rr);
                          const before = rooms;
                          undoRef.current.execute(() => setRooms(newRooms), () => setRooms(before));
                          setSelectedRoom(i);
                        }
                      }}
                    />
                  ))}

                  {/* current polygon */}
                  {current.length > 0 && (
                    <polygon points={current.map(pt => `${pt.x},${pt.y}`).join(' ')} fill="rgba(255,165,0,0.4)" stroke="orange" strokeWidth={2} />
                  )}

                  {/* dashed preview towards mouse */}
                  {current.length > 0 && mousePos && (
                    <polyline points={[...current, mousePos].map(pt => `${pt.x},${pt.y}`).join(' ')} fill="none" stroke="red" strokeWidth={2} strokeDasharray="4 2" />
                  )}

                  {/* helper: show points */}
                  {current.map((pt, i) => (
                    <circle key={i} cx={pt.x} cy={pt.y} r={6} fill="white" stroke="red" strokeWidth={2} />
                  ))}

                  {/* vertex handles for selected room */}
                  {selectedRoom != null && rooms[selectedRoom] && rooms[selectedRoom].points.map((pt, idx) => (
                    <circle
                      key={`v-${idx}`}
                      cx={pt.x}
                      cy={pt.y}
                      r={6}
                      fill={draggingRef.current && draggingRef.current.polyIdx === selectedRoom && draggingRef.current.vertexIdx === idx ? 'orange' : 'white'}
                      stroke="black"
                      strokeWidth={1}
                      onMouseDown={(e) => { e.stopPropagation(); startVertexDrag(selectedRoom, idx) }}
                      onContextMenu={(e) => { e.preventDefault(); e.stopPropagation(); // remove vertex
                        if (rooms[selectedRoom].points.length <= 3) { alert('Polygon must have at least 3 points'); return }
                        const before = rooms
                        const newRooms = rooms.map((room, i) => i === selectedRoom ? { ...room, points: room.points.filter((_, j) => j !== idx) } : room)
                        undoRef.current.execute(() => setRooms(newRooms), () => setRooms(before))
                      }}
                    />
                  ))}
                </svg>
              )}
            </>
          ) : (
            <div className="placeholder">No image loaded</div>
          )}
        </div>
      </div>
    </div>
  )
}
