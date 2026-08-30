#load "../Common/Ref.fsx"

module RuntimeTypes =
    open System.Collections.Generic
    open Effect.Effect
    open ReactiveTypes

    type Props = Map<string, obj>

    type LifecycleHook = unit -> unit

    type Slot = obj -> VNode list

    and Slots = Map<string, Slot>

    and VNodeType =
        | Element of string
        | Text
        | Fragment
        | Component of ComponentDefinition

    and VNodeChildren =
        | NoChildren
        | TextChildren of string
        | ArrayChildren of VNode list
        | SlotChildren of Slots

    and VNode =
        { mutable El: obj option
          mutable Anchor: obj option
          mutable Component: ComponentInstance option
          Type: VNodeType
          Props: Props
          Children: VNodeChildren
          Key: obj option }

    and SetupResult =
        | SetupBindings of Props
        | SetupRender of (RenderContext -> VNode)

    and SetupContext =
        { Attrs: Props
          Slots: Slots
          Emit: string -> obj array -> unit
          Expose: Props -> unit }

    and ComponentDefinition =
        { Name: string option
          Setup: (Props -> SetupContext -> SetupResult) option
          Render: (RenderContext -> VNode) option
          Data: (unit -> obj) option
          BeforeMount: LifecycleHook option
          Mounted: LifecycleHook option
          BeforeUpdate: LifecycleHook option
          Updated: LifecycleHook option
          BeforeUnmount: LifecycleHook option
          Unmounted: LifecycleHook option }

    and LifecycleCollection =
        { mutable BeforeMount: LifecycleHook list
          mutable Mounted: LifecycleHook list
          mutable BeforeUpdate: LifecycleHook list
          mutable Updated: LifecycleHook list
          mutable BeforeUnmount: LifecycleHook list
          mutable Unmounted: LifecycleHook list }

    and ComponentInstance =
        { mutable VNode: VNode
          mutable Parent: ComponentInstance option
          mutable Props: Props
          mutable Attrs: Props
          mutable Slots: Slots
          mutable Data: IReactiveObject option
          mutable SetupState: Props
          mutable Render: (RenderContext -> VNode) option
          mutable SubTree: VNode option
          mutable IsMounted: bool
          mutable IsUnmounted: bool
          mutable Update: (unit -> unit) option
          mutable Effect: ReactiveEffect option
          mutable Exposed: Props
          mutable Emit: string -> obj array -> unit
          Provides: Dictionary<obj, obj>
          Hooks: LifecycleCollection }

    and RenderContext =
        { Instance: ComponentInstance
          TryGet: string -> obj option
          Set: string -> obj -> bool
          Emit: string -> obj array -> unit
          Attrs: Props
          Slots: Slots
          Props: Props }

    type PatchFunction = VNode option -> VNode -> obj -> obj option -> ComponentInstance option -> unit

    type UnmountFunction = VNode -> unit

    type RendererInternals =
        { Patch: PatchFunction
          Unmount: UnmountFunction }

    type HostOperations =
        { CreateElement: string -> obj
          CreateText: string -> obj
          SetElementText: obj -> string -> unit
          SetText: obj -> string -> unit
          Insert: obj -> obj -> obj option -> unit
          Remove: obj -> unit
          PatchProp: obj -> string -> obj option -> obj option -> unit
          NextSibling: obj -> obj option
          QueryTarget: string -> obj option }

    type RenderRoot =
        { Container: obj
          mutable Current: VNode option }
