module OpenGLTypes

open System

type OpenGLContext =
    {
        MakeCurrent: unit -> unit
        SwapBuffers: unit -> unit
        GetProcAddress: string -> nativeint
    }
type PlatformKind =
    | Windows
    | Linux
    | MacOS
    | Unkown