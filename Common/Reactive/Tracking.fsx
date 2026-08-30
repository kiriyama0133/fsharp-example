#load "../Effect.fsx"
#load "../Dep.fsx"
#load "./TargetMap.fsx"

module Tracking =

    open Dep.Dep
    open Effect.Effect
    open ReactiveTypes
    open TargetMap.TargetMap
    open TargetMap

    let mutable activeEffect: ReactiveEffect option = None

    let targetMap = TargetMap()

    let Track (target: obj) (key: string) =
        match activeEffect with
        | None -> ()

        | Some effect ->
            let effectRef = effect :> IReactiveEffect

            let properties = targetMap.GetOrCreate(target)

            let dep =
                match properties.TryGetValue(key) with
                | true, dep -> dep

                | false, _ ->
                    let dep = Dep()
                    properties.Add(key, dep)
                    dep

            if not (dep.Contains effectRef) then
                dep.Add effectRef
                effect.AddDep dep
