module GraphicsTypes

open Vec

type Color =
    { R: float
      G: float
      B: float
      A: float }

type Size = { Width: float; Height: float }

type Rect = { Position: Point2D; Size: Size }

type ImageId = ImageId of int

type FontId = FontId of int

type Image = { Id: ImageId; Size: Size }
type Brush = SolidColor of Color
type Sroke = { Color: Color; Width: float }

type Canvas =
    { Clear: Color -> unit
      DrawRect: Rect -> uint
      DrawLine: Point2D -> Point2D -> Sroke -> unit
      DrawImage: Image -> Rect -> unit }


type Font =
    { Id: FontId
      Family: string
      Size: float }

type RenderTarget =
    | WindowTarget
    | TextureTarget of ImageId

type Primitive =
    | Rectangle of Rect * Color
    | Image of ImageId * Rect
    | Text of string * FontId * Point2D * Color
