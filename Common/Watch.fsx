#load "./Ref.fsx"

module Watch =
    open System.Collections.Generic
    open System.Threading
    open Effect.Effect
    open ReactiveTypes
    open Ref.Ref
    open Reactivity.Reactive

    type FlushMode =
        | Pre
        | Post
        | Sync

    type WatchOptions =
        { Immediate: bool
          Deep: bool
          Flush: FlushMode }

    type WatchHandle = unit -> unit

    let defaultWatchOptions =
        { Immediate = false
          Deep = false
          Flush = Sync }

    let private schedule flushMode (job: unit -> unit) =
        match flushMode with
        | Sync
        | Pre -> job
        | Post ->
            fun () ->
                ThreadPool.QueueUserWorkItem(WaitCallback(fun _ -> job ()))
                |> ignore

    let rec private traverse (value: obj) (maxDepth: int option) (currentDepth: int) (seen: HashSet<obj>) =
        if isNull value then
            value
        else
            let valueType = value.GetType()

            if valueType.IsValueType || valueType = typeof<string> then
                value
            elif maxDepth |> Option.exists (fun depth -> currentDepth >= depth) then
                value
            elif not (seen.Add value) then
                value
            else
                match value with
                | :? IRef as refValue ->
                    traverse refValue.ValueObject maxDepth (currentDepth + 1) seen

                | :? IReactiveObject as reactiveObject ->
                    for key in reactiveObject.PropertyNames do
                        traverse (reactiveObject.GetValue key) maxDepth (currentDepth + 1) seen
                        |> ignore

                    value

                | _ ->
                    match toReactive value with
                    | :? IReactiveObject as reactiveObject ->
                        traverse (reactiveObject :> obj) maxDepth currentDepth seen

                    | _ ->
                        for property in valueType.GetProperties() do
                            if property.CanRead then
                                traverse (property.GetValue value) maxDepth (currentDepth + 1) seen
                                |> ignore

                        value

    let private runWatch (getter: unit -> 'T) (callback: 'T -> 'T option -> unit) (options: WatchOptions option) =
        let options = defaultArg options defaultWatchOptions
        let mutable oldValue: 'T option = None
        let mutable currentValue = Unchecked.defaultof<'T>
        let mutable effectOpt: ReactiveEffect option = None

        let runner () =
            currentValue <- getter ()

        let job () =
            match effectOpt with
            | None -> ()
            | Some effect ->
                effect.Run()
                let newValue = currentValue
                callback newValue oldValue
                oldValue <- Some newValue

        let scheduler = schedule options.Flush job
        let effect = ReactiveEffect(runner, scheduler = scheduler)
        effectOpt <- Some effect

        if options.Immediate then
            job ()
        else
            effect.Run()
            oldValue <- Some currentValue

        fun () -> effect.Stop()

    let private traverseDepth (deep: bool) =
        if deep then None else Some 1

    let watchGetter (getter: unit -> 'T) (callback: 'T -> 'T option -> unit) (options: WatchOptions option) : WatchHandle
        =
        let options = defaultArg options defaultWatchOptions

        let trackedGetter () =
            let value = getter ()

            if options.Deep then
                let seen = HashSet<obj>(HashIdentity.Reference)
                traverse (box value) None 0 seen |> ignore

            value

        runWatch trackedGetter callback (Some options)

    let watchRef (source: IRef<'T>) (callback: 'T -> 'T option -> unit) (options: WatchOptions option) : WatchHandle =
        let options = defaultArg options defaultWatchOptions

        let getter () =
            let value = source.Value

            if options.Deep then
                let seen = HashSet<obj>(HashIdentity.Reference)
                traverse (box value) None 0 seen |> ignore

            value

        runWatch getter callback (Some options)

    let watchProperty<'T> (source: obj) (key: string) (callback: 'T -> 'T option -> unit) (options: WatchOptions option) : WatchHandle =
        let propertyRef = toRef<'T> source key
        watchRef propertyRef callback options

    let watchReactive
        (source: IReactiveObject)
        (callback: IReactiveObject -> IReactiveObject option -> unit)
        (options: WatchOptions option)
        : WatchHandle =
        let options = defaultArg options defaultWatchOptions

        let getter () =
            let seen = HashSet<obj>(HashIdentity.Reference)
            traverse (source :> obj) (traverseDepth options.Deep) 0 seen |> ignore
            source

        runWatch getter callback (Some options)

    let watchEffect (fn: (((unit -> unit) -> unit) -> unit)) : WatchHandle =
        let mutable cleanup: (unit -> unit) option = None
        let mutable effectOpt: ReactiveEffect option = None

        let onCleanup callback =
            cleanup <- Some callback

        let runner () =
            cleanup |> Option.iter (fun callback -> callback ())
            cleanup <- None
            fn onCleanup

        let job () =
            match effectOpt with
            | Some effect -> effect.Run()
            | None -> ()

        let effect = ReactiveEffect(runner, scheduler = job)
        effectOpt <- Some effect
        effect.Run()

        fun () ->
            effect.Stop()
            cleanup |> Option.iter (fun callback -> callback ())
            cleanup <- None
