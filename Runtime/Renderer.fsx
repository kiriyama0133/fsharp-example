#load "./Component.fsx"
#load "./VNode.fsx"

module RuntimeRenderer =
    open Component.RuntimeComponent
    open Types.RuntimeTypes
    open VNode.RuntimeVNode

    type Renderer =
        { CreateRoot: obj -> RenderRoot
          Render: VNode option -> RenderRoot -> unit }

    let private patchProps host (oldProps: Props) (newProps: Props) element =
        let oldKeys = oldProps |> Seq.map (fun pair -> pair.Key) |> Set.ofSeq
        let newKeys = newProps |> Seq.map (fun pair -> pair.Key) |> Set.ofSeq
        let allKeys = Set.union oldKeys newKeys

        for key in allKeys do
            let previousValue = oldProps |> Map.tryFind key
            let nextValue = newProps |> Map.tryFind key

            if previousValue <> nextValue then
                host.PatchProp element key previousValue nextValue

    let createRenderer (host: HostOperations) =
        let rec mountChildren children container parentComponent anchor =
            for child in children do
                patch None child container anchor parentComponent

        and unmountChildren children =
            for child in children do
                unmount child

        and mountElement vnode container anchor parentComponent =
            match vnode.Type with
            | Element tag ->
                let element = host.CreateElement tag
                vnode.El <- Some element

                for KeyValue(key, value) in vnode.Props do
                    host.PatchProp element key None (Some value)

                match vnode.Children with
                | TextChildren text -> host.SetElementText element text
                | ArrayChildren children -> mountChildren children element parentComponent None
                | NoChildren
                | SlotChildren _ -> ()

                host.Insert element container anchor
            | _ -> invalidArg "vnode" "Expected an element vnode."

        and patchChildren previousVNode nextVNode container parentComponent anchor =
            match previousVNode.Children, nextVNode.Children with
            | TextChildren previousText, TextChildren nextText ->
                if previousText <> nextText then
                    host.SetElementText container nextText

            | TextChildren _, ArrayChildren nextChildren ->
                host.SetElementText container ""
                mountChildren nextChildren container parentComponent anchor

            | TextChildren _, NoChildren ->
                host.SetElementText container ""

            | ArrayChildren previousChildren, TextChildren nextText ->
                unmountChildren previousChildren
                host.SetElementText container nextText

            | ArrayChildren previousChildren, ArrayChildren nextChildren ->
                let commonLength = min previousChildren.Length nextChildren.Length

                for index in 0 .. commonLength - 1 do
                    patch (Some previousChildren.[index]) nextChildren.[index] container None parentComponent

                if nextChildren.Length > previousChildren.Length then
                    for index in commonLength .. nextChildren.Length - 1 do
                        patch None nextChildren.[index] container anchor parentComponent
                elif previousChildren.Length > nextChildren.Length then
                    for index in commonLength .. previousChildren.Length - 1 do
                        unmount previousChildren.[index]

            | ArrayChildren previousChildren, NoChildren ->
                unmountChildren previousChildren

            | NoChildren, TextChildren nextText ->
                host.SetElementText container nextText

            | NoChildren, ArrayChildren nextChildren ->
                mountChildren nextChildren container parentComponent anchor

            | NoChildren, NoChildren ->
                ()

            | SlotChildren _, _
            | _, SlotChildren _ ->
                ()

        and patchElement previousVNode nextVNode parentComponent =
            match previousVNode.El with
            | None -> ()
            | Some element ->
                nextVNode.El <- Some element
                patchProps host previousVNode.Props nextVNode.Props element
                patchChildren previousVNode nextVNode element parentComponent None

        and processElement previousVNode nextVNode container anchor parentComponent =
            match previousVNode with
            | None -> mountElement nextVNode container anchor parentComponent
            | Some previous -> patchElement previous nextVNode parentComponent

        and processText previousVNode nextVNode container anchor =
            match previousVNode with
            | None ->
                let textNode =
                    match nextVNode.Children with
                    | TextChildren text -> host.CreateText text
                    | _ -> host.CreateText ""

                nextVNode.El <- Some textNode
                host.Insert textNode container anchor
            | Some previous ->
                nextVNode.El <- previous.El

                match previous.El, previous.Children, nextVNode.Children with
                | Some textNode, TextChildren previousText, TextChildren nextText when previousText <> nextText ->
                    host.SetText textNode nextText
                | _ -> ()

        and processFragment previousVNode nextVNode container anchor parentComponent =
            match previousVNode with
            | None ->
                match nextVNode.Children with
                | ArrayChildren children -> mountChildren children container parentComponent anchor
                | TextChildren textValue ->
                    let textChild = text textValue
                    patch None textChild container anchor parentComponent
                    nextVNode.El <- textChild.El
                | NoChildren
                | SlotChildren _ -> ()
            | Some previous ->
                patchChildren previous nextVNode container parentComponent anchor

        and patch previousVNode nextVNode container anchor parentComponent =
            match previousVNode with
            | Some previous when not (isSameVNode previous nextVNode) ->
                unmount previous
                patch None nextVNode container anchor parentComponent
            | _ ->
                match nextVNode.Type with
                | Text -> processText previousVNode nextVNode container anchor
                | Fragment -> processFragment previousVNode nextVNode container anchor parentComponent
                | Element _ -> processElement previousVNode nextVNode container anchor parentComponent
                | Component _ -> processComponent previousVNode nextVNode container anchor parentComponent internals

        and unmount vnode =
            match vnode.Type with
            | Component _ ->
                match vnode.Component with
                | Some instance -> unmountComponent instance internals
                | None -> ()

            | Fragment ->
                match vnode.Children with
                | ArrayChildren children -> unmountChildren children
                | _ -> ()

            | Element _ ->
                match vnode.Children with
                | ArrayChildren children -> unmountChildren children
                | _ -> ()

                vnode.El |> Option.iter host.Remove
                vnode.El <- None

            | Text ->
                vnode.El |> Option.iter host.Remove
                vnode.El <- None

        and internals =
            { Patch = patch
              Unmount = unmount }

        let createRoot container =
            { Container = container
              Current = None }

        let render vnode root =
            match vnode with
            | None ->
                root.Current |> Option.iter unmount
                root.Current <- None
            | Some nextVNode ->
                patch root.Current nextVNode root.Container None None
                root.Current <- Some nextVNode

        { CreateRoot = createRoot
          Render = render }
