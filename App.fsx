#load "./Windowing/Platform.fsx"

open Types.WindowTypes
open Platform

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

let window = CreateWindow options

window.WindowEvents.Add(fun (_, eventValue) -> printfn "window event: %A" eventValue)

window.KeyboardEvents.Add(fun (kind, eventValue) ->
    match kind, eventValue.Key with
    | KeyDown, Some "Escape" ->
        printfn "Escape pressed, closing window."
        window.Close()
    | _ -> ())

window.PointerEvents.Add(fun (kind: PointerEventKind, event) ->
    printfn "Pointer %A: X=%f, Y=%f" kind event.Position.X event.Position.Y)

printfn "Starting window on platform: %A" CurrentKind
printfn "Press Esc or click the close button to exit."

RunMessageLoop()
