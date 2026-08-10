#nullable enable

using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		private float _trimStart;
		private float _trimOffset;
		private float _trimEnd;

		internal CompositionGeometry(Compositor compositor) : base(compositor)
		{

		}

		public float TrimStart
		{
			get => _trimStart;
			set => SetProperty(ref _trimStart, value);
		}

		public float TrimOffset
		{
			get => _trimOffset;
			set => SetProperty(ref _trimOffset, value);
		}

		public float TrimEnd
		{
			get => _trimEnd;
			set => SetProperty(ref _trimEnd, value);
		}

		internal virtual IGeometrySource2D? BuildGeometry() => null;

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(TrimStart), StringComparison.OrdinalIgnoreCase))
			{
				return TrimStart;
			}
			else if (propertyName.Equals(nameof(TrimOffset), StringComparison.OrdinalIgnoreCase))
			{
				return TrimOffset;
			}
			else if (propertyName.Equals(nameof(TrimEnd), StringComparison.OrdinalIgnoreCase))
			{
				return TrimEnd;
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(TrimStart), StringComparison.OrdinalIgnoreCase))
			{
				TrimStart = SubPropertyHelpers.ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(TrimOffset), StringComparison.OrdinalIgnoreCase))
			{
				TrimOffset = SubPropertyHelpers.ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(TrimEnd), StringComparison.OrdinalIgnoreCase))
			{
				TrimEnd = SubPropertyHelpers.ValidateValue<float>(propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
