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

		private protected override bool IsAnimatableProperty(ReadOnlySpan<char> propertyName)
		{
			if (propertyName is nameof(Color))
			{
				return true;
			}

			return base.IsAnimatableProperty(propertyName);
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName is nameof(Color))
			{
				Color = UpdateColor(subPropertyName, Color, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
