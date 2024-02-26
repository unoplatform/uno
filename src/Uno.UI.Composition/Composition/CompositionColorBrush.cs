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
			return propertyName is nameof(Color);
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName is nameof(Color) && subPropertyName.Length == 0)
			{
				Color = ValidateValue<Color>(propertyValue);
			}
			else if (propertyName is nameof(Color) && subPropertyName == "A")
			{
				var a = ValidateValue<byte>(propertyValue);
				Color = new Color(a, Color.R, Color.G, Color.B);
			}
			else if (propertyName is nameof(Color) && subPropertyName == "R")
			{
				var r = ValidateValue<byte>(propertyValue);
				Color = new Color(Color.A, r, Color.G, Color.B);
			}
			else if (propertyName is nameof(Color) && subPropertyName == "G")
			{
				var g = ValidateValue<byte>(propertyValue);
				Color = new Color(Color.A, Color.R, g, Color.B);
			}
			else if (propertyName is nameof(Color) && subPropertyName == "B")
			{
				var b = ValidateValue<byte>(propertyValue);
				Color = new Color(Color.A, Color.R, Color.G, b);
			}
			else
			{
				throw new Exception($"Unable to set property '{propertyName}' on {this}");
			}
		}
	}
}
