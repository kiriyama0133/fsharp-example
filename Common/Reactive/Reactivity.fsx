#load "./Tracking.fsx"

module Reactive =
    open System.Collections.Generic
    open Tracking.Tracking

    type ReactiveObject(target: obj) =

        member _.Raw = target

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
