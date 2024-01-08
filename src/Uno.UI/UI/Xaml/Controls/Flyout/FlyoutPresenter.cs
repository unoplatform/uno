using System;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		public FlyoutPresenter()
		{
			DefaultStyleKey = typeof(FlyoutPresenter);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the default shadow effect is shown.
		/// </summary>
		public bool IsDefaultShadowEnabled
		{
			get => (bool)GetValue(IsDefaultShadowEnabledProperty);
			set => SetValue(IsDefaultShadowEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the IsDefaultShadowEnabled dependency property.
		/// </summary>
		public static DependencyProperty IsDefaultShadowEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsDefaultShadowEnabled), 
				typeof(bool),
				typeof(FlyoutPresenter),
				new FrameworkPropertyMetadata(true));

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
