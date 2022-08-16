#nullable enable

using System;
using System.Diagnostics;
using Silk.NET.OpenGL;

namespace Uno.UI.Runtime.Skia
{
	internal abstract partial class GLRenderSurfaceBase
	{
		protected GL _gl = null!;

		private void ClearOpenGL()
		{
			ValidateOpenGL();

			_gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);
			_gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
		}

		private void FlushOpenGL()
		{
			ValidateOpenGL();

			_gl.Flush();
		}

		[Conditional("DEBUG")]
		private void ValidateOpenGL()
		{
			if (_gl == null)
			{
				throw new InvalidOperationException($"_gl cannot be null");
			}
		}
	}
}
