module ReactiveTypes

type IReactiveEffect =
    abstract Run: unit -> unit
    abstract Stop: unit -> unit
