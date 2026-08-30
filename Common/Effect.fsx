#load "../Shared/ReactiveType.fsx"
#load "./Dep.fsx"

open ReactiveTypes
open Dep

module Effect =
    type ReactiveEffect(fn: unit -> unit, ?scheduler: unit -> unit) =

        let mutable executeCount = 0
        let mutable active = true
        let mutable running = 0

        let deps = ResizeArray<Dep.Dep>()

        member _.Fn = fn

        member _.Scheduler = scheduler

        member _.ExecuteCount = executeCount

        member _.Active = active

        member _.Running = running

        member _.Deps = deps

        member _.AddDep(dep: Dep.Dep) =
            if not (deps.Contains dep) then
                deps.Add dep

        member this.Run() =
            executeCount <- executeCount + 1

            if not active then
                fn ()
            else
                this.Cleanup()

                // TODO:
                // activeEffect <- this

                try
                    running <- running + 1
                    fn ()
                finally
                    running <- running - 1
        // TODO:
        // restore activeEffect

        member this.Cleanup() =
            let effect = this :> IReactiveEffect

            for dep in deps do
                dep.Remove effect

            deps.Clear()

        member this.Stop() =
            if active then
                this.Cleanup()
                active <- false

        interface IReactiveEffect with
            member this.Run() = this.Run()

            member this.Stop() = this.Stop()
