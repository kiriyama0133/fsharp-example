type Chicken =
    { Name: string
      Size: float }

    static member Create(name, size) = { Name = name; Size = size }

type Turkey = { Name: string; Size: float }

module Chicken =
    let getSize (c: Chicken) = c.Size
    let getName (c: Chicken) = c.Name

module Turkey =
    let create name size = { Name = name; Size = size }
    let getSize (t: Turkey) = t.Size
    let getName (t: Turkey) = t.Name

let c1 = Chicken.Create("kiriyama", 12.0)
printfn "name: %s, size: %f" c1.Name c1.Size
