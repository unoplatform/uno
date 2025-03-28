#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBarSeparator : Control, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement
	{
		public AppBarSeparator()
		{
			DefaultStyleKey = typeof(AppBarSeparator);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == AppBarSeparator.IsCompactProperty
				|| args.Property == AppBarSeparator.UseOverflowStyleProperty)
			{
				UpdateVisualState();
			}
		}

		// After template is applied, set the initial view state
		// (FullSize or Compact) based on the value of our
		// IsCompact property
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateVisualState();
		}

		// Sets the visual state to "Compact" or "FullSize" based on the value
		// of our IsCompact property
		private protected override void ChangeVisualState(bool useTransitions)
		{
			bool isCompact = false;
			bool useOverflowStyle = false;

			base.ChangeVisualState(useTransitions);

			useOverflowStyle = UseOverflowStyle;
			isCompact = IsCompact;

			if (useOverflowStyle)
			{
				GoToState(useTransitions, "Overflow");
			}
			else if (isCompact)
			{
				GoToState(useTransitions, "Compact");
			}
			else
			{
				GoToState(useTransitions, "FullSize");
			}
		}

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);
			CommandBar.OnCommandBarElementVisibilityChanged(this);
		}
	}
}
