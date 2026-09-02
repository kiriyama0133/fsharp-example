module OpenGLContext

open Platform
open Silk.NET.WGL
open Win32Platform
open WindowTypes
open Win32Types
open System.Runtime.InteropServices
open OpenGLTypes

let private getHdcInWindow (window: Window) =
    match window.TryGetNativeHandle() with
    | Some(Win32Hwnd hwnd) -> TryGetHdcUsingHwnd hwnd

    | None -> failwith "Window has no native handle."

let private createPixelFormatDescriptor () : PIXELFORMATDESCRIPTOR =
    { nSize = uint16 (Marshal.SizeOf<PIXELFORMATDESCRIPTOR>())
      nVersion = 1us

      dwFlags = PFD_DRAW_TO_WINDOW ||| PFD_SUPPORT_OPENGL ||| PFD_DOUBLEBUFFER

      iPixelType = PFD_TYPE_RGBA
      cColorBits = 32uy
      cDepthBits = 24uy
      cStencilBits = 8uy

      cRedBits = 0uy
      cRedShift = 0uy
      cGreenBits = 0uy
      cGreenShift = 0uy
      cBlueBits = 0uy
      cBlueShift = 0uy
      cAlphaBits = 0uy
      cAlphaShift = 0uy

      cAccumBits = 0uy
      cAccumRedBits = 0uy
      cAccumGreenBits = 0uy
      cAccumBlueBits = 0uy
      cAccumAlphaBits = 0uy

      cAuxBuffers = 0uy
      iLayerType = PFD_MAIN_PLANE
      bReserved = 0uy

      dwLayerMask = 0u
      dwVisibleMask = 0u
      dwDamageMask = 0u }

let private isInvalidProcAddress (address: nativeint) =
    address = 0n
    || address = nativeint 1
    || address = nativeint 2
    || address = nativeint 3
    || address = nativeint -1

let private getProcAddress (procName: string) =
    let procAddress = Win32Native.wglGetProcAddress(procName)

    if not (isInvalidProcAddress procAddress) then
        procAddress
    else
        let moduleHandle = Win32Native.GetModuleHandle("opengl32.dll")

        if moduleHandle = 0n then
            failwith "GetModuleHandle(opengl32.dll) failed."

        Win32Native.GetProcAddress(moduleHandle, procName)

let CreateContext (window: Window) =
    let hdc = getHdcInWindow window

    let pfd = createPixelFormatDescriptor ()

    SetPixelFormat(hdc, pfd) |> ignore

    let hglrc = createOpenGLContext hdc

    makeCurrent hdc hglrc

    let OpenGLContext: OpenGLContext =
        { MakeCurrent = fun () -> makeCurrent hdc hglrc
          GetProcAddress = fun procName -> getProcAddress procName
          SwapBuffers = fun () -> swapBuffers hdc
          Dispose =
            fun () ->
                makeCurrent 0n 0n
                deleteOpenGLContext hglrc }

    OpenGLContext
