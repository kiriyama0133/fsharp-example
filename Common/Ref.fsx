#load "./Reactive/Reactivity.fsx"

module Ref =
    open Dep.Dep
    open ReactiveTypes
    open ReactiveContext
    open Tracking.Tracking
    open Reactivity.Reactive

    type RefValue<'T>(initialValue: 'T) =
        let mutable rawValue = initialValue
        let mutable currentValue = initialValue
        let mutable dep: Dep option = None

        member _.RawValue = rawValue

        member _.Value
            with get () =
                match ReactiveContext.activeEffect with
                | None -> ()
                | Some effect ->
                    let currentDep =
                        match dep with
                        | Some currentDep -> currentDep
                        | None ->
                            let createdDep = Dep()
                            dep <- Some createdDep
                            createdDep

                    if not (currentDep.Contains effect) then
                        currentDep.Add effect
                        effect.AddDep(currentDep :> obj)

                currentValue
            and set newValue =
                rawValue <- newValue
                currentValue <- newValue

                match dep with
                | Some currentDep -> TriggerEffects currentDep
                | None -> ()

        interface IRef with
            member this.ValueObject
                with get () = box this.Value
                and set newValue = this.Value <- unbox<'T> newValue

        interface IRef<'T> with
            member this.Value
                with get () = this.Value
                and set newValue = this.Value <- newValue

    type ObjectRef<'T>(reactiveObject: IReactiveObject, key: string) =
        member _.ReactiveObject = reactiveObject
        member _.Key = key

        member _.Value
            with get () = reactiveObject.GetValue(key) :?> 'T
            and set newValue = reactiveObject.SetValue(key, box newValue)

        interface IRef with
            member this.ValueObject
                with get () = box this.Value
                and set newValue = this.Value <- unbox<'T> newValue

        interface IRef<'T> with
            member this.Value
                with get () = this.Value
                and set newValue = this.Value <- newValue

    let private ensureReactiveObject (source: obj) =
        match source with
        | :? IReactiveObject as reactiveObject -> reactiveObject
        | _ -> reactive source

    let ref (value: 'T) : IRef<'T> =
        RefValue<'T>(value) :> IRef<'T>

    let isRef (value: obj) =
        match value with
        | :? IRef -> true
        | _ -> false

    let toRef<'T> (source: obj) (key: string) : IRef<'T> =
        let reactiveObject = ensureReactiveObject source
        ObjectRef<'T>(reactiveObject, key) :> IRef<'T>

    let toRefs (source: obj) =
        let reactiveObject = ensureReactiveObject source
        reactiveObject.PropertyNames
        |> Seq.map (fun key -> key, (ObjectRef<obj>(reactiveObject, key) :> IRef))
        |> Map.ofSeq
