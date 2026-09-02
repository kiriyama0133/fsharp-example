module OpenGLResources

#nowarn "9"

open Silk.NET.OpenGL
open OpenGLTypes

type DeviceHandle = { Context: OpenGLContext; GL: GL }

type BufferUsage =
    | Static
    | Dynamic
    | Stream

type BufferDescription = { Size: uint64; Usage: BufferUsage }

let MakeCurrent (device: DeviceHandle) =
    device.Context.MakeCurrent()

let SwapBuffers (device: DeviceHandle) =
    device.Context.SwapBuffers()

let WithCurrentContext (device: DeviceHandle) (action: GL -> 'T) : 'T =
    MakeCurrent device
    action device.GL

let NullData: voidptr = Unchecked.defaultof<voidptr>

let ToBufferUsageARB (usage: BufferUsage) =
    match usage with
    | BufferUsage.Static -> BufferUsageARB.StaticDraw
    | BufferUsage.Dynamic -> BufferUsageARB.DynamicDraw
    | BufferUsage.Stream -> BufferUsageARB.StreamDraw
