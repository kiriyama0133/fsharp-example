module Dep

open System.Collections.Generic
open ReactiveTypes

type Dep() =

    let effects = HashSet<IReactiveEffect>(HashIdentity.Reference)

    member _.Effects = effects

    member _.Add(effect: IReactiveEffect) = effects.Add(effect) |> ignore

    member _.Remove(effect: IReactiveEffect) = effects.Remove(effect) |> ignore

    member _.Contains(effect: IReactiveEffect) = effects.Contains(effect)

    member _.Clear() = effects.Clear()
