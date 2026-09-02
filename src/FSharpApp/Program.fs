module Program

open WindowTypes
open WindowManager
open OpenGLResources
open Silk.NET.OpenGL

[<EntryPoint>]
let main _ =
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

    // manager.WindowAdded.Add(fun managedWindow -> printfn "window added: %A" managedWindow.Id)

    // manager.WindowRemoved.Add(fun managedWindow -> printfn "window removed: %A" managedWindow.Id)

    // manager.ActiveWindowChanged.Add(fun activeWindow ->
    //     let activeWindowId =
    //         activeWindow |> Option.map (fun managedWindow -> managedWindow.Id)

    //     printfn "active window changed: %A" activeWindowId)

    // manager.FocusedWindowChanged.Add(fun focusedWindow ->
    //     let focusedWindowId =
    //         focusedWindow |> Option.map (fun managedWindow -> managedWindow.Id)

    //     printfn "focused window changed: %A" focusedWindowId)

    let window = manager.CreateWindow options
    let context = OpenGLContext.CreateContext window
    let device = OpenGLDevice.CreateGraphicsDevice context
    let vertexBuffer = device.CreateBuffer { Size = 1024UL; Usage = Static }

    let shader =
        device.CreateWindowShader
            { Width = int (window.GetBounds().Width)
              Height = int (window.GetBounds().Height) }

    let renderFrame () =
        device.MakeCurrent()
        device.SetClearColor(0.1f, 0.2f, 0.3f, 1.0f)
        device.Clear [ ClearBuffer.ColorBuffer; ClearBuffer.DepthBuffer ]
        shader.Use()
        vertexBuffer.Bind()
        device.SwapBuffers()

    renderFrame ()

    // window.WindowEvents.Add(fun (_, eventValue) -> printfn "window event: %A" eventValue)

    // window.KeyboardEvents.Add(fun (kind, eventValue) ->
    //     match kind, eventValue.Key with
    //     | KeyDown, Some "Escape" ->
    //         printfn "Escape pressed, closing window."
    //         manager.CloseAll()
    //     | _ -> ())

    // window.PointerEvents.Add(fun (kind, eventValue) ->
    //     printfn "Pointer %A: X=%f, Y=%f" kind eventValue.Position.X eventValue.Position.Y)

    manager.Run()
    0
