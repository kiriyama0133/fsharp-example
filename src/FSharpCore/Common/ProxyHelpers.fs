module Common.ProxyHelpers

type Signal<'T>(initialValue: 'T) =
    let mutable currentValue = initialValue
    let changed = Event<'T>()

    member _.Value
        with get () = currentValue
        and set newValue =
            currentValue <- newValue
            changed.Trigger(newValue)

    member _.Changed = changed.Publish

let createSignal initialValue = Signal initialValue
