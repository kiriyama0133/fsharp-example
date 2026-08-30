#load "./Native.fsx"
#load "../../Types.fsx"
#load "../../Window.fsx"
#load "./WindowClass.fsx"

open System
open System.ComponentModel
open System.Collections.Generic


module Win32Platform =
    open Win32Types
    open Types.WindowTypes
    open Window
    open Native.Win32Native
    open System.Runtime.InteropServices


    let private windowIds = Dictionary<HWND, WindowId>()
    let private windows = Dictionary<HWND, Window.Window>()
    let private nextWindowId = ref 0
    let private nullWindow: HWND = Native.Win32Native.nullHandle

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

    let private ensureWindow (handle: HWND) apiName =
        if handle = nullWindow then
            raise (Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(), apiName + " failed."))

    let private WS_OVERLAPPEDWINDOW =
        WS_OVERLAPPED
        ||| WS_CAPTION
        ||| WS_SYSMENU
        ||| WS_THICKFRAME
        ||| WS_MINIMIZEBOX
        ||| WS_MAXIMIZEBOX

    let createWindow
        (className: string)
        (title: string)
        (x: int)
        (y: int)
        (width: int)
        (height: int)
        (wndProc: WndProc)
        : HWND =

        let registration: WindowClass.Win32WindowClass.WindowClassRegistration =
            WindowClass.Win32WindowClass.registerDefault className wndProc

        let classNameValue: string = registration.Name
        let instanceValue: HINSTANCE = registration.Instance
        let exStyle: DWORD = 0u
        let style: DWORD = WS_OVERLAPPEDWINDOW
        let parentWindow: HWND = nullWindow
        let menuWindow: HMENU = nullWindow
        let windowId = allocateWindowId ()
        let (WindowId id) = windowId
        let paramValue = nativeint id
        let window = Window.Window(windowId, title, { X = float x; Y = float y; Width = float width; Height = float height })

        let hwnd: HWND =
            Native.Win32Native.CreateWindowExW(
                exStyle,
                classNameValue,
                title,
                style,
                x,
                y,
                width,
                height,
                parentWindow,
                menuWindow,
                instanceValue,
                paramValue
            )

        ensureWindow hwnd "CreateWindowExW"
        registerWindow hwnd windowId window |> ignore
        hwnd

    let destroyWindow (hwnd: HWND) =
        let result = DestroyWindow hwnd

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "DestroyWindow failed."))

    let showWindow (hwnd: HWND) =
        ShowWindow(hwnd, 5) |> ignore
        UpdateWindow(hwnd) |> ignore

    let hideWindow (hwnd: HWND) = ShowWindow(hwnd, 0) |> ignore

    let setWindowTitle (hwnd: HWND) (title: string) =
        let result = SetWindowTextW(hwnd, title)

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "SetWindowTextW failed."))

    let setWindowBounds (hwnd: HWND) (bounds: WindowBounds) =
        let x = int bounds.X
        let y = int bounds.Y
        let width = int bounds.Width
        let height = int bounds.Height

        let result = SetWindowPos(hwnd, nullHandle, x, y, width, height, 0u)

        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), "SetWindowPos failed."))

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
        | Pointer(kind, event) ->
            match tryGetWindow hwnd with
            | None -> None
            | Some window ->
                match kind with
                | PointerEventKind.Entered ->
                    window.EnterPointer(event.PointerId, event.Position)
                | PointerEventKind.Exited ->
                    window.LeavePointer(event.PointerId, event.Position)
                | PointerEventKind.Moved ->
                    window.MovePointer(event.PointerId, event.Position)
                | PointerEventKind.Pressed ->
                    match event.Button with
                    | Some button -> window.PressPointer(event.PointerId, button, event.Position)
                    | None -> ()
                | PointerEventKind.Released ->
                    match event.Button with
                    | Some button -> window.ReleasePointer(event.PointerId, button, event.Position)
                    | None -> ()
                | PointerEventKind.Wheel ->
                    match event.Delta with
                    | Some delta -> window.ScrollPointer(event.PointerId, event.Position, delta)
                    | None -> ()

                Some event.Context
        | Keyboard(kind, event) ->
            match tryGetWindow hwnd with
            | None -> None
            | Some window ->
                match kind with
                | KeyDown ->
                    match event.Key with
                    | Some key -> window.RaiseKeyDown(key, event.Ctrl, event.Shift, event.Alt)
                    | None -> ()
                | KeyUp ->
                    match event.Key with
                    | Some key -> window.RaiseKeyUp(key, event.Ctrl, event.Shift, event.Alt)
                    | None -> ()
                | TextInput ->
                    match event.Text with
                    | Some text -> window.RaiseTextInput text
                    | None -> ()

                Some event.Context

    let private handleClose (hwnd: HWND) =
        match tryGetWindowId hwnd with
        | None -> DefWindowProcW(hwnd, WM_CLOSE, 0un, 0n)

        | Some windowId ->
            let context = createEventContext windowId

            let message = WindowMessage.Window(context, CloseRequested)

            let dispatchedContext =
                dispatchWindowMessage hwnd message

            let effectiveContext = defaultArg dispatchedContext context

            if not effectiveContext.Cancel then
                DestroyWindow hwnd |> ignore

            0n

    let private handleDestroy (hwnd: HWND) =
        match tryGetWindowId hwnd with
        | None -> 0n
        | Some windowId ->
            let context = createEventContext windowId
            let message = WindowMessage.Window(context, Closed)
            dispatchWindowMessage hwnd message |> ignore
            unregisterWindow hwnd

            PostQuitMessage(0)

            0n
