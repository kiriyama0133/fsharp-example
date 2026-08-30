module ReactiveContext =

    let mutable activeEffect: ReactiveTypes.IReactiveEffect option = None
