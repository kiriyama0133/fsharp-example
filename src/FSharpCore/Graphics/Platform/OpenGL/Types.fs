module OpenGLTypes

open System

type OpenGLContext =
    { MakeCurrent: unit -> unit
      GetProcAddress: string -> nativeint
      SwapBuffers: unit -> unit
      Dispose: unit -> unit }

type PlatformKind =
    | Windows
    | Linux
    | MacOS
    | Unknown
