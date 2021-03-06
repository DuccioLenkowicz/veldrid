﻿using System;
using OpenTK.Graphics;
using Veldrid.Platform;
using OpenTK.Graphics.ES30;
using System.Runtime.InteropServices;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESRenderContext : RenderContext
    {
        private readonly GraphicsContext _openGLGraphicsContext;
        private readonly OpenGLESDefaultFramebuffer _defaultFramebuffer;
        private PrimitiveType _primitiveType = PrimitiveType.Triangles;
        private int _vertexAttributesBound;
        private bool _vertexLayoutChanged;
        private int _baseVertexOffset = 0;

        public DebugSeverity MinimumLogSeverity { get; set; } = DebugSeverity.DebugSeverityLow;

        public OpenGLESRenderContext(OpenTKWindow window) : base(window)
        {
            ResourceFactory = new OpenGLESResourceFactory();
            RenderCapabilities = new RenderCapabilities(false, false);
            _openGLGraphicsContext = new GraphicsContext(GraphicsMode.Default, window.OpenTKWindowInfo, 2, 0, GraphicsContextFlags.Embedded);
            _openGLGraphicsContext.MakeCurrent(window.OpenTKWindowInfo);
            _openGLGraphicsContext.LoadAll();

            _defaultFramebuffer = new OpenGLESDefaultFramebuffer(Window);

            SetInitialStates();
            OnWindowResized();

            PostContextCreated();
        }

        public override ResourceFactory ResourceFactory { get; }

        public override RgbaFloat ClearColor
        {
            get
            {
                return base.ClearColor;
            }
            set
            {
                base.ClearColor = value;
                Color4 openTKColor = RgbaFloat.ToOpenTKColor(value);
                GL.ClearColor(openTKColor);
                Utilities.CheckLastGLES3Error();
            }
        }

        protected override void PlatformClearBuffer()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Utilities.CheckLastGLES3Error();
        }

        protected override void PlatformSwapBuffers()
        {
            if (Window.Exists)
            {
                _openGLGraphicsContext.SwapBuffers();
            }
        }

        public override void DrawIndexedPrimitives(int count, int startingIndex)
        {
            SetBaseVertexOffset(0);
            PreDrawCommand();
            var elementsType = ((OpenGLESIndexBuffer)IndexBuffer).ElementsType;
            int indexSize = OpenGLESFormats.GetIndexFormatSize(elementsType);

            GL.DrawElements(_primitiveType, count, elementsType, new IntPtr(startingIndex * indexSize));
            Utilities.CheckLastGLES3Error();
        }

        public override void DrawIndexedPrimitives(int count, int startingIndex, int startingVertex)
        {
            SetBaseVertexOffset(startingVertex);
            PreDrawCommand();
            var elementsType = ((OpenGLESIndexBuffer)IndexBuffer).ElementsType;
            int indexSize = OpenGLESFormats.GetIndexFormatSize(elementsType);
            GL.DrawElements(_primitiveType, count, elementsType, new IntPtr(startingIndex * indexSize));
        }

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex)
        {
            SetBaseVertexOffset(0);
            PreDrawCommand();
            var elementsType = ((OpenGLESIndexBuffer)IndexBuffer).ElementsType;
            int indexSize = OpenGLESFormats.GetIndexFormatSize(elementsType);
            GL.DrawElementsInstanced(_primitiveType, indexCount, elementsType, new IntPtr(startingIndex * indexSize), instanceCount);
            Utilities.CheckLastGLES3Error();
        }

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex, int startingVertex)
        {
            SetBaseVertexOffset(startingVertex);
            PreDrawCommand();
            var elementsType = ((OpenGLESIndexBuffer)IndexBuffer).ElementsType;
            int indexSize = OpenGLESFormats.GetIndexFormatSize(elementsType);
            GL.DrawElementsInstanced(
                _primitiveType,
                indexCount,
                elementsType,
                new IntPtr(startingIndex * indexSize),
                instanceCount);
        }

        private void SetBaseVertexOffset(int offset)
        {
            if (_baseVertexOffset != offset)
            {
                _baseVertexOffset = offset;
                _vertexLayoutChanged = true;
            }
        }

        private void SetInitialStates()
        {
            GL.ClearColor(ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
            Utilities.CheckLastGLES3Error();
            GL.Enable(EnableCap.CullFace);
            Utilities.CheckLastGLES3Error();
            GL.FrontFace(FrontFaceDirection.Cw);
            Utilities.CheckLastGLES3Error();
        }

        protected override void PlatformResize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Documentation indicates that this needs to be called on OSX for proper behavior.
                _openGLGraphicsContext.Update(((OpenTKWindow)Window).OpenTKWindowInfo);
            }
        }

        protected override void PlatformSetViewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
            Utilities.CheckLastGLES3Error();
        }

        protected override void PlatformSetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            _primitiveType = OpenGLESFormats.ConvertPrimitiveTopology(primitiveTopology);
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
            SetFramebuffer(_defaultFramebuffer);
        }

        protected override void PlatformSetScissorRectangle(System.Drawing.Rectangle rectangle)
        {
            GL.Enable(EnableCap.ScissorTest);
            Utilities.CheckLastGLES3Error();
            GL.Scissor(
                rectangle.Left,
                Window.Height - rectangle.Bottom,
                rectangle.Width,
                rectangle.Height);
        }

        public override void ClearScissorRectangle()
        {
            GL.Disable(EnableCap.ScissorTest);
            Utilities.CheckLastGLES3Error();
        }

        protected override void PlatformSetVertexBuffer(int slot, VertexBuffer vb)
        {
            _vertexLayoutChanged = true;
        }

        protected override void PlatformSetIndexBuffer(IndexBuffer ib)
        {
            ((OpenGLESIndexBuffer)ib).Apply();
            _vertexLayoutChanged = true;
        }

        protected override void PlatformSetShaderSet(ShaderSet shaderSet)
        {
            OpenGLESShaderSet glShaderSet = (OpenGLESShaderSet)shaderSet;
            GL.UseProgram(glShaderSet.ProgramID);
            Utilities.CheckLastGLES3Error();
            _vertexLayoutChanged = true;
        }

        protected override void PlatformSetShaderConstantBindings(ShaderConstantBindings shaderConstantBindings)
        {
            shaderConstantBindings.Apply();
        }

        protected override void PlatformSetShaderTextureBindingSlots(ShaderTextureBindingSlots bindingSlots)
        {
        }

        protected override void PlatformSetTexture(int slot, ShaderTextureBinding textureBinding)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            Utilities.CheckLastGLES3Error();
            ((OpenGLESTexture)textureBinding.BoundTexture).Bind();
            int uniformLocation = ShaderTextureBindingSlots.GetUniformLocation(slot);
            GL.Uniform1(uniformLocation, slot);
            Utilities.CheckLastGLES3Error();
        }

        protected override void PlatformSetFramebuffer(Framebuffer framebuffer)
        {
            OpenGLESFramebufferBase baseFramebuffer = (OpenGLESFramebufferBase)framebuffer;
            if (baseFramebuffer is OpenGLESFramebuffer)
            {
                OpenGLESFramebuffer glFramebuffer = (OpenGLESFramebuffer)baseFramebuffer;
                if (!glFramebuffer.HasDepthAttachment || !DepthStencilState.IsDepthEnabled)
                {
                    GL.Disable(EnableCap.DepthTest);
                    Utilities.CheckLastGLES3Error();
                    GL.DepthMask(false);
                    Utilities.CheckLastGLES3Error();
                }
                else
                {
                    GL.Enable(EnableCap.DepthTest);
                    Utilities.CheckLastGLES3Error();
                    GL.DepthMask(DepthStencilState.IsDepthWriteEnabled);
                    Utilities.CheckLastGLES3Error();
                }
            }

            baseFramebuffer.Apply();
        }

        protected override void PlatformSetBlendstate(BlendState blendState)
        {
            ((OpenGLESBlendState)blendState).Apply();
        }

        protected override void PlatformSetDepthStencilState(DepthStencilState depthStencilState)
        {
            ((OpenGLESDepthStencilState)depthStencilState).Apply();
        }

        protected override void PlatformSetRasterizerState(RasterizerState rasterizerState)
        {
            ((OpenGLESRasterizerState)rasterizerState).Apply();
        }

        protected override void PlatformClearMaterialResourceBindings()
        {
        }

        public override RenderCapabilities RenderCapabilities { get; }

        protected override void PlatformDispose()
        {
            _openGLGraphicsContext.Dispose();
        }

        protected override System.Numerics.Vector2 GetTopLeftUvCoordinate()
        {
            return new System.Numerics.Vector2(0, 1);
        }

        protected override System.Numerics.Vector2 GetBottomRightUvCoordinate()
        {
            return new System.Numerics.Vector2(1, 0);
        }

        private void PreDrawCommand()
        {
            if (_vertexLayoutChanged)
            {
                _vertexAttributesBound = ((OpenGLESVertexInputLayout)ShaderSet.InputLayout).SetVertexAttributes(VertexBuffers, _vertexAttributesBound, _baseVertexOffset);
                _vertexLayoutChanged = false;
            }
        }

        private new OpenGLESTextureBindingSlots ShaderTextureBindingSlots => (OpenGLESTextureBindingSlots)base.ShaderTextureBindingSlots;
    }
}
