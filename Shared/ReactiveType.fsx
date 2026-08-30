module ReactiveTypes

type IReactiveEffect =
    abstract AddDep: obj -> unit
    abstract Running: int
    abstract Scheduler: (unit -> unit) option
    abstract Run: unit -> unit
    abstract Stop: unit -> unit
