#load "../Dep.fsx"
module TargetMap =

    open System.Collections.Generic
    open Dep.Dep

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
