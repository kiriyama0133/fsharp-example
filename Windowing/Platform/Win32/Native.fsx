#load "./Types.fsx"

module Win32Native =

    open System
    open System.Runtime.InteropServices
    open Win32Types

    [<Literal>]
    let private User32 = "user32.dll"

    [<Literal>]
    let private Kernel32 = "kernel32.dll"

    let nullHandle: nativeint = nativeint 0

    let makeIntResource (value: uint16) : nativeint = nativeint (int value)

    [<DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern HINSTANCE GetModuleHandleW(string lpModuleName)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern HCURSOR LoadCursorW(HINSTANCE hInstance, nativeint lpCursorName)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern ATOM RegisterClassExW(WNDCLASSEXW& lpwcx)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern BOOL SetWindowTextW(HWND hWnd, string lpString)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern BOOL UnregisterClassW(string lpClassName, HINSTANCE hInstance)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern HWND CreateWindowExW(
        DWORD dwExStyle,
        string lpClassName,
        string lpWindowName,
        DWORD dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        HWND hWndParent,
        HMENU hMenu,
        HINSTANCE hInstance,
        nativeint lpParam
    )

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern BOOL DestroyWindow(HWND hWnd)

    [<DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)>]
    extern LRESULT DefWindowProcW(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL ShowWindow(HWND hWnd, int nCmdShow)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL UpdateWindow(HWND hWnd)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL GetClientRect(HWND hWnd, RECT& lpRect)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL GetWindowRect(HWND hWnd, RECT& lpRect)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL AdjustWindowRectEx(RECT& lpRect, DWORD dwStyle, BOOL bMenu, DWORD dwExStyle)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL GetMessageW(MSG& lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL TranslateMessage(MSG& lpMsg)

    [<DllImport(User32, SetLastError = true)>]
    extern LRESULT DispatchMessageW(MSG& lpMsg)

    [<DllImport(User32)>]
    extern void PostQuitMessage(int nExitCode)

    [<DllImport(User32, SetLastError = true)>]
    extern HWND SetFocus(HWND hWnd)

    [<DllImport(User32, SetLastError = true)>]
    extern HWND GetFocus()

    [<DllImport(User32, SetLastError = true)>]
    extern HWND SetActiveWindow(HWND hWnd)

    [<DllImport(User32, SetLastError = true)>]
    extern HWND GetActiveWindow()

    [<DllImport(User32, SetLastError = true)>]
    extern HWND SetCapture(HWND hWnd)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL ReleaseCapture()

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL TrackMouseEvent(TRACKMOUSEEVENT& lpEventTrack)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL GetCursorPos(POINT& lpPoint)

    [<DllImport(User32, SetLastError = true)>]
    extern BOOL ScreenToClient(HWND hWnd, POINT& lpPoint)

    [<DllImport(User32, SetLastError = true)>]
    extern int16 GetKeyState(int nVirtKey)

    [<DllImport(User32, EntryPoint = "GetWindowLongPtrW", SetLastError = true)>]
    extern nativeint GetWindowLongPtrW(HWND hWnd, int nIndex)

    [<DllImport(User32, EntryPoint = "SetWindowLongPtrW", SetLastError = true)>]
    extern nativeint SetWindowLongPtrW(HWND hWnd, int nIndex, nativeint dwNewLong)

    let lowWord (value: unativeint) = int (uint32 value &&& 0xFFFFu)

    let highWord (value: unativeint) = int ((uint32 value >>> 16) &&& 0xFFFFu)

    let getXLParam (lParam: nativeint) =
        int16 ((int64 lParam) &&& 0xFFFFL) |> int

    let getYLParam (lParam: nativeint) =
        int16 (((int64 lParam) >>> 16) &&& 0xFFFFL) |> int
