#load "./Scheduler.fsx"

module RuntimeComponent =
    open System
    open System.Collections.Generic
    open Effect.Effect
    open ReactiveTypes
    open Reactivity.Reactive
    open Scheduler.RuntimeScheduler
    open Types.RuntimeTypes

    let private emptyLifecycleCollection () =
        { BeforeMount = []
          Mounted = []
          BeforeUpdate = []
          Updated = []
          BeforeUnmount = []
          Unmounted = [] }

    let private getComponentDefinition (vnode: VNode) =
        match vnode.Type with
        | Component definition -> definition
        | _ -> invalidArg "vnode" "Expected a component vnode."

    let private unwrapPublicValue (value: obj) =
        match value with
        | null -> null
        | :? IRef as refValue -> refValue.ValueObject
        | _ -> value

    let private hasReactiveProperty (reactiveObject: IReactiveObject) key =
        reactiveObject.PropertyNames |> Seq.exists ((=) key)

    let private tryGetReactiveProperty (reactiveObject: IReactiveObject) key =
        if hasReactiveProperty reactiveObject key then
            Some(reactiveObject.GetValue key)
        else
            None

    let private copyProvides (parent: ComponentInstance option) =
        let provides = Dictionary<obj, obj>()

        match parent with
        | None -> ()
        | Some parentInstance ->
            for KeyValue(key, value) in parentInstance.Provides do
                provides.[key] <- value

        provides

    let private appendHook hook hooks =
        match hook with
        | Some hookValue -> hookValue :: hooks
        | None -> hooks

    let private registerDefinitionHooks (definition: ComponentDefinition) (instance: ComponentInstance) =
        instance.Hooks.BeforeMount <- appendHook definition.BeforeMount instance.Hooks.BeforeMount
        instance.Hooks.Mounted <- appendHook definition.Mounted instance.Hooks.Mounted
        instance.Hooks.BeforeUpdate <- appendHook definition.BeforeUpdate instance.Hooks.BeforeUpdate
        instance.Hooks.Updated <- appendHook definition.Updated instance.Hooks.Updated
        instance.Hooks.BeforeUnmount <- appendHook definition.BeforeUnmount instance.Hooks.BeforeUnmount
        instance.Hooks.Unmounted <- appendHook definition.Unmounted instance.Hooks.Unmounted

    let private toHandlerName (eventName: string) =
        if String.IsNullOrWhiteSpace eventName then
            "on"
        else
            "on" + string (Char.ToUpperInvariant eventName.[0]) + eventName.Substring(1)

    let createComponentInstance (vnode: VNode) (parent: ComponentInstance option) =
        let provides = copyProvides parent

        let instance =
            { VNode = vnode
              Parent = parent
              Props = vnode.Props
              Attrs = Map.empty
              Slots = Map.empty
              Data = None
              SetupState = Map.empty
              Render = None
              SubTree = None
              IsMounted = false
              IsUnmounted = false
              Update = None
              Effect = None
              Exposed = Map.empty
              Emit = fun _ _ -> ()
              Provides = provides
              Hooks = emptyLifecycleCollection () }

        let emit eventName args =
            let handlerName = toHandlerName eventName

            match instance.Props |> Map.tryFind handlerName with
            | Some (:? (obj array -> unit) as handler) -> handler args
            | Some (:? Action as handler) when args.Length = 0 -> handler.Invoke()
            | Some (:? Action<obj array> as handler) -> handler.Invoke(args)
            | Some (:? (unit -> unit) as handler) when args.Length = 0 -> handler ()
            | _ -> ()

        instance.Emit <- emit
        instance

    let mutable private currentInstance: ComponentInstance option = None

    let getCurrentInstance () = currentInstance

    let private setCurrentInstance instance =
        currentInstance <- instance

    let private createRenderContext (instance: ComponentInstance) =
        let tryGet key =
            match instance.SetupState |> Map.tryFind key with
            | Some value -> Some(unwrapPublicValue value)
            | None ->
                match instance.Data with
                | Some reactiveObject ->
                    match tryGetReactiveProperty reactiveObject key with
                    | Some value -> Some value
                    | None ->
                        match instance.Props |> Map.tryFind key with
                        | Some value -> Some value
                        | None ->
                            match key with
                            | "$attrs" -> Some(box instance.Attrs)
                            | "$slots" -> Some(box instance.Slots)
                            | "$emit" -> Some(box instance.Emit)
                            | _ -> None
                | None ->
                    match instance.Props |> Map.tryFind key with
                    | Some value -> Some value
                    | None ->
                        match key with
                        | "$attrs" -> Some(box instance.Attrs)
                        | "$slots" -> Some(box instance.Slots)
                        | "$emit" -> Some(box instance.Emit)
                        | _ -> None

        let setValue key value =
            match instance.SetupState |> Map.tryFind key with
            | Some (:? IRef as refValue) ->
                refValue.ValueObject <- value
                true
            | Some _ ->
                instance.SetupState <- instance.SetupState.Add(key, value)
                true
            | None ->
                match instance.Data with
                | Some reactiveObject when hasReactiveProperty reactiveObject key ->
                    reactiveObject.SetValue(key, value)
                    true
                | _ ->
                    if instance.Props |> Map.containsKey key then
                        false
                    else
                        false

        { Instance = instance
          TryGet = tryGet
          Set = setValue
          Emit = instance.Emit
          Attrs = instance.Attrs
          Slots = instance.Slots
          Props = instance.Props }

    let private invokeLifecycleHooks hooks =
        for hook in hooks do
            hook ()

    let setupComponent (instance: ComponentInstance) =
        let definition = getComponentDefinition instance.VNode
        registerDefinitionHooks definition instance

        instance.Slots <-
            match instance.VNode.Children with
            | SlotChildren slots -> slots
            | _ -> Map.empty

        instance.Render <- definition.Render

        match definition.Data with
        | Some initData ->
            let dataObject = initData ()
            instance.Data <- Some(reactive dataObject)
        | None -> ()

        match definition.Setup with
        | Some setup ->
            let setupContext =
                { Attrs = instance.Attrs
                  Slots = instance.Slots
                  Emit = instance.Emit
                  Expose = fun exposed -> instance.Exposed <- exposed }

            setCurrentInstance (Some instance)

            try
                match setup instance.Props setupContext with
                | SetupBindings bindings ->
                    instance.SetupState <- bindings
                | SetupRender render ->
                    instance.Render <- Some render
            finally
                setCurrentInstance None
        | None -> ()

        if instance.Render.IsNone then
            invalidOp "Component must provide a render function."

    let setupRenderEffect
        (instance: ComponentInstance)
        (vnode: VNode)
        container
        anchor
        (internals: RendererInternals)
        =
        let renderCurrentTree () =
            match instance.Render with
            | Some render -> render (createRenderContext instance)
            | None -> invalidOp "Component render function is missing."

        let componentUpdateFn () =
            if not instance.IsMounted then
                invokeLifecycleHooks instance.Hooks.BeforeMount

                let subTree = renderCurrentTree ()
                instance.SubTree <- Some subTree
                internals.Patch None subTree container anchor (Some instance)
                vnode.El <- subTree.El
                instance.IsMounted <- true

                invokeLifecycleHooks instance.Hooks.Mounted
            else
                invokeLifecycleHooks instance.Hooks.BeforeUpdate

                let nextTree = renderCurrentTree ()
                let previousTree = instance.SubTree

                instance.SubTree <- Some nextTree
                internals.Patch previousTree nextTree container anchor (Some instance)
                vnode.El <- nextTree.El

                invokeLifecycleHooks instance.Hooks.Updated

        let mutable update = fun () -> ()

        let effect =
            ReactiveEffect(
                componentUpdateFn,
                scheduler =
                    (fun () ->
                        queueJob update))

        instance.Effect <- Some effect

        update <-
            fun () ->
                if not instance.IsUnmounted then
                    effect.Run()

        instance.Update <- Some update
        update ()

    let unmountComponent (instance: ComponentInstance) (internals: RendererInternals) =
        if instance.IsUnmounted then
            ()
        else
            invokeLifecycleHooks instance.Hooks.BeforeUnmount
            instance.IsUnmounted <- true
            instance.Effect |> Option.iter (fun effect -> effect.Stop())

            match instance.SubTree with
            | Some subTree -> internals.Unmount subTree
            | None -> ()

            invokeLifecycleHooks instance.Hooks.Unmounted

    let private mountComponent vnode container anchor parent internals =
        let instance = createComponentInstance vnode parent
        vnode.Component <- Some instance
        setupComponent instance
        setupRenderEffect instance vnode container anchor internals

    let private updateComponent previousVNode nextVNode =
        match previousVNode.Component with
        | None -> invalidOp "Component instance is missing on previous vnode."
        | Some instance ->
            nextVNode.Component <- Some instance
            nextVNode.El <- previousVNode.El
            instance.VNode <- nextVNode
            instance.Props <- nextVNode.Props

            instance.Slots <-
                match nextVNode.Children with
                | SlotChildren slots -> slots
                | _ -> Map.empty

            instance.Update |> Option.iter (fun update -> update ())

    let processComponent n1 n2 container anchor parent internals =
        match n1 with
        | None -> mountComponent n2 container anchor parent internals
        | Some previousVNode -> updateComponent previousVNode n2
