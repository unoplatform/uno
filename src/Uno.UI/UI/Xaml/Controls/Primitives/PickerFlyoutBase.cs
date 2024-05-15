using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class PickerFlyoutBase : FlyoutBase
	{
		public static DependencyProperty TitleProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
				"Title", typeof(string),
				typeof(PickerFlyoutBase),
				new FrameworkPropertyMetadata(default(string)));

		public static string GetTitle(DependencyObject element) => (string)element.GetValue(TitleProperty) ?? "";

		public static void SetTitle(DependencyObject element, string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			element.SetValue(TitleProperty, value);
		}

		protected virtual void OnConfirmed() => Hide();

		protected virtual bool ShouldShowConfirmationButtons() => throw new InvalidOperationException();

		protected override void InitializePopupPanel()
		{
			// -- UNO SPECIFIC --
			// Prevent Flyout from creating a PlacementPopupPanel and let the Popup create it.
			// That way the FlyoutBase.PlaceFlyoutForDateTimePicker() will be able to do its job.
		}
	}

}
