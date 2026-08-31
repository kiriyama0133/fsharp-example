module Window
    open System
    open WindowTypes

    type Window
        (
            id: WindowId,
            title: string,
            bounds: WindowBounds,
            ?style: WindowStyle,
            ?owner: WindowId,
            ?isModal: bool
        ) =
        let messageEvent = Event<WindowMessage>()
        let windowEvent = Event<EventContext * WindowEvent>()
        let pointerEvent = Event<PointerEventKind * PointerEvent>()
        let keyboardEvent = Event<KeyboardEventKind * KeyboardEvent>()

        let mutable title = title
        let mutable bounds = bounds
        let mutable style = defaultArg style defaultWindowStyle
        let mutable state = WindowState.Created
        let mutable activation = ActivationState.Inactive
        let mutable focus = FocusState.Unfocused
        let mutable pointer = defaultPointerState
        let mutable owner = owner
        let mutable isModal = defaultArg isModal false
        let mutable isEnabled = true
        let mutable zIndex = 0

        let emitWindowEvent context eventValue =
            windowEvent.Trigger(context, eventValue)
            messageEvent.Trigger(WindowMessage.Window(context, eventValue))
            context

        let emitPointerEvent kind eventValue =
            pointerEvent.Trigger(kind, eventValue)
            messageEvent.Trigger(WindowMessage.Pointer(kind, eventValue))
            eventValue

        let emitKeyboardEvent kind eventValue =
            keyboardEvent.Trigger(kind, eventValue)
            messageEvent.Trigger(WindowMessage.Keyboard(kind, eventValue))
            eventValue

        let createPointerEvent
            (pointerId: PointerId)
            (position: PointerPosition)
            (button: PointerButton option)
            (delta: PointerDelta option)
            : PointerEvent
            =
            { Context = createEventContext id
              PointerId = pointerId
              Position = position
              Button = button
              Delta = delta }

        let createKeyboardEvent
            (key: string option)
            (text: string option)
            (ctrl: bool)
            (shift: bool)
            (alt: bool)
            : KeyboardEvent
            =
            { Context = createEventContext id
              Key = key
              Text = text
              Ctrl = ctrl
              Shift = shift
              Alt = alt }

        member _.Id = id

        member _.Events = messageEvent.Publish

        member _.WindowEvents = windowEvent.Publish

        member _.PointerEvents = pointerEvent.Publish

        member _.KeyboardEvents = keyboardEvent.Publish

        member _.Title = title

        member _.Bounds = bounds

        member _.Style = style

        member _.State = state

        member _.Activation = activation

        member _.Focus = focus

        member _.Pointer = pointer

        member _.Owner
            with get () = owner
            and set value = owner <- value

        member _.IsModal
            with get () = isModal
            and set value = isModal <- value

        member _.IsEnabled
            with get () = isEnabled
            and set value = isEnabled <- value

        member _.ZIndex
            with get () = zIndex
            and set value = zIndex <- value

        member _.SetTitle(value: string) =
            if title <> value then
                title <- value
                emitWindowEvent (createEventContext id) (TitleChanged value) |> ignore

        member _.SetStyle(value: WindowStyle) =
            if style <> value then
                style <- value
                emitWindowEvent (createEventContext id) (StyleChanged value) |> ignore

        member _.SetBounds(value: WindowBounds) =
            let previousBounds = bounds

            if previousBounds <> value then
                bounds <- value

                if previousBounds.X <> value.X || previousBounds.Y <> value.Y then
                    emitWindowEvent (createEventContext id) (WindowEvent.Moved value) |> ignore

                if previousBounds.Width <> value.Width || previousBounds.Height <> value.Height then
                    emitWindowEvent (createEventContext id) (WindowEvent.Resized value) |> ignore

        member _.Show() =
            if state <> WindowState.Visible then
                state <- WindowState.Visible
                emitWindowEvent (createEventContext id) WindowEvent.Shown |> ignore

        member _.Hide() =
            if state <> WindowState.Hidden then
                state <- WindowState.Hidden
                emitWindowEvent (createEventContext id) WindowEvent.Hidden |> ignore

        member _.Activate() =
            if activation <> ActivationState.Active then
                activation <- ActivationState.Active
                emitWindowEvent (createEventContext id) WindowEvent.Activated |> ignore

        member _.Deactivate() =
            if activation <> ActivationState.Inactive then
                activation <- ActivationState.Inactive
                emitWindowEvent (createEventContext id) WindowEvent.Deactivated |> ignore

        member _.SetFocused() =
            if focus <> FocusState.Focused then
                focus <- FocusState.Focused
                emitWindowEvent (createEventContext id) WindowEvent.FocusGained |> ignore

        member _.SetUnfocused() =
            if focus <> FocusState.Unfocused then
                focus <- FocusState.Unfocused
                emitWindowEvent (createEventContext id) WindowEvent.FocusLost |> ignore

        member _.RequestClose() =
            let context = emitWindowEvent (createEventContext id) WindowEvent.CloseRequested

            if not context.Cancel then
                state <- WindowState.Closing

            context

        member _.Close() =
            if state <> WindowState.Closed then
                state <- WindowState.Closed
                activation <- ActivationState.Inactive
                focus <- FocusState.Unfocused
                emitWindowEvent (createEventContext id) WindowEvent.Closed |> ignore

        member _.EnterPointer(pointerId: PointerId, position: PointerPosition) =
            pointer <-
                { pointer with
                    Position = position
                    IsInside = true }

            createPointerEvent pointerId position None None
            |> emitPointerEvent PointerEventKind.Entered
            |> ignore

        member _.LeavePointer(pointerId: PointerId, position: PointerPosition) =
            pointer <-
                { pointer with
                    Position = position
                    IsInside = false }

            createPointerEvent pointerId position None None
            |> emitPointerEvent PointerEventKind.Exited
            |> ignore

        member _.MovePointer(pointerId: PointerId, position: PointerPosition) =
            pointer <- { pointer with Position = position }
            createPointerEvent pointerId position None None
            |> emitPointerEvent PointerEventKind.Moved
            |> ignore

        member _.PressPointer(pointerId: PointerId, button: PointerButton, position: PointerPosition) =
            pointer <- { pointer with Position = position }
            createPointerEvent pointerId position (Some button) None
            |> emitPointerEvent PointerEventKind.Pressed
            |> ignore

        member _.ReleasePointer(pointerId: PointerId, button: PointerButton, position: PointerPosition) =
            pointer <- { pointer with Position = position }
            createPointerEvent pointerId position (Some button) None
            |> emitPointerEvent PointerEventKind.Released
            |> ignore

        member _.ScrollPointer(pointerId: PointerId, position: PointerPosition, delta: PointerDelta) =
            pointer <- { pointer with Position = position }
            createPointerEvent pointerId position None (Some delta)
            |> emitPointerEvent PointerEventKind.Wheel
            |> ignore

        member _.CapturePointer() =
            pointer <-
                { pointer with
                    CapturedBy = Some id }

        member _.ReleasePointerCapture() =
            pointer <-
                { pointer with
                    CapturedBy = None }

        member _.RaiseKeyDown(key: string, ctrl: bool, shift: bool, alt: bool) =
            createKeyboardEvent (Some key) None ctrl shift alt
            |> emitKeyboardEvent KeyboardEventKind.KeyDown
            |> ignore

        member _.RaiseKeyUp(key: string, ctrl: bool, shift: bool, alt: bool) =
            createKeyboardEvent (Some key) None ctrl shift alt
            |> emitKeyboardEvent KeyboardEventKind.KeyUp
            |> ignore

        member _.RaiseTextInput(text: string) =
            createKeyboardEvent None (Some text) false false false
            |> emitKeyboardEvent KeyboardEventKind.TextInput
            |> ignore
