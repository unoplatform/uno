using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

#nullable enable

namespace Microsoft.UI.Composition
{
	internal class TextVisual : Visual
	{
		private readonly TextBlock _owner;

		public TextVisual(Compositor compositor, TextBlock owner) : base(compositor)
		{
			_owner = owner;
		}

		internal override void Render(SKSurface surface)
		{
			_owner.Inlines.Render(surface, Compositor);
		}
	}
}
