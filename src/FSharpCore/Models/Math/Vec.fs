module Vec

[<Struct>]
type Point2D =
    { x: float
      y: float }

    static member (+)(p1: Point2D, p2: Point2D) = { x = p1.x + p2.x; y = p1.y + p2.y }
    static member (-)(p1: Point2D, p2: Point2D) = { x = p1.x - p2.x; y = p1.y - p2.y }
    static member (*)(p: Point2D, scalar: float) = { x = p.x * scalar; y = p.y * scalar }
    static member (*)(scalar: float, p: Point2D) = p * scalar

    member this.Length() =
        sqrt (this.x * this.x + this.y * this.y)

[<Struct>]
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

    static member (*)(p: Point3D, scalar: float) =
        { x = p.x * scalar
          y = p.y * scalar
          z = p.z * scalar }

    static member (*)(scalar: float, p: Point3D) = p * scalar

    member this.Length() =
        sqrt (this.x * this.x + this.y * this.y + this.z * this.z)
