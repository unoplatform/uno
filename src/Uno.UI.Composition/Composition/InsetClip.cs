#nullable enable

using System;

using static Windows.UI.Composition.SubPropertyHelpers;

namespace Windows.UI.Composition
{
	public partial class InsetClip : CompositionClip
	{
		private float _topInset;
		private float _rightInset;
		private float _leftInset;
		private float _bottomInset;

		internal InsetClip(Compositor compositor) : base(compositor)
		{

		}

		public float TopInset
		{
			get => _topInset;
			set => SetProperty(ref _topInset, value);
		}

		public float RightInset
		{
			get => _rightInset;
			set => SetProperty(ref _rightInset, value);
		}

		public float LeftInset
		{
			get => _leftInset;
			set => SetProperty(ref _leftInset, value);
		}

		public float BottomInset
		{
			get => _bottomInset;
			set => SetProperty(ref _bottomInset, value);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(BottomInset), StringComparison.OrdinalIgnoreCase))
			{
				return BottomInset;
			}
			else if (propertyName.Equals(nameof(LeftInset), StringComparison.OrdinalIgnoreCase))
			{
				return LeftInset;
			}
			else if (propertyName.Equals(nameof(RightInset), StringComparison.OrdinalIgnoreCase))
			{
				return RightInset;
			}
			else if (propertyName.Equals(nameof(TopInset), StringComparison.OrdinalIgnoreCase))
			{
				return TopInset;
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(BottomInset), StringComparison.OrdinalIgnoreCase))
			{
				BottomInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(LeftInset), StringComparison.OrdinalIgnoreCase))
			{
				LeftInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(RightInset), StringComparison.OrdinalIgnoreCase))
			{
				RightInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(TopInset), StringComparison.OrdinalIgnoreCase))
			{
				TopInset = ValidateValue<float>(propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
