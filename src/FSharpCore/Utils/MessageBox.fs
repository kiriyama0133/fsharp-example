module Utils.MessageBox

open System
open System.Runtime.InteropServices
open Common.Logger

module NativeMethods =
    [<DllImport("user32.dll", CharSet = CharSet.Auto)>]
    extern int MessageBox(IntPtr hWnd, string text, string caption, uint32 uType)

let show (text: string) (caption: string) (uType: uint32) =
    NativeMethods.MessageBox(IntPtr.Zero, text, caption, uType)

let showLogged (text: string) (caption: string) (uType: uint32) (logger: Logger) =
    let result = show text caption uType
    let finalLogger = logger |> Logger.log LogLevel.Info (sprintf "MessageBox 返回: %d" result)
    result, finalLogger
