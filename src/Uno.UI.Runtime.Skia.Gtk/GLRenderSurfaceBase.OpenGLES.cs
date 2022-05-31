#nullable enable

using System;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace Uno.UI.Runtime.Skia
{
	internal abstract partial class GLRenderSurfaceBase
	{
		protected GL _glES = null!;

		private void ClearOpenGLES()
		{
			ValidateOpenGLES();

			_glES.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);
			_glES.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
		}

		private void FlushOpenGLES()
		{
			ValidateOpenGLES();

			_glES.Flush();
		}

		[Conditional("DEBUG")]
		private void ValidateOpenGLES()
		{
			if (_gl == null)
			{
				throw new InvalidOperationException($"_glES cannot be null");
			}
		}
	}
}
