open System

[<Struct>]
[<CustomEquality>]
[<CustomComparison>]
type Point2D =
    { x: float
      y: float }

    static member (+)(p1: Point2D, p2: Point2D) = { x = p1.x + p2.x; y = p1.y + p2.y }
    static member (-)(p1: Point2D, p2: Point2D) = { x = p1.x - p2.x; y = p1.y - p2.y }

    member this.Length() =
        sqrt (this.x * this.x + this.y * this.y)

    override this.Equals(obj: obj) : bool =
        match obj with
        | :? Point2D as p -> this.Length() = p.Length()
        | _ -> false

    override this.GetHashCode() : int = this.Length().GetHashCode()

    interface System.IComparable with
        member this.CompareTo(obj: obj) : int =
            match obj with
            | :? Point2D as p ->
                let len1 = this.Length()
                let len2 = p.Length()
                compare len1 len2
            | _ -> invalidArg "other" "Must be a Point"

[<Struct>]
[<CustomEquality>]
[<CustomComparison>]
type Point3D =
    { x: float
      y: float
      z: float }

    static member (+)(p1: Point3D, p2: Point3D) =
        { x = p1.x + p2.x
          y = p1.y + p2.y
          z = p1.z + p2.z }

    static member (-)(p1: Point3D, p2: Point3D) =
        { x = p1.x - p2.x
          y = p1.y - p2.y
          z = p1.z - p2.z }

    member this.Length() =
        sqrt (this.x * this.x + this.y * this.y + this.z * this.z)

    override this.Equals(obj: obj) : bool =
        match obj with
        | :? Point3D as p -> this.Length() = p.Length()
        | _ -> false

    override this.GetHashCode() : int = this.Length().GetHashCode()

    interface System.IComparable with
        member this.CompareTo(obj: obj) : int =
            match obj with
            | :? Point2D as p -> raise (ArgumentException "type p must be Point3D but Point2D")
            | :? Point3D as p ->
                let len1 = this.Length()
                let len2 = p.Length()
                compare len1 len2
            | _ -> raise (ArgumentException("Expected Point3D", "obj"))
