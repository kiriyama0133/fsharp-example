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
