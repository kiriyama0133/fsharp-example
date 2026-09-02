module OpenGLBuffer

#nowarn "9"

open System
open System.Threading
open Silk.NET.OpenGL
open OpenGLResources

type Buffer internal (device: DeviceHandle, handle: uint32, description: BufferDescription) as this =
    let mutable disposeState = 0

    let throwIfDisposed () =
        if disposeState <> 0 then
            raise (ObjectDisposedException(nameof Buffer))

    let release () =
        if Interlocked.Exchange(&disposeState, 1) = 0 then
            WithCurrentContext device (fun gl -> gl.DeleteBuffer(handle))

    let ensureCapacity offset size =
        let endOffset = offset + size

        if endOffset > description.Size then
            invalidArg
                (nameof size)
                $"Buffer write exceeds the allocated size. Requested {endOffset} bytes, capacity is {description.Size} bytes."

    member _.Handle = handle

    member _.Description = description

    member _.Size = description.Size

    member _.Usage = description.Usage

    member _.Target = BufferTargetARB.ArrayBuffer

    member _.IsDisposed = disposeState <> 0

    member _.Bind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl -> gl.BindBuffer(BufferTargetARB.ArrayBuffer, handle))

    member _.Unbind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl -> gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0u))

    member _.SetSubData(offset: uint64, size: uint64, data: voidptr) =
        throwIfDisposed ()
        ensureCapacity offset size

        WithCurrentContext device (fun gl ->
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, handle)
            gl.BufferSubData(BufferTargetARB.ArrayBuffer, nativeint (int64 offset), unativeint size, data))

    override _.Finalize() = release ()

    interface IDisposable with
        member _.Dispose() =
            release ()
            GC.SuppressFinalize(this)

let CreateBuffer (device: DeviceHandle) (description: BufferDescription) : Buffer =
    if description.Size = 0UL then
        invalidArg (nameof description) "Buffer size must be greater than zero."

    let handle =
        WithCurrentContext device (fun gl ->
            let handle = gl.GenBuffer()
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, handle)

            gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                unativeint description.Size,
                NullData,
                ToBufferUsageARB description.Usage
            )

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0u)
            handle)

    new Buffer(device, handle, description)
