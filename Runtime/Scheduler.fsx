#load "./Types.fsx"

module RuntimeScheduler =
    open System
    open System.Collections.Generic
    open System.Threading

    let private gate = obj ()

    let private queue = ResizeArray<unit -> unit>()

    let mutable private isFlushPending = false

    let private containsJob (job: unit -> unit) =
        queue |> Seq.exists (fun existing -> obj.ReferenceEquals(existing, job))

    let flushJobs () =
        let jobs =
            lock gate (fun () ->
                isFlushPending <- false

                let copy = queue.ToArray()
                queue.Clear()
                copy)

        for job in jobs do
            job ()

    let queueJob (job: unit -> unit) =
        let shouldSchedule =
            lock gate (fun () ->
                if not (containsJob job) then
                    queue.Add job

                if isFlushPending then
                    false
                else
                    isFlushPending <- true
                    true)

        if shouldSchedule then
            ThreadPool.QueueUserWorkItem(WaitCallback(fun _ -> flushJobs ()))
            |> ignore
