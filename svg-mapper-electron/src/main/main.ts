import { app, BrowserWindow } from 'electron'
import * as path from 'path'

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, '../preload/preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  })

  // During development, load the Vite dev server. Fall back to localhost:5173.
  if (process.env.VITE_DEV_SERVER_URL || process.env.NODE_ENV !== 'production') {
    const devUrl = process.env.VITE_DEV_SERVER_URL || 'http://localhost:5173'
    win.loadURL(devUrl)
    win.webContents.openDevTools()
  } else {
    // in production, load the built index.html
    win.loadFile(path.join(__dirname, '../../renderer/index.html'))
  }
}

app.whenReady().then(() => {
  createWindow()
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})
