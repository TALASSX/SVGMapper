import { contextBridge } from 'electron'

// Expose safe APIs here in future if needed
contextBridge.exposeInMainWorld('electron', {
  // placeholder
})
