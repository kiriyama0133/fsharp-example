#load "../Effect.fsx"
#load "../Dep.fsx"
#load "./TargetMap.fsx"
#load "./Tracking.fsx"

module Reactive =
    open System.Collections.Generic
    open Dep.Dep
    open Effect.Effect
    open Tracking.Tracking
    open TargetMap.TargetMap

    type ReactiveObject(target: obj) =

        member _.Raw = target

        member _.Get(key: string) : obj =
            Tracking.Tracking.Track target key


            let property = target.GetType().GetProperty(key)

            if isNull property then
                invalidArg "key" ("Property '" + key + "' does not exist.")

            null

        member _.Set(key: string, value: obj) =
            let property = target.GetType().GetProperty(key)

            if isNull property then
                invalidArg "key" ("Property '" + key + "'does not exists")

            // trigger(target, key)

            ()

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

    let mutable activeEffect: ReactiveEffect option = None
    let targetMap = TargetMap()
