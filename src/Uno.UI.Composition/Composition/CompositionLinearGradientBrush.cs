#nullable enable

using System;
using System.Numerics;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLinearGradientBrush : CompositionGradientBrush
	{
		private Vector2 _startPoint = Vector2.Zero;
		private Vector2 _endPoint = new Vector2(1, 0);

		internal CompositionLinearGradientBrush(Compositor compositor)
			: base(compositor)
		{

		}

		public Vector2 StartPoint
		{
			get => _startPoint;
			set => SetProperty(ref _startPoint, value);
		}

		public Vector2 EndPoint
		{
			get => _endPoint;
			set => SetProperty(ref _endPoint, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(StartPoint), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, StartPoint);
			}
			else if (propertyName.Equals(nameof(EndPoint), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, EndPoint);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(StartPoint), StringComparison.OrdinalIgnoreCase))
			{
				StartPoint = UpdateVector2(subPropertyName, StartPoint, propertyValue);
			}
			else if (propertyName.Equals(nameof(EndPoint), StringComparison.OrdinalIgnoreCase))
			{
				EndPoint = UpdateVector2(subPropertyName, EndPoint, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
