module Win32Platform

open System
open System.ComponentModel
open System.Collections.Generic
    open Win32Types
    open WindowTypes
    open Window
    open Win32Native
    open System.Runtime.InteropServices


    let private windowIds = Dictionary<HWND, WindowId>()
    let private windows = Dictionary<HWND, Window.Window>()
    let private nextWindowId = ref 0
    let private nullWindow: HWND = Win32Native.nullHandle
    let private mousePointerId = PointerId 0

    let private ensureNonZeroHandle handle apiName =
        if handle = nullHandle then
            raise (Win32Exception(Marshal.GetLastWin32Error(), apiName + " failed."))

    let private allocateWindowId () =
        let id = nextWindowId.Value
        nextWindowId.Value <- id + 1
        WindowId id

    let private registerWindow hwnd (windowId: WindowId) (window: Window.Window) =
        windowIds.[hwnd] <- windowId
        windows.[hwnd] <- window
        windowId

    let private tryGetWindowId hwnd =
        match windowIds.TryGetValue hwnd with
        | true, windowId -> Some windowId
        | false, _ -> None

    let private unregisterWindow hwnd =
        windowIds.Remove(hwnd) |> ignore
        windows.Remove(hwnd) |> ignore

    let private tryGetWindow hwnd =
        match windows.TryGetValue hwnd with
        | true, window -> Some window
        | false, _ -> None

    let private requireWindow hwnd apiName =
        match tryGetWindow hwnd with
        | Some window -> window
        | None -> invalidOp (apiName + " requires a registered window handle.")

    let private ensureWindow (handle: HWND) apiName =
        if handle = nullWindow then
            raise (Win32Exception(Marshal.GetLastWin32Error(), apiName + " failed."))

    let private ensureTrue result apiName =
        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), apiName + " failed."))

    let private getModifierState () =
        let isPressed virtualKey = (int (GetKeyState virtualKey) &&& 0x8000) <> 0

        isPressed VK_CONTROL, isPressed VK_SHIFT, isPressed VK_MENU

    let private createEventContextForHandle hwnd =
        match tryGetWindowId hwnd with
        | Some windowId -> createEventContext windowId
        | None -> invalidOp "The window is not registered."

    let private toPointerPosition x y : PointerPosition =
        { X = float x
          Y = float y }

    let private getPointerPositionFromClientLParam lParam =
        toPointerPosition (getXLParam lParam) (getYLParam lParam)

    let private getPointerPositionFromScreenLParam hwnd lParam =
        let mutable point = POINT(getXLParam lParam, getYLParam lParam)
        ensureTrue (ScreenToClient(hwnd, &point)) "ScreenToClient"
        toPointerPosition point.X point.Y

    let private getNativeWindowBounds hwnd =
        let mutable rect = Unchecked.defaultof<RECT>
        ensureTrue (GetWindowRect(hwnd, &rect)) "GetWindowRect"

        { X = float rect.Left
          Y = float rect.Top
          Width = float (rect.Right - rect.Left)
          Height = float (rect.Bottom - rect.Top) }

    let private computeWindowStyle (style: WindowStyle) =
        let frameStyle =
            if style.Borderless then
                WS_POPUP
            else
                WS_OVERLAPPED ||| WS_CAPTION ||| WS_SYSMENU

        let resizableStyle =
            if style.Resizable && not style.Borderless then
                WS_THICKFRAME ||| WS_MINIMIZEBOX ||| WS_MAXIMIZEBOX
            else
                0u

        let inputStyle =
            if style.AcceptsInput then
                0u
            else
                WS_DISABLED

        frameStyle ||| resizableStyle ||| inputStyle

    let private computeExtendedWindowStyle (style: WindowStyle) =
        let topmostStyle =
            if style.Topmost then
                WS_EX_TOPMOST
            else
                0u

        let transparentStyle =
            if style.Transparent then
                WS_EX_TRANSPARENT
            else
                0u

        topmostStyle ||| transparentStyle

    let private dispatchWindowMessage (hwnd: HWND) (message: WindowMessage) =
        match message with
        | Window(context, eventValue) ->
            match tryGetWindow hwnd with
            | None -> None
            | Some window ->
                match eventValue with
                | Shown ->
                    window.Show()
                    Some context
                | Hidden ->
                    window.Hide()
                    Some context
                | CloseRequested ->
                    let closeContext = window.RequestClose()
                    Some closeContext
                | Closed ->
                    window.Close()
                    Some context
                | Activated ->
                    window.Activate()
                    Some context
                | Deactivated ->
                    window.Deactivate()
                    Some context
                | FocusGained ->
                    window.SetFocused()
                    Some context
                | FocusLost ->
                    window.SetUnfocused()
                    Some context
                | TitleChanged title ->
                    window.SetTitle title
                    Some context
                | StyleChanged style ->
                    window.SetStyle style
                    Some context
                | WindowEvent.Moved bounds
                | WindowEvent.Resized bounds ->
                    window.SetBounds bounds
                    Some context
        | Pointer(kind, eventValue) ->
            match tryGetWindow hwnd with
            | None -> None
            | Some window ->
                match kind with
                | PointerEventKind.Entered ->
                    window.EnterPointer(eventValue.PointerId, eventValue.Position)
                | PointerEventKind.Exited ->
                    window.LeavePointer(eventValue.PointerId, eventValue.Position)
                | PointerEventKind.Moved ->
                    window.MovePointer(eventValue.PointerId, eventValue.Position)
                | PointerEventKind.Pressed ->
                    match eventValue.Button with
                    | Some button -> window.PressPointer(eventValue.PointerId, button, eventValue.Position)
                    | None -> ()
                | PointerEventKind.Released ->
                    match eventValue.Button with
                    | Some button -> window.ReleasePointer(eventValue.PointerId, button, eventValue.Position)
                    | None -> ()
                | PointerEventKind.Wheel ->
                    match eventValue.Delta with
                    | Some delta -> window.ScrollPointer(eventValue.PointerId, eventValue.Position, delta)
                    | None -> ()

                Some eventValue.Context
        | Keyboard(kind, eventValue) ->
            match tryGetWindow hwnd with
            | None -> None
            | Some window ->
                match kind with
                | KeyDown ->
                    match eventValue.Key with
                    | Some key -> window.RaiseKeyDown(key, eventValue.Ctrl, eventValue.Shift, eventValue.Alt)
                    | None -> ()
                | KeyUp ->
                    match eventValue.Key with
                    | Some key -> window.RaiseKeyUp(key, eventValue.Ctrl, eventValue.Shift, eventValue.Alt)
                    | None -> ()
                | TextInput ->
                    match eventValue.Text with
                    | Some text -> window.RaiseTextInput text
                    | None -> ()

                Some eventValue.Context

    let private dispatchWindowEvent hwnd eventValue =
        let context = createEventContextForHandle hwnd
        let message = WindowMessage.Window(context, eventValue)
        defaultArg (dispatchWindowMessage hwnd message) context

    let private dispatchPointerEvent hwnd kind position button delta =
        let context = createEventContextForHandle hwnd

        let message =
            WindowMessage.Pointer(
                kind,
                { Context = context
                  PointerId = mousePointerId
                  Position = position
                  Button = button
                  Delta = delta }
            )

        defaultArg (dispatchWindowMessage hwnd message) context

    let private dispatchKeyboardEvent hwnd kind key text =
        let context = createEventContextForHandle hwnd
        let ctrl, shift, alt = getModifierState ()

        let message =
            WindowMessage.Keyboard(
                kind,
                { Context = context
                  Key = key
                  Text = text
                  Ctrl = ctrl
                  Shift = shift
                  Alt = alt }
            )

        defaultArg (dispatchWindowMessage hwnd message) context

    let private trackMouseLeave hwnd =
        let mutable trackEvent = Unchecked.defaultof<TRACKMOUSEEVENT>
        trackEvent.cbSize <- uint32 (Marshal.SizeOf<TRACKMOUSEEVENT>())
        trackEvent.dwFlags <- TME_LEAVE
        trackEvent.hwndTrack <- hwnd
        trackEvent.dwHoverTime <- 0u
        ensureTrue (TrackMouseEvent(&trackEvent)) "TrackMouseEvent"

    let private ensurePointerInside hwnd position =
        let window = requireWindow hwnd "ensurePointerInside"

        if not window.Pointer.IsInside then
            dispatchPointerEvent hwnd PointerEventKind.Entered position None None |> ignore

    let private virtualKeyToKey keyCode =
        match keyCode with
        | 0x08 -> Some "Backspace"
        | 0x09 -> Some "Tab"
        | 0x0D -> Some "Enter"
        | 0x1B -> Some "Escape"
        | 0x20 -> Some "Space"
        | 0x25 -> Some "ArrowLeft"
        | 0x26 -> Some "ArrowUp"
        | 0x27 -> Some "ArrowRight"
        | 0x28 -> Some "ArrowDown"
        | value when value >= 0x30 && value <= 0x39 -> Some(string (char value))
        | value when value >= 0x41 && value <= 0x5A -> Some(string (char value))
        | _ -> None

    let private handleClose (hwnd: HWND) =
        match tryGetWindowId hwnd with
        | None -> DefWindowProcW(hwnd, WM_CLOSE, 0un, 0n)
        | Some _ ->
            let context = dispatchWindowEvent hwnd CloseRequested

            if not context.Cancel then
                DestroyWindow hwnd |> ignore

            0n

    let private handleDestroy (hwnd: HWND) =
        match tryGetWindowId hwnd with
        | None -> 0n
        | Some _ ->
            dispatchWindowEvent hwnd Closed |> ignore
            unregisterWindow hwnd

            if windows.Count = 0 then
                PostQuitMessage(0)

            0n

    let private handleShowWindow hwnd (wParam: WPARAM) =
        if wParam <> 0un then
            dispatchWindowEvent hwnd Shown |> ignore
        else
            dispatchWindowEvent hwnd Hidden |> ignore

        0n

    let private handleActivate hwnd (wParam: WPARAM) =
        if lowWord wParam = WA_INACTIVE then
            dispatchWindowEvent hwnd Deactivated |> ignore
        else
            dispatchWindowEvent hwnd Activated |> ignore

        0n

    let private handleSetFocus hwnd =
        dispatchWindowEvent hwnd FocusGained |> ignore
        0n

    let private handleKillFocus hwnd =
        dispatchWindowEvent hwnd FocusLost |> ignore
        0n

    let private handleMove hwnd =
        dispatchWindowEvent hwnd (WindowEvent.Moved(getNativeWindowBounds hwnd)) |> ignore
        0n

    let private handleSize hwnd =
        dispatchWindowEvent hwnd (WindowEvent.Resized(getNativeWindowBounds hwnd)) |> ignore
        0n

    let private handleMouseMove hwnd lParam =
        let position = getPointerPositionFromClientLParam lParam
        trackMouseLeave hwnd
        ensurePointerInside hwnd position
        dispatchPointerEvent hwnd PointerEventKind.Moved position None None |> ignore
        0n

    let private handleMouseLeave hwnd =
        let position = (requireWindow hwnd "handleMouseLeave").Pointer.Position
        dispatchPointerEvent hwnd PointerEventKind.Exited position None None |> ignore
        0n

    let private handleMouseButtonDown hwnd button lParam =
        let position = getPointerPositionFromClientLParam lParam
        ensurePointerInside hwnd position
        SetCapture(hwnd) |> ignore
        dispatchPointerEvent hwnd PointerEventKind.Pressed position (Some button) None |> ignore
        0n

    let private handleMouseButtonUp hwnd button lParam =
        let position = getPointerPositionFromClientLParam lParam
        ReleaseCapture() |> ignore
        dispatchPointerEvent hwnd PointerEventKind.Released position (Some button) None |> ignore
        0n

    let private handleMouseWheel hwnd (wParam: WPARAM) lParam =
        let position = getPointerPositionFromScreenLParam hwnd lParam
        let deltaValue = int16 (highWord wParam) |> float
        let delta = { X = 0.0; Y = deltaValue / float WHEEL_DELTA }
        ensurePointerInside hwnd position
        dispatchPointerEvent hwnd PointerEventKind.Wheel position None (Some delta) |> ignore
        0n

    let private handleKeyDown hwnd (wParam: WPARAM) =
        match virtualKeyToKey (int wParam) with
        | Some key -> dispatchKeyboardEvent hwnd KeyDown (Some key) None |> ignore
        | None -> ()

        0n

    let private handleKeyUp hwnd (wParam: WPARAM) =
        match virtualKeyToKey (int wParam) with
        | Some key -> dispatchKeyboardEvent hwnd KeyUp (Some key) None |> ignore
        | None -> ()

        0n

    let private handleChar hwnd (wParam: WPARAM) =
        let codePoint = int wParam

        if codePoint > 0 then
            dispatchKeyboardEvent hwnd TextInput None (Some(Char.ConvertFromUtf32 codePoint)) |> ignore

        0n

    let private dispatchNativeMessage hwnd message wParam lParam =
        match message with
        | WM_CLOSE -> handleClose hwnd
        | WM_DESTROY -> handleDestroy hwnd
        | WM_SHOWWINDOW -> handleShowWindow hwnd wParam
        | WM_ACTIVATE -> handleActivate hwnd wParam
        | WM_SETFOCUS -> handleSetFocus hwnd
        | WM_KILLFOCUS -> handleKillFocus hwnd
        | WM_MOVE -> handleMove hwnd
        | WM_SIZE -> handleSize hwnd
        | WM_MOUSEMOVE -> handleMouseMove hwnd lParam
        | WM_MOUSELEAVE -> handleMouseLeave hwnd
        | WM_LBUTTONDOWN -> handleMouseButtonDown hwnd PointerButton.Left lParam
        | WM_LBUTTONUP -> handleMouseButtonUp hwnd PointerButton.Left lParam
        | WM_RBUTTONDOWN -> handleMouseButtonDown hwnd PointerButton.Right lParam
        | WM_RBUTTONUP -> handleMouseButtonUp hwnd PointerButton.Right lParam
        | WM_MBUTTONDOWN -> handleMouseButtonDown hwnd PointerButton.Middle lParam
        | WM_MBUTTONUP -> handleMouseButtonUp hwnd PointerButton.Middle lParam
        | WM_MOUSEWHEEL -> handleMouseWheel hwnd wParam lParam
        | WM_KEYDOWN
        | WM_SYSKEYDOWN -> handleKeyDown hwnd wParam
        | WM_KEYUP
        | WM_SYSKEYUP -> handleKeyUp hwnd wParam
        | WM_CHAR -> handleChar hwnd wParam
        | _ -> DefWindowProcW(hwnd, message, wParam, lParam)

    let private defaultWndProc =
        WndProc(fun hwnd message wParam lParam -> dispatchNativeMessage hwnd message wParam lParam)

    let destroyWindow (hwnd: HWND) =
        let result = DestroyWindow hwnd

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "DestroyWindow failed."))

    let closeWindow (hwnd: HWND) =
        handleClose hwnd |> ignore

    let showWindow (hwnd: HWND) =
        let window = requireWindow hwnd "showWindow"
        ShowWindow(hwnd, 5) |> ignore
        UpdateWindow(hwnd) |> ignore
        window.Show()

    let hideWindow (hwnd: HWND) =
        let window = requireWindow hwnd "hideWindow"
        ShowWindow(hwnd, 0) |> ignore
        window.Hide()

    let setWindowTitle (hwnd: HWND) (title: string) =
        let window = requireWindow hwnd "setWindowTitle"
        let result = SetWindowTextW(hwnd, title)

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "SetWindowTextW failed."))

        window.SetTitle title

    let setWindowBounds (hwnd: HWND) (bounds: WindowBounds) =
        let window = requireWindow hwnd "setWindowBounds"
        let x = int bounds.X
        let y = int bounds.Y
        let width = int bounds.Width
        let height = int bounds.Height

        let result = SetWindowPos(hwnd, nullHandle, x, y, width, height, 0u)

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "SetWindowPos failed."))

        window.SetBounds bounds

    let getWindowId (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowId"
        window.Id

    let getWindowState (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowState"
        window.State

    let getWindowBounds (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowBounds"
        window.Bounds

    let getWindowTitle (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowTitle"
        window.Title

    let getWindowStyle (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowStyle"
        window.Style

    let getWindowEvents (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowEvents"
        window.Events

    let getWindowLifecycleEvents (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowLifecycleEvents"
        window.WindowEvents

    let getWindowPointerEvents (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowPointerEvents"
        window.PointerEvents

    let getWindowKeyboardEvents (hwnd: HWND) =
        let window = requireWindow hwnd "getWindowKeyboardEvents"
        window.KeyboardEvents

    let runMessageLoop () =
        let mutable msg = Unchecked.defaultof<MSG>
        let mutable running = true

        while running do
            let result = GetMessageW(&msg, nullWindow, 0u, 0u)

            if result = -1 then
                raise (Win32Exception(Marshal.GetLastWin32Error(), "GetMessageW failed."))
            elif result = 0 then
                running <- false
            else
                TranslateMessage(&msg) |> ignore
                DispatchMessageW(&msg) |> ignore

    let createWindow (options: WindowCreateOptions) : HWND =
        let registration: Win32WindowClass.WindowClassRegistration =
            Win32WindowClass.registerDefault options.ClassName defaultWndProc

        let classNameValue: string = registration.Name
        let instanceValue: HINSTANCE = registration.Instance
        let exStyle: DWORD = computeExtendedWindowStyle options.Style
        let style: DWORD = computeWindowStyle options.Style
        let parentWindow: HWND = nullWindow
        let menuWindow: HMENU = nullWindow
        let windowId = allocateWindowId ()
        let (WindowId id) = windowId
        let paramValue = nativeint id
        let bounds = options.Bounds
        let window = Window.Window(windowId, options.Title, bounds, style = options.Style)

        let hwnd: HWND =
            Win32Native.CreateWindowExW(
                exStyle,
                classNameValue,
                options.Title,
                style,
                int bounds.X,
                int bounds.Y,
                int bounds.Width,
                int bounds.Height,
                parentWindow,
                menuWindow,
                instanceValue,
                paramValue
            )

        ensureWindow hwnd "CreateWindowExW"
        registerWindow hwnd windowId window |> ignore

        if options.IsVisible then
            showWindow hwnd

        hwnd

    let TryGetHdcUsingHwnd (hwnd: HWND): HDC  =
        let hdc = GetDC(hwnd)
        ensureNonZeroHandle hdc "GetDC"
        hdc

    let SetPixelFormat (hdc: HDC, pixelFormatDescriptor: PIXELFORMATDESCRIPTOR): unit =
        let mutable pfd = pixelFormatDescriptor

        let pixelFormat =
            ChoosePixelFormat(hdc, &pfd)

        if pixelFormat = 0 then
            raise (
                Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "ChoosePixelFormat failed."
                )
            )

        let result =
            SetPixelFormat(hdc, pixelFormat, &pfd)

        if not result then
            raise (
                Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "SetPixelFormat failed."
                )
            )
    let makeCurrent (hdc: HDC) (hglrc: HGLRC) =
        if not (wglMakeCurrent(hdc, hglrc)) then
            raise (
                Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "wglMakeCurrent failed."
                )
            )
    let createOpenGLContext (hdc: HDC): HGLRC =
        let hglrc = wglCreateContext(hdc)

        if hglrc = 0n then
            raise (
                Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "wglCreateContext failed."
                )
            )

        hglrc
