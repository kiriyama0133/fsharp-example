#load "../Shared/ReactiveType.fsx"

open System.Collections.Generic
open ReactiveTypes

module Dep =

    type Dep() =

        let effects = HashSet<IReactiveEffect>(HashIdentity.Reference)

        member _.Effects = effects

        member _.Add(effect: IReactiveEffect) = effects.Add(effect)

        member _.Remove(effect: IReactiveEffect) = effects.Remove(effect)

        member _.Contains(effect: IReactiveEffect) = effects.Contains(effect)

        member _.Clear() = effects.Clear()
