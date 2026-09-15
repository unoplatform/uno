#nullable enable

using System;
using System.Numerics;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private Vector2 _gradientOriginOffset = Vector2.Zero;
		private Vector2 _ellipseRadius = new Vector2(0.5f, 0.5f);
		private Vector2 _ellipseCenter = new Vector2(0.5f, 0.5f);

		internal CompositionRadialGradientBrush(Compositor compositor)
			: base(compositor)
		{

		}

		public Vector2 GradientOriginOffset
		{
			get => _gradientOriginOffset;
			set => SetProperty(ref _gradientOriginOffset, value);
		}

		public Vector2 EllipseRadius
		{
			get => _ellipseRadius;
			set => SetProperty(ref _ellipseRadius, value);
		}

		public Vector2 EllipseCenter
		{
			get => _ellipseCenter;
			set => SetProperty(ref _ellipseCenter, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(EllipseCenter), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, EllipseCenter);
			}
			else if (propertyName.Equals(nameof(EllipseRadius), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, EllipseRadius);
			}
			else if (propertyName.Equals(nameof(GradientOriginOffset), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, GradientOriginOffset);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(EllipseCenter), StringComparison.OrdinalIgnoreCase))
			{
				EllipseCenter = UpdateVector2(subPropertyName, EllipseCenter, propertyValue);
			}
			else if (propertyName.Equals(nameof(EllipseRadius), StringComparison.OrdinalIgnoreCase))
			{
				EllipseRadius = UpdateVector2(subPropertyName, EllipseRadius, propertyValue);
			}
			else if (propertyName.Equals(nameof(GradientOriginOffset), StringComparison.OrdinalIgnoreCase))
			{
				GradientOriginOffset = UpdateVector2(subPropertyName, GradientOriginOffset, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
