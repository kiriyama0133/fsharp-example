#load "./Windowing/WindowManager.fsx"

open Types.WindowTypes
open WindowManager

let bounds =
    { X = 160.0
      Y = 120.0
      Width = 960.0
      Height = 640.0 }

let options =
    { ClassName = "FSharpWindowingApp"
      Title = "F# Windowing Demo"
      Bounds = bounds
      Style = defaultWindowStyle
      IsVisible = true }

let manager = Manager()

manager.WindowAdded.Add(fun managedWindow -> printfn "window added: %A" managedWindow.Id)

manager.WindowRemoved.Add(fun managedWindow -> printfn "window removed: %A" managedWindow.Id)
let window = manager.CreateWindow options

manager.ActiveWindowChanged.Add(fun activeWindow ->
    let activeWindowId =
        activeWindow |> Option.map (fun managedWindow -> managedWindow.Id)

    printfn "active window changed: %A" activeWindowId)

manager.FocusedWindowChanged.Add(fun focusedWindow ->
    let focusedWindowId =
        focusedWindow |> Option.map (fun managedWindow -> managedWindow.Id)

    printfn "focused window changed: %A" focusedWindowId)

window.WindowEvents.Add(fun (_, eventValue) -> printfn "window event: %A" eventValue)

window.KeyboardEvents.Add(fun (kind, eventValue) ->
    match kind, eventValue.Key with
    | KeyDown, Some "Escape" ->
        printfn "Escape pressed, closing window."
        manager.CloseAll()
    | _ -> ())

window.PointerEvents.Add(fun (kind: PointerEventKind, event) ->
    printfn "Pointer %A: X=%f, Y=%f" kind event.Position.X event.Position.Y)

printfn "Starting window manager on platform: %A" manager.CurrentPlatform.Kind
printfn "Press Esc or click the close button to exit."

manager.Run()
