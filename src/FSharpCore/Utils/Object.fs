module Utils.Object

module Object =
    let isObject (obj: obj) =
        obj <> null && obj.GetType().IsClass && not (obj.GetType() = typeof<string>)
