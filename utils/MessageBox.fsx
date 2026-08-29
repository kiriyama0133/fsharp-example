#load "../Common/Logger.fsx"

open System
open System.Runtime.InteropServices
open Common.Logger

let logger =
    Logger.empty
    |> Logger.log LogLevel.Info "程序启动"
    |> Logger.log LogLevel.Info "准备调用 MessageBox"

module NativeMethods =
    [<DllImport("user32.dll", CharSet = CharSet.Auto)>]
    extern int MessageBox(IntPtr hWnd, string text, string caption, uint32 uType)

let result =
    NativeMethods.MessageBox(IntPtr.Zero, "Hello from F#!", "F# P/Invoke", 0u)

let finalLogger =
    logger |> Logger.log LogLevel.Info (sprintf "MessageBox 返回: %d" result)

finalLogger |> Logger.print
printfn "MessageBox result: %d" result
