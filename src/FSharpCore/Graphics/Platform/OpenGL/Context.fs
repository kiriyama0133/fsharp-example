module OpenGLContext

open Platform
open Silk.NET.WGL
open Win32Platform
open WindowTypes
open Win32Types
open OpenGLTypes
open System

let Platform: PlatformKind = PlatformKind.Windows

let private detectPlatform () =
    if OperatingSystem.IsWindows() then
        Platform = PlatformKind.Windows
    elif OperatingSystem.IsLinux() then
        Platform = PlatformKind.Linux
    elif OperatingSystem.IsMacOS() then
        Platform = PlatformKind.MacOS
    else
        Platform = Unkown

let getHdcInWindow (window: Window) =
    match window.TryGetNativeHandle() with
    | Some (Win32Hwnd hwnd) ->
        TryGetHdcUsingHwnd hwnd

    | None ->
        failwith "Window has no native handle."