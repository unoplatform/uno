#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

internal interface ICanvasEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop, IDisposable
{
	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }
}
