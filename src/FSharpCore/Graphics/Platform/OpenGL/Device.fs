module OpenGLDevice

#nowarn "9"

open System
open System.Threading
open Silk.NET.OpenGL
open OpenGLTypes
open OpenGLResources
open OpenGLBuffer

type ShaderStage =
    | Vertex
    | Fragment

type TextureDimension =
    | Texture1D
    | Texture2D
    | Texture3D
    | TextureCube

type TextureFormat =
    | R8
    | RG8
    | RGB8
    | RGBA8
    | R16
    | RG16
    | RGBA16
    | R32Float
    | RG32Float
    | RGB32Float
    | RGBA32Float
    | Depth16
    | Depth24
    | Depth32Float
    | Depth24Stencil8
    | Depth32FloatStencil8

type ShaderSource = { Stage: ShaderStage; Source: string }

type ShaderDescription = { Stages: ShaderSource list }

type TextureDescription =
    { Dimension: TextureDimension
      Width: int
      Height: int
      Depth: int
      MipLevels: int
      Format: TextureFormat }

type Texture internal (device: DeviceHandle, handle: uint32, target: TextureTarget, description: TextureDescription) as this =
    let mutable disposeState = 0

    let throwIfDisposed () =
        if disposeState <> 0 then
            raise (ObjectDisposedException(nameof Texture))

    let release () =
        if Interlocked.Exchange(&disposeState, 1) = 0 then
            WithCurrentContext device (fun gl ->
                gl.DeleteTexture(handle))

    member _.Handle = handle

    member _.Target = target

    member _.Description = description

    member _.IsDisposed = disposeState <> 0

    member _.Bind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.BindTexture(target, handle))

    member _.Unbind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.BindTexture(target, 0u))

    member _.GenerateMipmaps() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.BindTexture(target, handle)
            gl.GenerateMipmap(target))

    override _.Finalize() =
        release ()

    interface IDisposable with
        member _.Dispose() =
            release ()
            GC.SuppressFinalize(this)

type FramebufferDescription =
    { ColorAttachments: Texture list
      DepthAttachment: Texture option
      StencilAttachment: Texture option }

type Shader internal (device: DeviceHandle, handle: uint32, description: ShaderDescription) as this =
    let mutable disposeState = 0

    let throwIfDisposed () =
        if disposeState <> 0 then
            raise (ObjectDisposedException(nameof Shader))

    let release () =
        if Interlocked.Exchange(&disposeState, 1) = 0 then
            WithCurrentContext device (fun gl ->
                gl.DeleteProgram(handle))

    member _.Handle = handle

    member _.Description = description

    member _.IsDisposed = disposeState <> 0

    member _.Use() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.UseProgram(handle))

    override _.Finalize() =
        release ()

    interface IDisposable with
        member _.Dispose() =
            release ()
            GC.SuppressFinalize(this)

type Framebuffer internal (device: DeviceHandle, handle: uint32, description: FramebufferDescription) as this =
    let mutable disposeState = 0

    let throwIfDisposed () =
        if disposeState <> 0 then
            raise (ObjectDisposedException(nameof Framebuffer))

    let release () =
        if Interlocked.Exchange(&disposeState, 1) = 0 then
            WithCurrentContext device (fun gl ->
                gl.DeleteFramebuffer(handle))

    member _.Handle = handle

    member _.Description = description

    member _.IsDisposed = disposeState <> 0

    member _.Bind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle))

    member _.Unbind() =
        throwIfDisposed ()

        WithCurrentContext device (fun gl ->
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0u))

    override _.Finalize() =
        release ()

    interface IDisposable with
        member _.Dispose() =
            release ()
            GC.SuppressFinalize(this)

let private toShaderType (stage: ShaderStage) =
    match stage with
    | ShaderStage.Vertex -> ShaderType.VertexShader
    | ShaderStage.Fragment -> ShaderType.FragmentShader

let private toTextureTarget (dimension: TextureDimension) =
    match dimension with
    | TextureDimension.Texture1D -> TextureTarget.Texture1D
    | TextureDimension.Texture2D -> TextureTarget.Texture2D
    | TextureDimension.Texture3D -> TextureTarget.Texture3D
    | TextureDimension.TextureCube -> TextureTarget.TextureCubeMap

let private toInternalFormat (format: TextureFormat) =
    match format with
    | TextureFormat.R8 -> InternalFormat.R8
    | TextureFormat.RG8 -> InternalFormat.RG8
    | TextureFormat.RGB8 -> InternalFormat.Rgb8
    | TextureFormat.RGBA8 -> InternalFormat.Rgba8
    | TextureFormat.R16 -> InternalFormat.R16
    | TextureFormat.RG16 -> InternalFormat.RG16
    | TextureFormat.RGBA16 -> InternalFormat.Rgba16
    | TextureFormat.R32Float -> InternalFormat.R32f
    | TextureFormat.RG32Float -> InternalFormat.RG32f
    | TextureFormat.RGB32Float -> InternalFormat.Rgb32f
    | TextureFormat.RGBA32Float -> InternalFormat.Rgba32f
    | TextureFormat.Depth16 -> InternalFormat.DepthComponent16
    | TextureFormat.Depth24 -> InternalFormat.DepthComponent24
    | TextureFormat.Depth32Float -> InternalFormat.DepthComponent32f
    | TextureFormat.Depth24Stencil8 -> InternalFormat.Depth24Stencil8
    | TextureFormat.Depth32FloatStencil8 -> InternalFormat.Depth32fStencil8

let private toPixelFormat (format: TextureFormat) =
    match format with
    | TextureFormat.R8
    | TextureFormat.R16
    | TextureFormat.R32Float -> PixelFormat.Red
    | TextureFormat.RG8
    | TextureFormat.RG16
    | TextureFormat.RG32Float -> PixelFormat.RG
    | TextureFormat.RGB8
    | TextureFormat.RGB32Float -> PixelFormat.Rgb
    | TextureFormat.RGBA8
    | TextureFormat.RGBA16
    | TextureFormat.RGBA32Float -> PixelFormat.Rgba
    | TextureFormat.Depth16
    | TextureFormat.Depth24
    | TextureFormat.Depth32Float -> PixelFormat.DepthComponent
    | TextureFormat.Depth24Stencil8
    | TextureFormat.Depth32FloatStencil8 -> PixelFormat.DepthStencil

let private toPixelType (format: TextureFormat) =
    match format with
    | TextureFormat.R8
    | TextureFormat.RG8
    | TextureFormat.RGB8
    | TextureFormat.RGBA8 -> PixelType.UnsignedByte
    | TextureFormat.R16
    | TextureFormat.RG16
    | TextureFormat.RGBA16
    | TextureFormat.Depth16 -> PixelType.UnsignedShort
    | TextureFormat.R32Float
    | TextureFormat.RG32Float
    | TextureFormat.RGB32Float
    | TextureFormat.RGBA32Float
    | TextureFormat.Depth32Float -> PixelType.Float
    | TextureFormat.Depth24 -> PixelType.UnsignedInt
    | TextureFormat.Depth24Stencil8 -> PixelType.UnsignedInt248
    | TextureFormat.Depth32FloatStencil8 -> PixelType.Float32UnsignedInt248Rev

let private getMipExtent level value =
    let mipValue = value >>> level
    if mipValue = 0u then 1u else mipValue

let private asGLEnum (value: TextureTarget) =
    enum<GLEnum>(int value)

let private validateTextureDescription (description: TextureDescription) =
    if description.Width <= 0 then
        invalidArg (nameof description.Width) "Texture width must be greater than zero."

    if description.MipLevels <= 0 then
        invalidArg (nameof description.MipLevels) "Texture mip level count must be greater than zero."

    match description.Dimension with
    | TextureDimension.Texture1D -> ()
    | TextureDimension.Texture2D ->
        if description.Height <= 0 then
            invalidArg (nameof description.Height) "2D texture height must be greater than zero."
    | TextureDimension.Texture3D ->
        if description.Height <= 0 then
            invalidArg (nameof description.Height) "3D texture height must be greater than zero."

        if description.Depth <= 0 then
            invalidArg (nameof description.Depth) "3D texture depth must be greater than zero."
    | TextureDimension.TextureCube ->
        if description.Height <= 0 then
            invalidArg (nameof description.Height) "Cube texture height must be greater than zero."

        if description.Width <> description.Height then
            invalidArg (nameof description) "Cube textures require identical width and height."

let private createTexture (device: DeviceHandle) (description: TextureDescription) =
    validateTextureDescription description

    let target = toTextureTarget description.Dimension

    let handle =
        WithCurrentContext device (fun gl ->
            let handle = gl.GenTexture()
            let internalFormat = toInternalFormat description.Format
            let pixelFormat = toPixelFormat description.Format
            let pixelType = toPixelType description.Format

            gl.BindTexture(target, handle)

            for level in 0 .. description.MipLevels - 1 do
                let width = getMipExtent level (uint32 description.Width)
                let height = getMipExtent level (uint32 (max 1 description.Height))
                let depth = getMipExtent level (uint32 (max 1 description.Depth))

                match description.Dimension with
                | TextureDimension.Texture1D ->
                    gl.TexImage1D(asGLEnum target, level, int internalFormat, width, 0, pixelFormat, pixelType, NullData)
                | TextureDimension.Texture2D ->
                    gl.TexImage2D(asGLEnum target, level, int internalFormat, width, height, 0, pixelFormat, pixelType, NullData)
                | TextureDimension.Texture3D ->
                    gl.TexImage3D(asGLEnum target, level, int internalFormat, width, height, depth, 0, pixelFormat, pixelType, NullData)
                | TextureDimension.TextureCube ->
                    for face in 0 .. 5 do
                        let faceTarget = enum<GLEnum>(int TextureTarget.TextureCubeMapPositiveX + face)
                        gl.TexImage2D(faceTarget, level, int internalFormat, width, height, 0, pixelFormat, pixelType, NullData)

            let minFilter =
                if description.MipLevels > 1 then
                    int TextureMinFilter.LinearMipmapLinear
                else
                    int TextureMinFilter.Linear

            gl.TexParameter(target, TextureParameterName.TextureBaseLevel, 0)
            gl.TexParameter(target, TextureParameterName.TextureMaxLevel, description.MipLevels - 1)
            gl.TexParameter(target, TextureParameterName.TextureMinFilter, minFilter)
            gl.TexParameter(target, TextureParameterName.TextureMagFilter, int TextureMagFilter.Linear)
            gl.TexParameter(target, TextureParameterName.TextureWrapS, int TextureWrapMode.ClampToEdge)

            if description.Dimension <> TextureDimension.Texture1D then
                gl.TexParameter(target, TextureParameterName.TextureWrapT, int TextureWrapMode.ClampToEdge)

            if description.Dimension = TextureDimension.Texture3D
               || description.Dimension = TextureDimension.TextureCube then
                gl.TexParameter(target, TextureParameterName.TextureWrapR, int TextureWrapMode.ClampToEdge)

            gl.BindTexture(target, 0u)
            handle)

    new Texture(device, handle, target, description)

let private compileShaderStage (gl: GL) (source: ShaderSource) =
    let shaderHandle = gl.CreateShader(toShaderType source.Stage)
    gl.ShaderSource(shaderHandle, source.Source)
    gl.CompileShader(shaderHandle)

    let mutable compileStatus = 0
    gl.GetShader(shaderHandle, ShaderParameterName.CompileStatus, &compileStatus)

    if compileStatus = 0 then
        let infoLog = gl.GetShaderInfoLog(shaderHandle)
        gl.DeleteShader(shaderHandle)
        failwith $"Failed to compile {source.Stage} shader: {infoLog}"

    shaderHandle

let private createShader (device: DeviceHandle) (description: ShaderDescription) =
    if List.isEmpty description.Stages then
        invalidArg (nameof description.Stages) "At least one shader stage is required."

    let handle =
        WithCurrentContext device (fun gl ->
            let programHandle = gl.CreateProgram()
            let stageHandles = description.Stages |> List.map (compileShaderStage gl)

            try
                for stageHandle in stageHandles do
                    gl.AttachShader(programHandle, stageHandle)

                gl.LinkProgram(programHandle)

                let mutable linkStatus = 0
                gl.GetProgram(programHandle, ProgramPropertyARB.LinkStatus, &linkStatus)

                if linkStatus = 0 then
                    let infoLog = gl.GetProgramInfoLog(programHandle)
                    gl.DeleteProgram(programHandle)
                    failwith $"Failed to link shader program: {infoLog}"

                for stageHandle in stageHandles do
                    gl.DetachShader(programHandle, stageHandle)
                    gl.DeleteShader(stageHandle)

                programHandle
            with ex ->
                for stageHandle in stageHandles do
                    gl.DeleteShader(stageHandle)

                reraise ())

    new Shader(device, handle, description)

let private createFramebuffer (device: DeviceHandle) (description: FramebufferDescription) =
    if List.isEmpty description.ColorAttachments
       && Option.isNone description.DepthAttachment
       && Option.isNone description.StencilAttachment then
        invalidArg (nameof description) "Framebuffer requires at least one attachment."

    let handle =
        WithCurrentContext device (fun gl ->
            let handle = gl.GenFramebuffer()
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle)

            description.ColorAttachments
            |> List.iteri (fun index texture ->
                gl.FramebufferTexture(
                    FramebufferTarget.Framebuffer,
                    enum<FramebufferAttachment>(int FramebufferAttachment.ColorAttachment0 + index),
                    texture.Handle,
                    0
                ))

            description.DepthAttachment
            |> Option.iter (fun texture ->
                gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, texture.Handle, 0))

            description.StencilAttachment
            |> Option.iter (fun texture ->
                gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, texture.Handle, 0))

            let status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer)
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0u)

            if status <> GLEnum.FramebufferComplete then
                gl.DeleteFramebuffer(handle)
                failwith $"Framebuffer is incomplete: {status}"

            handle)

    new Framebuffer(device, handle, description)

type GraphicsDevice =
    inherit IDisposable
    abstract Context: OpenGLContext
    abstract GL: GL
    abstract MakeCurrent: unit -> unit
    abstract SwapBuffers: unit -> unit
    abstract CreateBuffer: BufferDescription -> Buffer
    abstract CreateTexture: TextureDescription -> Texture
    abstract CreateShader: ShaderDescription -> Shader
    abstract CreateFramebuffer: FramebufferDescription -> Framebuffer

type Device(context: OpenGLContext) as this =
    let mutable disposeState = 0

    let handle =
        context.MakeCurrent()
        let getProcAddress = Func<string, nativeint>(context.GetProcAddress)
        let gl = GL.GetApi(getProcAddress)
        { Context = context; GL = gl }

    let throwIfDisposed () =
        if disposeState <> 0 then
            raise (ObjectDisposedException(nameof Device))

    let release () =
        if Interlocked.Exchange(&disposeState, 1) = 0 then
            context.Dispose()

    member _.Handle = handle

    member _.Context = handle.Context

    member _.GL = handle.GL

    member _.IsDisposed = disposeState <> 0

    member _.MakeCurrent() =
        throwIfDisposed ()
        MakeCurrent handle

    member _.SwapBuffers() =
        throwIfDisposed ()
        SwapBuffers handle

    member _.CreateBuffer(description: BufferDescription) =
        throwIfDisposed ()
        OpenGLBuffer.CreateBuffer handle description

    member _.CreateTexture(description: TextureDescription) =
        throwIfDisposed ()
        createTexture handle description

    member _.CreateShader(description: ShaderDescription) =
        throwIfDisposed ()
        createShader handle description

    member _.CreateFramebuffer(description: FramebufferDescription) =
        throwIfDisposed ()
        createFramebuffer handle description

    interface GraphicsDevice with
        member _.Context = handle.Context
        member _.GL = handle.GL
        member _.MakeCurrent() = this.MakeCurrent()
        member _.SwapBuffers() = this.SwapBuffers()
        member _.CreateBuffer(description: BufferDescription) = this.CreateBuffer(description)
        member _.CreateTexture(description: TextureDescription) = this.CreateTexture(description)
        member _.CreateShader(description: ShaderDescription) = this.CreateShader(description)
        member _.CreateFramebuffer(description: FramebufferDescription) = this.CreateFramebuffer(description)

    interface IDisposable with
        member _.Dispose() =
            release ()

let CreateDevice (context: OpenGLContext) : Device =
    new Device(context)

let CreateGraphicsDevice (context: OpenGLContext) : GraphicsDevice =
    new Device(context) :> GraphicsDevice
