#load "./Native.fsx"

module Win32WindowClass =

    open System
    open System.Collections.Generic
    open System.ComponentModel
    open System.Runtime.InteropServices
    open Native.Win32Native
    open Win32Types

    [<Literal>]
    let IDC_ARROW = 32512us

    [<Literal>]
    let CS_VREDRAW = 0x0001u

    [<Literal>]
    let CS_HREDRAW = 0x0002u

    type WindowClassRegistration =
        { Name: string
          Instance: HINSTANCE
          WndProc: WndProc
          Atom: ATOM
          Style: UINT
          Cursor: HCURSOR }

    let private registrations = Dictionary<string, WindowClassRegistration>()

    let private ensureNonZeroHandle handle apiName =
        if handle = nullHandle then
            raise (Win32Exception(Marshal.GetLastWin32Error(), apiName + " failed."))

    let private ensureTrue result apiName =
        if result = 0 then
            raise (Win32Exception(Marshal.GetLastWin32Error(), apiName + " failed."))

    let private createWindowClass
        (name: string)
        (instance: HINSTANCE)
        (wndProc: WndProc)
        (style: UINT)
        (cursor: HCURSOR)
        =
        let mutable windowClass = Unchecked.defaultof<WNDCLASSEXW>

        windowClass.cbSize <- uint32 (Marshal.SizeOf<WNDCLASSEXW>())
        windowClass.style <- style
        windowClass.lpfnWndProc <- wndProc
        windowClass.cbClsExtra <- 0
        windowClass.cbWndExtra <- 0
        windowClass.hInstance <- instance
        windowClass.hIcon <- nullHandle
        windowClass.hCursor <- cursor
        windowClass.hbrBackground <- nullHandle
        windowClass.lpszMenuName <- null
        windowClass.lpszClassName <- name
        windowClass.hIconSm <- nullHandle

        windowClass

    let register
        (className: string)
        (wndProc: WndProc)
        (style: UINT option)
        (instance: HINSTANCE option)
        (cursor: HCURSOR option)
        : WindowClassRegistration =
        match registrations.TryGetValue className with
        | true, registration -> registration
        | false, _ ->
            let resolvedInstance = defaultArg instance (GetModuleHandleW null)
            ensureNonZeroHandle resolvedInstance "GetModuleHandleW"

            let resolvedCursor =
                defaultArg cursor (LoadCursorW(nullHandle, makeIntResource IDC_ARROW))

            ensureNonZeroHandle resolvedCursor "LoadCursorW"

            let resolvedStyle = defaultArg style (CS_HREDRAW ||| CS_VREDRAW)

            let mutable windowClass =
                createWindowClass className resolvedInstance wndProc resolvedStyle resolvedCursor

            let atom = RegisterClassExW(&windowClass)

            if atom = 0us then
                raise (Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassExW failed."))

            let registration =
                { Name = className
                  Instance = resolvedInstance
                  WndProc = wndProc
                  Atom = atom
                  Style = resolvedStyle
                  Cursor = resolvedCursor }

            registrations.[className] <- registration
            registration

    let registerDefault className wndProc =
        register className wndProc None None None

    let unregister (registration: WindowClassRegistration) =
        match registrations.TryGetValue registration.Name with
        | false, _ -> ()
        | true, current when not (obj.ReferenceEquals(current.WndProc, registration.WndProc)) -> ()
        | true, _ ->
            let result = UnregisterClassW(registration.Name, registration.Instance)
            ensureTrue result "UnregisterClassW"
            registrations.Remove(registration.Name) |> ignore

    let tryFind className =
        match registrations.TryGetValue className with
        | true, registration -> Some registration
        | false, _ -> None
