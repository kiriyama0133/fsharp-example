module WindowTypes =

    open System

    type WindowId = WindowId of int
    type PointerId = PointerId of int

    type EventContext =
        { WindowId: WindowId
          Timestamp: int64
          mutable Handled: bool
          mutable Cancel: bool }

    type WindowState =
        | Created
        | Visible
        | Hidden
        | Closing
        | Closed

    type FocusState =
        | Focused
        | Unfocused

    type ActivationState =
        | Active
        | Inactive

    type PointerButton =
        | Left
        | Right
        | Middle
        | X1
        | X2

    type WindowBounds =
        { X: float
          Y: float
          Width: float
          Height: float }

    type WindowStyle =
        { Resizable: bool
          Borderless: bool
          Topmost: bool
          Transparent: bool
          AcceptsInput: bool }

    type PointerPosition = { X: float; Y: float }

    type PointerDelta = { X: float; Y: float }

    type PointerState =
        { Position: PointerPosition
          IsInside: bool
          CapturedBy: WindowId option }

    type WindowEvent =
        | Shown
        | Hidden
        | CloseRequested
        | Closed
        | Activated
        | Deactivated
        | FocusGained
        | FocusLost
        | TitleChanged of string
        | StyleChanged of WindowStyle
        | Moved of WindowBounds
        | Resized of WindowBounds

    type PointerEventKind =
        | Entered
        | Exited
        | Moved
        | Pressed
        | Released
        | Wheel

    type KeyboardEventKind =
        | KeyDown
        | KeyUp
        | TextInput

    type PointerEvent =
        { Context: EventContext
          PointerId: PointerId
          Position: PointerPosition
          Button: PointerButton option
          Delta: PointerDelta option }

    type KeyboardEvent =
        { Context: EventContext
          Key: string option
          Text: string option
          Ctrl: bool
          Shift: bool
          Alt: bool }

    type WindowMessage =
        | Window of EventContext * WindowEvent
        | Pointer of PointerEventKind * PointerEvent
        | Keyboard of KeyboardEventKind * KeyboardEvent

    let createEventContext windowId : EventContext =
        { WindowId = windowId
          Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
          Handled = false
          Cancel = false }

    let defaultWindowStyle: WindowStyle =
        { Resizable = true
          Borderless = false
          Topmost = false
          Transparent = false
          AcceptsInput = true }

    let defaultPointerPosition: PointerPosition = { X = 0.0; Y = 0.0 }

    let defaultPointerState: PointerState =
        { Position = defaultPointerPosition
          IsInside = false
          CapturedBy = None }

    [<Literal>]
    let WS_OVERLAPPED = 0x00000000u

    [<Literal>]
    let WS_CAPTION = 0x00C00000u

    [<Literal>]
    let WS_SYSMENU = 0x00080000u

    [<Literal>]
    let WS_THICKFRAME = 0x00040000u

    [<Literal>]
    let WS_MINIMIZEBOX = 0x00020000u

    [<Literal>]
    let WS_MAXIMIZEBOX = 0x00010000u
