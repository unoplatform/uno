#nullable enable

using System;
using Windows.UI;

using static Windows.UI.Composition.SubPropertyHelpers;

namespace Windows.UI.Composition
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

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(Color), StringComparison.OrdinalIgnoreCase))
			{
				return GetColor(subPropertyName, Color);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(Color), StringComparison.OrdinalIgnoreCase))
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
