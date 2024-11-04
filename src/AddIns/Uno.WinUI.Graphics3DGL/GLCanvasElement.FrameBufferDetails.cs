using System;
using Windows.Foundation;
using Silk.NET.OpenGL;

namespace Uno.WinUI.Graphics3DGL;

public abstract partial class GLCanvasElement
{
	private class FrameBufferDetails : IDisposable
	{
		private readonly GL _gl;
		private readonly uint _textureColorBuffer;
		private readonly uint _renderBuffer;

		public uint Framebuffer { get; }

		public unsafe FrameBufferDetails(GL gl, Size renderSize)
		{
			_gl = gl;

			Framebuffer = gl.GenBuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, Framebuffer);
			{
				_textureColorBuffer = gl.GenTexture();
				gl.BindTexture(GLEnum.Texture2D, _textureColorBuffer);
				{
					gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgb, (uint)renderSize.Width, (uint)renderSize.Height, 0, GLEnum.Rgb,
						GLEnum.UnsignedByte, (void*)0);
					gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
					gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
					gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0,
						GLEnum.Texture2D, _textureColorBuffer, 0);
				}
				gl.BindTexture(GLEnum.Texture2D, 0);

				_renderBuffer = gl.GenRenderbuffer();
				gl.BindRenderbuffer(GLEnum.Renderbuffer, _renderBuffer);
				{
					gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)renderSize.Width, (uint)renderSize.Width);
					gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment,
						GLEnum.Renderbuffer, _renderBuffer);
				}
				gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

				if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
				{
					throw new InvalidOperationException("Offscreen framebuffer is not complete");
				}
			}
			gl.BindFramebuffer(GLEnum.Framebuffer, 0);
		}

		public void Dispose()
		{
			_gl.DeleteFramebuffer(Framebuffer);
			_gl.DeleteTexture(_textureColorBuffer);
			_gl.DeleteRenderbuffer(_renderBuffer);
		}
	}
}
