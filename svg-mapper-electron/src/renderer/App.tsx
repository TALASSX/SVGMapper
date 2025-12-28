import React from 'react'
import ImageCanvas from './components/ImageCanvas'

export default function App() {
  return (
    <div className="app">
      <header>
        <h1>SVG Mapper (Electron)</h1>
      </header>
      <main>
        <ImageCanvas />
      </main>
    </div>
  )
}
