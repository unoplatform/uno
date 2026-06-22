#nullable enable

using System;
using System.Numerics;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		private Vector2 _radius;
		private Vector2 _center;

		internal CompositionEllipseGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Radius
		{
			get => _radius;
			set => SetProperty(ref _radius, value);
		}

		public Vector2 Center
		{
			get => _center;
			set => SetProperty(ref _center, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(Radius), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, Radius);
			}
			else if (propertyName.Equals(nameof(Center), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, Center);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(Radius), StringComparison.OrdinalIgnoreCase))
			{
				Radius = UpdateVector2(subPropertyName, Radius, propertyValue);
			}
			else if (propertyName.Equals(nameof(Center), StringComparison.OrdinalIgnoreCase))
			{
				Center = UpdateVector2(subPropertyName, Center, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
