module SkiaCanvas

open GraphicsTypes
open Vec

type Canvas =
    { Clear: Color -> unit
      DrawRect: Rect -> Brush -> unit
      DrawLine: Point2D -> Point2D -> Stroke -> unit
      DrawImage: Image -> Rect -> unit
      DrawText: string -> Point2D -> Font -> Color -> unit }
