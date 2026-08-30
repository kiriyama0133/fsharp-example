module ReactiveTypes

type IReactiveEffect =
    abstract AddDep: obj -> unit
    abstract Running: int
    abstract Scheduler: (unit -> unit) option
    abstract Run: unit -> unit
    abstract Stop: unit -> unit

type IReactiveObject =
    abstract Raw: obj
    abstract GetValue: string -> obj
    abstract SetValue: string * obj -> unit
    abstract PropertyNames: seq<string>

type IRef =
    abstract ValueObject: obj with get, set

type IRef<'T> =
    inherit IRef
    abstract Value: 'T with get, set
