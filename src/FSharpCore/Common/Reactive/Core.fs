module Core

module TargetMap =

    open System.Collections.Generic
    open Dep

    type TargetMap() =
        let targets = Dictionary<obj, Dictionary<string, Dep>>()

        member _.Get(target: obj, key: string) =

            let properties =
                match targets.TryGetValue(target) with
                | true, properties -> properties
                | false, _ ->
                    let properties = Dictionary<string, Dep>()
                    targets.Add(target, properties)
                    properties

            match properties.TryGetValue(key) with
            | true, dep -> dep
            | false, _ ->
                let dep = Dep()
                properties.Add(key, dep)
                dep

        member _.Set(target: obj, key: string, dep: Dep) =
            match targets.TryGetValue(target) with
            | true, properties -> properties.[key] <- dep
            | false, _ ->
                let properties = Dictionary<string, Dep>()
                properties.Add(key, dep)
                targets.Add(target, properties)

        member _.GetOrCreate(target: obj) =
            match targets.TryGetValue(target) with
            | true, properties -> properties
            | false, _ ->
                let properties = Dictionary<string, Dep>()
                targets.Add(target, properties)
                properties

        member _.TryGet(target: obj, key: string) =
            match targets.TryGetValue(target) with
            | false, _ -> None
            | true, properties ->
                match properties.TryGetValue(key) with
                | true, dep -> Some dep
                | false, _ -> None

module Tracking =

    open System
    open Dep
    open ReactiveContext
    open TargetMap

    let targetMap = TargetMap()

    let Track (target: obj) (key: string) =
        match ReactiveContext.activeEffect with
        | None -> ()
        | Some effect ->
            let properties = targetMap.GetOrCreate(target)

            let dep =
                match properties.TryGetValue(key) with
                | true, dep -> dep
                | false, _ ->
                    let dep = Dep()
                    properties.Add(key, dep)
                    dep

            if not (dep.Contains effect) then
                dep.Add effect
                effect.AddDep(dep :> obj)

    let TriggerEffects (dep: Dep) =
        let effects = dep.Effects |> Seq.toArray

        for effect in effects do
            match ReactiveContext.activeEffect with
            | Some active when Object.ReferenceEquals(effect, active) -> ()
            | _ ->
                match effect.Scheduler with
                | Some scheduler when effect.Running = 0 -> scheduler ()
                | Some _ -> ()
                | None -> effect.Run()

    let Trigger (target: obj) (key: string) =
        match targetMap.TryGet(target, key) with
        | None -> ()
        | Some dep -> TriggerEffects dep
