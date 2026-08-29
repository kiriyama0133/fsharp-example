type Signal<'T>(initialValue: 'T) =
    let mutable currentValue = initialValue
    let changed = Event<'T>()

    member _.Value
        with get () = currentValue
        and set newValue =
            currentValue <- newValue
            changed.Trigger(newValue)

    member _.Changed = changed.Publish


let count = Signal 0

count.Changed.Add(fun value -> printfn "count changed: %d" value)

printfn "current = %d" count.Value

count.Value <- 1
count.Value <- 2
count.Value <- 3
