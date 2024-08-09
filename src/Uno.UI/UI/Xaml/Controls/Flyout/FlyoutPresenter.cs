using System;
using Uno.UI;
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
				Content?.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction);
			}
			else if (args.Property == AllowFocusWhenDisabledProperty)
			{
				Content?.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);
			}

			base.OnPropertyChanged2(args);
		}

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			Content?.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction);
			Content?.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
