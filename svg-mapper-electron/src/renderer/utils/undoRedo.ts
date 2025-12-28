type Entry = { doAction: () => void; undoAction: () => void }

export default class UndoRedo {
  private undoStack: Entry[] = []
  private redoStack: Entry[] = []

  execute(doAction: () => void, undoAction: () => void) {
    doAction()
    this.undoStack.push({ doAction, undoAction })
    this.redoStack = []
  }

  undoOne() {
    if (this.undoStack.length === 0) return
    const entry = this.undoStack.pop()!
    try {
      entry.undoAction()
      this.redoStack.push(entry)
    } catch (e) {
      // swallow errors
    }
  }

  redoOne() {
    if (this.redoStack.length === 0) return
    const entry = this.redoStack.pop()!
    try {
      entry.doAction()
      this.undoStack.push(entry)
    } catch (e) {
      // swallow
    }
  }

  clear() {
    this.undoStack = []
    this.redoStack = []
  }
}
