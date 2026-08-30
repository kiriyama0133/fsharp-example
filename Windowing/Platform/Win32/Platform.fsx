#load "./Native.fsx"
#load "../../Types.fsx"
#load "./WindowClass.fsx"

open System
open System.ComponentModel
open System.Collections.Generic


module Win32Platform =
    open Win32Types
    open Types.WindowTypes
    open Native.Win32Native
    open System.Runtime.InteropServices

    let private windowIds = Dictionary<HWND, WindowId>()
    let private nextWindowId = ref 0
    let private nullWindow: HWND = Native.Win32Native.nullHandle

    let private allocateWindowId () =
        let id = nextWindowId.Value
        nextWindowId.Value <- id + 1
        WindowId id

    let private registerWindow hwnd =
        let windowId = allocateWindowId ()
        windowIds.[hwnd] <- windowId
        windowId

    let private tryGetWindowId hwnd =
        match windowIds.TryGetValue hwnd with
        | true, windowId -> Some windowId
        | false, _ -> None

    let private unregisterWindow hwnd = windowIds.Remove(hwnd) |> ignore

    let private ensureWindow (handle: HWND) apiName =
        if handle = nullWindow then
            raise (Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(), apiName + " failed."))

    [<Literal>]
    let private WS_OVERLAPPED = 0x00000000u

    [<Literal>]
    let private WS_CAPTION = 0x00C00000u

    [<Literal>]
    let private WS_SYSMENU = 0x00080000u

    [<Literal>]
    let private WS_THICKFRAME = 0x00040000u

    [<Literal>]
    let private WS_MINIMIZEBOX = 0x00020000u

    [<Literal>]
    let private WS_MAXIMIZEBOX = 0x00010000u

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
        registerWindow hwnd |> ignore
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

    let private dispatchWindowMessage (message: WindowMessage) = printfn "%A" message
