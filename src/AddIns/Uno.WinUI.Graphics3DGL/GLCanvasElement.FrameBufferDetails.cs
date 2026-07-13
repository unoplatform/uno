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

			Framebuffer = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, Framebuffer);
			{
				_textureColorBuffer = gl.GenTexture();
				gl.BindTexture(GLEnum.Texture2D, _textureColorBuffer);
				{
					gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgb, (uint)renderSize.Width, (uint)renderSize.Height, 0, GLEnum.Rgb,
						GLEnum.UnsignedByte, (void*)0);
					// Use the scalar glTexParameteri (TexParameter with an int) rather than the
					// integer-texture glTexParameterIuiv (TexParameterI). Min/mag filters are plain
					// enum parameters, and glTexParameterIuiv is GLES 3.1+/desktop-only - it is absent
					// from Apple's OpenGL ES 3.0, where resolving it throws and aborts framebuffer setup.
					gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
					gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
					gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0,
						GLEnum.Texture2D, _textureColorBuffer, 0);
				}
				gl.BindTexture(GLEnum.Texture2D, 0);

				_renderBuffer = gl.GenRenderbuffer();
				gl.BindRenderbuffer(GLEnum.Renderbuffer, _renderBuffer);
				{
					gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)renderSize.Width, (uint)renderSize.Height);
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
