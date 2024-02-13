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

		private protected override bool IsAnimatableProperty(string propertyName)
		{
			return propertyName is nameof(Color);
		}

		private protected override void SetAnimatableProperty(string propertyName, object? propertyValue)
		{
			if (propertyName is nameof(Color))
			{
				Color = ValidateValue<Color>(propertyValue);
			}
			else
			{
				throw new Exception($"Unable to set property '{propertyName}' on {this}");
			}
		}
	}
}
