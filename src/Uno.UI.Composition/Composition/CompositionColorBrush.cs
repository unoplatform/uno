#nullable enable

using System;
using Windows.UI;

using static Microsoft.UI.Composition.SubPropertyHelpers;
using System.Collections.Generic;
using SkiaSharp;
using Uno.Disposables;

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

#nullable disable
		// We don't call SKPaint.Reset() after usage, so make sure
		// that only SKPaint.Color is being set
		private static readonly SKPaint _tempPaint = new() { IsAntialias = true };

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			_tempPaint.Color = Color.ToSKColor(opacity);
			canvas.DrawRect(bounds, _tempPaint);
		}

		internal override bool CanPaint() => Color != Colors.Transparent;
#nullable enable
	}
}
