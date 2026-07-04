#nullable enable

using System;
using System.Numerics;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		private Vector2 _size;
		private Vector2 _offset;

		internal CompositionRectangleGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Size
		{
			get => _size;
			set => SetProperty(ref _size, value);
		}

		public Vector2 Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(Size), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, Size);
			}
			else if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, Offset);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(Size), StringComparison.OrdinalIgnoreCase))
			{
				Size = UpdateVector2(subPropertyName, Size, propertyValue);
			}
			else if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				Offset = UpdateVector2(subPropertyName, Offset, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
