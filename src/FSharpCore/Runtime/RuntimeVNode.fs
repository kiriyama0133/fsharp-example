module RuntimeVNode
    open RuntimeTypes

    let emptyProps: Props = Map.empty

    let emptySlots: Slots = Map.empty

    let private tryFindKey (props: Props) =
        props |> Map.tryFind "key"

    let create (nodeType: VNodeType) (props: Props option) (children: VNodeChildren) =
        let actualProps = defaultArg props emptyProps

        { El = None
          Anchor = None
          Component = None
          Type = nodeType
          Props = actualProps
          Children = children
          Key = tryFindKey actualProps }

    let text content = create Text None (TextChildren content)

    let fragment children = create Fragment None (ArrayChildren children)

    let element tag props children = create (Element tag) props children

    let componentNode definition props children = create (Component definition) props children

    let isSameVNode (left: VNode) (right: VNode) =
        let sameType =
            match left.Type, right.Type with
            | Text, Text -> true
            | Fragment, Fragment -> true
            | Element leftTag, Element rightTag -> leftTag = rightTag
            | Component leftComponent, Component rightComponent -> obj.ReferenceEquals(leftComponent, rightComponent)
            | _ -> false

        sameType && left.Key = right.Key
