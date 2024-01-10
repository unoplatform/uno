#nullable enable

using System;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorBrush : CompositionBrush
	{
		private Color _color;

		internal CompositionColorBrush(Compositor compositor) : base(compositor)
		{

		}

		public Color Color
		{
			get { return _color; }
			set { SetProperty(ref _color, value); }
		}
	}
}
