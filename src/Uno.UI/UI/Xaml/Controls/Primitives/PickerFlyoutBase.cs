using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class PickerFlyoutBase : FlyoutBase
	{
		public static DependencyProperty TitleProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.RegisterAttached(
				"Title", typeof(string),
				typeof(PickerFlyoutBase),
#pragma warning disable Uno0002_Internal // String dependency properties (in *most* cases) shouldn't have null default value.
				new FrameworkPropertyMetadata(default(string))); // NOTE: This shouldn't be string.Empty to match UWP
#pragma warning restore Uno0002_Internal // String dependency properties (in *most* cases) shouldn't have null default value.

		public static string GetTitle(DependencyObject element) => (string)element.GetValue(TitleProperty) ?? string.Empty;

		public static void SetTitle(DependencyObject element, string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			element.SetValue(TitleProperty, value);
		}

		protected virtual void OnConfirmed() => throw new InvalidOperationException();

		protected virtual bool ShouldShowConfirmationButtons() => throw new InvalidOperationException();

		protected override void InitializePopupPanel()
		{
			// -- UNO SPECIFIC --
			// Prevent Flyout from creating a PlacementPopupPanel and let the Popup create it.
			// That way the FlyoutBase.PlaceFlyoutForDateTimePicker() will be able to do its job.
		}
	}

}
