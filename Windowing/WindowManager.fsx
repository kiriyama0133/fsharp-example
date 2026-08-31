#load "./Platform.fsx"

open System
open System.Collections.Generic
open Types.WindowTypes

type ManagedWindow = Platform.Window

type Manager() =
    let windows = Dictionary<WindowId, ManagedWindow>()
    let subscriptions = Dictionary<WindowId, IDisposable list>()
    let zOrder = ResizeArray<WindowId>()

    let windowAdded = Event<ManagedWindow>()
    let windowRemoved = Event<ManagedWindow>()
    let windowMessageReceived = Event<ManagedWindow * WindowMessage>()
    let activeWindowChanged = Event<ManagedWindow option>()
    let focusedWindowChanged = Event<ManagedWindow option>()

    let mutable activeWindowId: WindowId option = None
    let mutable focusedWindowId: WindowId option = None

    let tryGetWindow windowId =
        match windows.TryGetValue windowId with
        | true, window -> Some window
        | false, _ -> None

    let resolveWindow windowId = windowId |> Option.bind tryGetWindow

    let setActiveWindowId windowId =
        if activeWindowId <> windowId then
            activeWindowId <- windowId
            activeWindowChanged.Trigger(resolveWindow windowId)

    let setFocusedWindowId windowId =
        if focusedWindowId <> windowId then
            focusedWindowId <- windowId
            focusedWindowChanged.Trigger(resolveWindow windowId)

    let removeFromZOrder windowId =
        let mutable index = zOrder.Count - 1

        while index >= 0 do
            if zOrder.[index] = windowId then
                zOrder.RemoveAt(index)

            index <- index - 1

    let bringToFront windowId =
        removeFromZOrder windowId
        zOrder.Add windowId

    let getWindows () =
        zOrder
        |> Seq.choose tryGetWindow
        |> Seq.toList

    let detachSubscriptions windowId =
        match subscriptions.TryGetValue windowId with
        | true, items ->
            items |> List.iter (fun item -> item.Dispose())
            subscriptions.Remove(windowId) |> ignore
        | false, _ -> ()

    let unregisterWindow windowId =
        match windows.TryGetValue windowId with
        | false, _ -> ()
        | true, window ->
            detachSubscriptions windowId
            windows.Remove(windowId) |> ignore
            removeFromZOrder windowId

            if activeWindowId = Some windowId then
                setActiveWindowId None

            if focusedWindowId = Some windowId then
                setFocusedWindowId None

            windowRemoved.Trigger window

    let attachWindow (window: ManagedWindow) =
        if windows.ContainsKey window.Id then
            invalidOp "The window is already registered with this manager."

        windows.[window.Id] <- window
        bringToFront window.Id

        let messageSubscription =
            window.Events.Subscribe(fun message ->
                windowMessageReceived.Trigger(window, message)
            )

        let stateSubscription =
            window.WindowEvents.Subscribe(fun (context, eventValue) ->
                match eventValue with
                | Shown ->
                    bringToFront context.WindowId
                | Activated ->
                    bringToFront context.WindowId
                    setActiveWindowId (Some context.WindowId)
                | Deactivated when activeWindowId = Some context.WindowId ->
                    setActiveWindowId None
                | FocusGained ->
                    setFocusedWindowId (Some context.WindowId)
                | FocusLost when focusedWindowId = Some context.WindowId ->
                    setFocusedWindowId None
                | Closed ->
                    unregisterWindow context.WindowId
                | _ -> ()
            )

        subscriptions.[window.Id] <- [ messageSubscription; stateSubscription ]
        windowAdded.Trigger window
        window

    member _.Count = windows.Count

    member _.CurrentPlatform = Platform.Current

    member _.Windows = getWindows ()

    member _.ActiveWindow = resolveWindow activeWindowId

    member _.FocusedWindow = resolveWindow focusedWindowId

    member _.WindowAdded = windowAdded.Publish

    member _.WindowRemoved = windowRemoved.Publish

    member _.WindowMessageReceived = windowMessageReceived.Publish

    member _.ActiveWindowChanged = activeWindowChanged.Publish

    member _.FocusedWindowChanged = focusedWindowChanged.Publish

    member _.WindowOrder = zOrder |> Seq.toList

    member _.TryGetWindow(windowId: WindowId) = tryGetWindow windowId

    member _.CreateWindow(options: WindowCreateOptions) =
        Platform.CreateWindow options
        |> attachWindow

    member _.Register(window: ManagedWindow) = attachWindow window

    member _.ShowAll() =
        getWindows () |> List.iter (fun window -> window.Show())

    member _.HideAll() =
        getWindows () |> List.iter (fun window -> window.Hide())

    member _.CloseAll() =
        getWindows () |> List.iter (fun window -> window.Close())

    member _.Run() = Platform.RunMessageLoop ()
