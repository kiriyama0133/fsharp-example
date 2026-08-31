#load "./Types.fsx"
#load "./Platform/Win32/Backend.fsx"

open Types.WindowTypes
open Backend.Win32Platform
open System

type Window =
    { Id: WindowId
      Events: IEvent<WindowMessage>
      WindowEvents: IEvent<EventContext * WindowEvent>
      PointerEvents: IEvent<PointerEventKind * PointerEvent>
      KeyboardEvents: IEvent<KeyboardEventKind * KeyboardEvent>
      GetState: unit -> WindowState
      GetBounds: unit -> WindowBounds
      GetTitle: unit -> string
      GetStyle: unit -> WindowStyle
      Show: unit -> unit
      Hide: unit -> unit
      Close: unit -> unit
      SetTitle: string -> unit
      SetBounds: WindowBounds -> unit }

type WindowPlatform =
    { Kind: PlatformKind
      CreateWindow: WindowCreateOptions -> Window
      RunMessageLoop: unit -> unit }

let private detectPlatform () =
    if OperatingSystem.IsWindows() then
        Types.WindowTypes.Windows
    elif OperatingSystem.IsLinux() then
        Types.WindowTypes.Linux
    elif OperatingSystem.IsMacOS() then
        Types.WindowTypes.MacOS
    else
        Unkown

let private wrapWin32Window hwnd : Window =
    { Id = getWindowId hwnd
      Events = getWindowEvents hwnd
      WindowEvents = getWindowLifecycleEvents hwnd
      PointerEvents = getWindowPointerEvents hwnd
      KeyboardEvents = getWindowKeyboardEvents hwnd
      GetState = fun () -> getWindowState hwnd
      GetBounds = fun () -> getWindowBounds hwnd
      GetTitle = fun () -> getWindowTitle hwnd
      GetStyle = fun () -> getWindowStyle hwnd
      Show = fun () -> showWindow hwnd
      Hide = fun () -> hideWindow hwnd
      Close = fun () -> closeWindow hwnd
      SetTitle = fun title -> setWindowTitle hwnd title
      SetBounds = fun bounds -> setWindowBounds hwnd bounds }

let private currentPlatform () : WindowPlatform =
    match detectPlatform () with
    | Windows ->
        { Kind = Windows
          CreateWindow = fun options -> createWindow options |> wrapWin32Window
          RunMessageLoop = runMessageLoop }
    //| Linux -> Linux.create ()
    | _ -> failwith "Unsupported platform."

let Current : WindowPlatform = currentPlatform ()
let CurrentKind : PlatformKind = Current.Kind

let CreateWindow (options: WindowCreateOptions) : Window = Current.CreateWindow options

let CreateDefaultWindow className title bounds : Window =
    defaultWindowCreateOptions className title bounds
    |> CreateWindow

let RunMessageLoop () = Current.RunMessageLoop ()
