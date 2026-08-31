module Win32Types

open System
open System.Runtime.InteropServices



type HWND = nativeint
type HINSTANCE = nativeint
type HCURSOR = nativeint
type HICON = nativeint
type HMENU = nativeint
type HBRUSH = nativeint
type WPARAM = unativeint
type LPARAM = nativeint
type LRESULT = nativeint

type UINT = uint32 // 32 bit unsigned int
type DWORD = uint32 // 32 bit unsigned long
type WORD = uint16 // 16 bit unsigned int
type BOOL = int32 // 32 bit int
type LONG = int32 // 32 bit int
type ATOM = uint16 // 16bit unsigned int

[<Literal>]
let WM_CLOSE: UINT = 0x0010u

[<Literal>]
let WM_DESTROY: UINT = 0x0002u

[<Literal>]
let WM_NCCREATE: UINT = 0x0081u

[<Literal>]
let WM_SHOWWINDOW: UINT = 0x0018u

[<Literal>]
let WM_MOVE: UINT = 0x0003u

[<Literal>]
let WM_SIZE: UINT = 0x0005u

[<Literal>]
let WM_ACTIVATE: UINT = 0x0006u

[<Literal>]
let WM_SETFOCUS: UINT = 0x0007u

[<Literal>]
let WM_KILLFOCUS: UINT = 0x0008u

[<Literal>]
let WM_KEYDOWN: UINT = 0x0100u

[<Literal>]
let WM_KEYUP: UINT = 0x0101u

[<Literal>]
let WM_CHAR: UINT = 0x0102u

[<Literal>]
let WM_SYSKEYDOWN: UINT = 0x0104u

[<Literal>]
let WM_SYSKEYUP: UINT = 0x0105u

[<Literal>]
let WM_MOUSEMOVE: UINT = 0x0200u

[<Literal>]
let WM_LBUTTONDOWN: UINT = 0x0201u

[<Literal>]
let WM_LBUTTONUP: UINT = 0x0202u

[<Literal>]
let WM_RBUTTONDOWN: UINT = 0x0204u

[<Literal>]
let WM_RBUTTONUP: UINT = 0x0205u

[<Literal>]
let WM_MBUTTONDOWN: UINT = 0x0207u

[<Literal>]
let WM_MBUTTONUP: UINT = 0x0208u

[<Literal>]
let WM_MOUSEWHEEL: UINT = 0x020Au

[<Literal>]
let WM_MOUSELEAVE: UINT = 0x02A3u

[<Literal>]
let WA_INACTIVE = 0

[<Literal>]
let TME_LEAVE: DWORD = 0x00000002u

[<Literal>]
let VK_SHIFT = 0x10

[<Literal>]
let VK_CONTROL = 0x11

[<Literal>]
let VK_MENU = 0x12

[<Literal>]
let WHEEL_DELTA = 120

[<Literal>]
let WS_OVERLAPPED: DWORD = 0x00000000u

[<Literal>]
let WS_CAPTION: DWORD = 0x00C00000u

[<Literal>]
let WS_SYSMENU: DWORD = 0x00080000u

[<Literal>]
let WS_THICKFRAME: DWORD = 0x00040000u

[<Literal>]
let WS_MINIMIZEBOX: DWORD = 0x00020000u

[<Literal>]
let WS_MAXIMIZEBOX: DWORD = 0x00010000u

[<Literal>]
let WS_POPUP: DWORD = 0x80000000u

[<Literal>]
let WS_DISABLED: DWORD = 0x08000000u

[<Literal>]
let WS_EX_TOPMOST: DWORD = 0x00000008u

[<Literal>]
let WS_EX_TRANSPARENT: DWORD = 0x00000020u

type WndProc = delegate of HWND * UINT * WPARAM * LPARAM -> LRESULT

[<Struct; StructLayout(LayoutKind.Sequential)>]
type POINT =
    val mutable X: int32
    val mutable Y: int32
    new(x, y) = { X = x; Y = y }

[<Struct; StructLayout(LayoutKind.Sequential)>]
type RECT =
    val mutable Left: int32
    val mutable Top: int32
    val mutable Right: int32
    val mutable Bottom: int32

[<Struct; StructLayout(LayoutKind.Sequential)>]
type MSG =
    val mutable hwnd: HWND
    val mutable message: UINT
    val mutable wParam: WPARAM
    val mutable lParam: LPARAM
    val mutable time: DWORD
    val mutable pt: POINT
    val mutable lPrivate: DWORD

[<Struct; StructLayout(LayoutKind.Sequential)>]
type TRACKMOUSEEVENT =
    val mutable cbSize: DWORD
    val mutable dwFlags: DWORD
    val mutable hwndTrack: HWND
    val mutable dwHoverTime: DWORD

[<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
type WNDCLASSEXW =
    val mutable cbSize: UINT
    val mutable style: UINT

    [<MarshalAs(UnmanagedType.FunctionPtr)>]
    val mutable lpfnWndProc: WndProc

    val mutable cbClsExtra: int32
    val mutable cbWndExtra: int32
    val mutable hInstance: HINSTANCE
    val mutable hIcon: HICON
    val mutable hCursor: HCURSOR
    val mutable hbrBackground: HBRUSH

    [<MarshalAs(UnmanagedType.LPWStr)>]
    val mutable lpszMenuName: string

    [<MarshalAs(UnmanagedType.LPWStr)>]
    val mutable lpszClassName: string

    val mutable hIconSm: HICON
