#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Uno_UI_Runtime;

[TestClass]
public sealed class Given_GLRetainedFramebuffer
{
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void When_Blit_Then_WholeRetainedFrameIsCopiedAndScissorRestored(bool scissorEnabled)
	{
		using var gl = new FakeGl();
		using var framebuffer = new GLRetainedFramebuffer(gl.GetProcAddress);
		Assert.IsTrue(framebuffer.TryResize(2, 2, out var preserved));
		Assert.IsFalse(preserved);
		Assert.IsTrue(framebuffer.TryResize(2, 2, out preserved));
		Assert.IsTrue(preserved);
		gl.ScissorEnabled = scissorEnabled;

		framebuffer.BlitToDefault();

		CollectionAssert.AreEqual(gl.RetainedPixels, gl.DefaultPixels);
		Assert.AreEqual((0, 0, 2, 2, 0, 0, 2, 2, 0x4000u, 0x2600u), gl.BlitArguments);
		Assert.AreEqual(framebuffer.FramebufferId, gl.ReadFramebufferAtBlit);
		Assert.AreEqual(0u, gl.DrawFramebufferAtBlit);
		Assert.AreEqual(0u, gl.ReadFramebuffer);
		Assert.AreEqual(0u, gl.DrawFramebuffer);
		Assert.AreEqual(scissorEnabled, gl.ScissorEnabled);
	}

	// The loader exercises the real GL delegates; the blit models a 1x1 active scissor on a 2x2 target.
	private sealed unsafe class FakeGl : IDisposable
	{
		[ThreadStatic]
		private static FakeGl? _current;
		private readonly nint _version = Marshal.StringToHGlobalAnsi("OpenGL ES 3.0");
		private uint _nextName;
		public byte[] RetainedPixels { get; } = [1, 2, 3, 4];
		public byte[] DefaultPixels { get; } = new byte[4];
		public bool ScissorEnabled { get; set; }
		public uint ReadFramebuffer { get; private set; }
		public uint DrawFramebuffer { get; private set; }
		public uint ReadFramebufferAtBlit { get; private set; }
		public uint DrawFramebufferAtBlit { get; private set; }
		public (int, int, int, int, int, int, int, int, uint, uint) BlitArguments { get; private set; }

		public FakeGl() => _current = this;

		public nint GetProcAddress(string name) => name switch
		{
			"glGetString" => (nint)(delegate* unmanaged<uint, nint>)&GetString,
			"glIsEnabled" => (nint)(delegate* unmanaged<uint, byte>)&IsEnabled,
			"glEnable" => (nint)(delegate* unmanaged<uint, void>)&Enable,
			"glDisable" => (nint)(delegate* unmanaged<uint, void>)&Disable,
			"glGenFramebuffers" or "glGenTextures" or "glGenRenderbuffers" => (nint)(delegate* unmanaged<int, uint*, void>)&Generate,
			"glDeleteFramebuffers" or "glDeleteTextures" or "glDeleteRenderbuffers" => (nint)(delegate* unmanaged<int, uint*, void>)&Delete,
			"glBindFramebuffer" => (nint)(delegate* unmanaged<uint, uint, void>)&BindFramebuffer,
			"glBindTexture" or "glBindRenderbuffer" => (nint)(delegate* unmanaged<uint, uint, void>)&Bind,
			"glTexImage2D" => (nint)(delegate* unmanaged<uint, int, int, int, int, int, uint, uint, nint, void>)&TexImage,
			"glTexParameteri" => (nint)(delegate* unmanaged<uint, uint, int, void>)&TexParameter,
			"glFramebufferTexture2D" => (nint)(delegate* unmanaged<uint, uint, uint, uint, int, void>)&FramebufferTexture,
			"glRenderbufferStorage" => (nint)(delegate* unmanaged<uint, uint, int, int, void>)&RenderbufferStorage,
			"glFramebufferRenderbuffer" => (nint)(delegate* unmanaged<uint, uint, uint, uint, void>)&FramebufferRenderbuffer,
			"glCheckFramebufferStatus" => (nint)(delegate* unmanaged<uint, uint>)&CheckFramebuffer,
			"glBlitFramebuffer" => (nint)(delegate* unmanaged<int, int, int, int, int, int, int, int, uint, uint, void>)&Blit,
			_ => 0,
		};

		[UnmanagedCallersOnly]
		private static nint GetString(uint name) => _current!._version;
		[UnmanagedCallersOnly]
		private static byte IsEnabled(uint capability) => (byte)(capability == 0x0C11 && _current!.ScissorEnabled ? 1 : 0);
		[UnmanagedCallersOnly]
		private static void Enable(uint capability)
		{
			if (capability == 0x0C11)
			{
				_current!.ScissorEnabled = true;
			}
		}
		[UnmanagedCallersOnly]
		private static void Disable(uint capability)
		{
			if (capability == 0x0C11)
			{
				_current!.ScissorEnabled = false;
			}
		}
		[UnmanagedCallersOnly]
		private static void Generate(int count, uint* names)
		{
			for (var i = 0; i < count; i++)
			{
				names[i] = ++_current!._nextName;
			}
		}
		[UnmanagedCallersOnly]
		private static void Delete(int count, uint* names)
		{
		}
		[UnmanagedCallersOnly]
		private static void Bind(uint target, uint name)
		{
		}
		[UnmanagedCallersOnly]
		private static void BindFramebuffer(uint target, uint name)
		{
			var gl = _current!;
			if (target is 0x8D40 or 0x8CA8)
			{
				gl.ReadFramebuffer = name;
			}
			if (target is 0x8D40 or 0x8CA9)
			{
				gl.DrawFramebuffer = name;
			}
		}
		[UnmanagedCallersOnly]
		private static void TexImage(uint target, int level, int format, int width, int height, int border, uint pixelFormat, uint type, nint pixels)
		{
		}
		[UnmanagedCallersOnly]
		private static void TexParameter(uint target, uint name, int value)
		{
		}
		[UnmanagedCallersOnly]
		private static void FramebufferTexture(uint target, uint attachment, uint textureTarget, uint texture, int level)
		{
		}
		[UnmanagedCallersOnly]
		private static void RenderbufferStorage(uint target, uint format, int width, int height)
		{
		}
		[UnmanagedCallersOnly]
		private static void FramebufferRenderbuffer(uint target, uint attachment, uint renderbufferTarget, uint renderbuffer)
		{
		}
		[UnmanagedCallersOnly]
		private static uint CheckFramebuffer(uint target) => 0x8CD5;
		[UnmanagedCallersOnly]
		private static void Blit(int sx0, int sy0, int sx1, int sy1, int dx0, int dy0, int dx1, int dy1, uint mask, uint filter)
		{
			var gl = _current!;
			gl.BlitArguments = (sx0, sy0, sx1, sy1, dx0, dy0, dx1, dy1, mask, filter);
			gl.ReadFramebufferAtBlit = gl.ReadFramebuffer;
			gl.DrawFramebufferAtBlit = gl.DrawFramebuffer;
			Array.Copy(gl.RetainedPixels, gl.DefaultPixels, gl.ScissorEnabled ? 1 : gl.RetainedPixels.Length);
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal(_version);
			_current = null;
		}
	}
}
