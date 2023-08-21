using System;
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Uno.UI.Composition
{
	internal interface ISkiaSurface
	{
		internal SKSurface? Surface { get; }
		internal void UpdateSurface(bool recreateSurface = false);
		internal void UpdateSurface(in DrawingSession session);
	}
}
