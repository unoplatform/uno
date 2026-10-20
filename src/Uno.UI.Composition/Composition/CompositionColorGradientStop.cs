#nullable enable

using System;
using Windows.UI;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionColorGradientStop : CompositionObject
	{
		private float _offset;
		private Color _color;

		internal CompositionColorGradientStop(Compositor compositor)
			: base(compositor)
		{

		}

		public float Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}

		public Color Color
		{
			get => _color;
			set => SetProperty(ref _color, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				return Offset;
			}
			else if (propertyName.Equals(nameof(Color), StringComparison.OrdinalIgnoreCase))
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
			if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				Offset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(Color), StringComparison.OrdinalIgnoreCase))
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
