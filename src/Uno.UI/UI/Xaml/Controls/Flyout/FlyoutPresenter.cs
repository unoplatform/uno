using System;
using Uno.UI;
using Uno.UI.Helpers;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		public FlyoutPresenter()
		{
			DefaultStyleKey = typeof(FlyoutPresenter);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == AllowFocusOnInteractionProperty)
			{
				Content?.SetValue(AllowFocusOnInteractionProperty, Boxes.Box(AllowFocusOnInteraction));
			}
			else if (args.Property == AllowFocusWhenDisabledProperty)
			{
				Content?.SetValue(AllowFocusWhenDisabledProperty, Boxes.Box(AllowFocusWhenDisabled));
			}

			base.OnPropertyChanged2(args);
		}

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			Content?.SetValue(AllowFocusOnInteractionProperty, Boxes.Box(AllowFocusOnInteraction));
			Content?.SetValue(AllowFocusWhenDisabledProperty, Boxes.Box(AllowFocusWhenDisabled));
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
