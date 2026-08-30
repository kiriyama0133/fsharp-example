#load "./Tracking.fsx"

module Reactive =
    open System.Collections.Generic
    open ReactiveTypes
    open Tracking.Tracking

    type ReactiveObject(target: obj) =

        member _.Raw = target

        member _.PropertyNames =
            target.GetType().GetProperties()
            |> Seq.filter (fun property -> property.CanRead)
            |> Seq.map (fun property -> property.Name)

        member _.Get(key: string) : obj =
            Track target key
            let property = target.GetType().GetProperty(key)

            if isNull property then
                invalidArg "key" ("Property '" + key + "' does not exist.")

            if not property.CanRead then
                invalidArg "key" ("Property '" + key + "' is not readable.")

            property.GetValue(target)

        member _.Set(key: string, value: obj) =
            let property = target.GetType().GetProperty(key)

            if isNull property then
                invalidArg "key" ("Property '" + key + "'does not exists")

            if not property.CanWrite then
                invalidArg "key" ("Property '" + key + "' is not writable.")

            property.SetValue(target, value)
            Trigger target key

        interface IReactiveObject with
            member this.Raw = this.Raw
            member this.GetValue(key: string) = this.Get(key)
            member this.SetValue(key: string, value: obj) = this.Set(key, value)
            member this.PropertyNames = this.PropertyNames

    type ReactiveMap() =
        let reactives = Dictionary<obj, ReactiveObject>()

        member _.Get(target: obj) : ReactiveObject option =
            match reactives.TryGetValue(target) with
            | true, reactive -> Some reactive
            | false, _ -> None

        member _.Set(target: obj, reactive: ReactiveObject) = reactives.[target] <- reactive

        member _.GetOrCreate(target: obj) =
            match reactives.TryGetValue(target) with
            | true, reactive -> reactive

            | false, _ ->
                let reactive = ReactiveObject(target)
                reactives.Add(target, reactive)
                reactive

    let reactiveMap = ReactiveMap()

    let reactive (target: obj) : IReactiveObject =
        reactiveMap.GetOrCreate(target) :> IReactiveObject

    let isReactive (value: obj) =
        match value with
        | :? IReactiveObject -> true
        | _ -> false

    let toReactive (value: obj) =
        if isNull value then
            null
        else
            let valueType = value.GetType()

            if valueType.IsClass && valueType <> typeof<string> then
                reactiveMap.GetOrCreate(value) :> obj
            else
                value
