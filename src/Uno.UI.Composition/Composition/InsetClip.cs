#nullable enable

using System;

namespace Microsoft.UI.Composition
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

		private protected override bool IsAnimatableProperty(ReadOnlySpan<char> propertyName)
		{
			if (propertyName is nameof(BottomInset) or nameof(LeftInset) or nameof(RightInset) or nameof(TopInset))
			{
				return true;
			}

			return base.IsAnimatableProperty(propertyName);
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName is nameof(BottomInset))
			{
				BottomInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(LeftInset))
			{
				LeftInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(RightInset))
			{
				RightInset = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(TopInset))
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
