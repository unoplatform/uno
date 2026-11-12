#nullable enable

using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		private float _trimStart;
		private float _trimOffset;
		// Docs say 0, but WinUI reports and renders 1 (untrimmed).
		private float _trimEnd = 1f;

		internal CompositionGeometry(Compositor compositor) : base(compositor)
		{

		}

		public float TrimStart
		{
			get => _trimStart;
			set => SetProperty(ref _trimStart, ValidateTrim(value, nameof(TrimStart)));
		}

		public float TrimOffset
		{
			get => _trimOffset;
			set => SetProperty(ref _trimOffset, value);
		}

		public float TrimEnd
		{
			get => _trimEnd;
			set => SetProperty(ref _trimEnd, ValidateTrim(value, nameof(TrimEnd)));
		}

		private static float ValidateTrim(float value, string name)
			=> value is >= 0f and <= 1f
				? value
				: throw new ArgumentException($"Invalid argument to parameter {name}. Value must be in the range of 0.0f to 1.0f.", name);

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
				SetProperty(ref _trimStart, Math.Clamp(SubPropertyHelpers.ValidateValue<float>(propertyValue), 0f, 1f), nameof(TrimStart));
			}
			else if (propertyName.Equals(nameof(TrimOffset), StringComparison.OrdinalIgnoreCase))
			{
				TrimOffset = SubPropertyHelpers.ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(TrimEnd), StringComparison.OrdinalIgnoreCase))
			{
				SetProperty(ref _trimEnd, Math.Clamp(SubPropertyHelpers.ValidateValue<float>(propertyValue), 0f, 1f), nameof(TrimEnd));
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
