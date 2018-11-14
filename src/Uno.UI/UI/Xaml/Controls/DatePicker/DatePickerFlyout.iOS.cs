#if XAMARIN_IOS
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Common;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	//TODO: should inherit from PickerFlyoutBase (Task 17592)
	public partial class DatePickerFlyout : Flyout
	{
		public DatePickerFlyout()
		{
			InitializeContent();
		}

		private void InitializeContent()
		{
			Content = new DatePickerSelector();
			BindToContent("MinYear");
			BindToContent("MaxYear");
			Opening += DatePickerFlyout_Opening;
			Closed += DatePickerFlyout_Closed;

			//TODO: support Day/Month/YearVisible (Task 17591)

			AttachAcceptCommand((DatePickerSelector)Content);
		}

		private void DatePickerFlyout_Opening(object sender, EventArgs e)
		{
			// The date coerced by UIDatePicker doesn't propagate back to DatePickerSelector (#137137)
			// When the user selected an invalid date, a `ValueChanged` will be raised after coercion to propagate the coerced date.
			// However, when the `Date` is set below, there no `ValueChanged` to propagate the coerced date.
			// To address this, we clamp the date between the valid range.
			var validDate = Date;
			validDate = validDate > MaxYear ? MaxYear : validDate;
			validDate = validDate < MinYear ? MinYear : validDate;

			if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				((DatePickerSelector)Content).Date = validDate;
			}
			else
			{
				// This is a workaround for the datepicker crashing on iOS 9 with this error:
				// NSInternalInconsistencyException Reason: UITableView dataSource is not set.
				this.Dispatcher.RunAsync(Core.CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(100);
					((DatePickerSelector)Content).Date = validDate;
				});
			}
		}

		private void DatePickerFlyout_Closed(object sender, EventArgs e)
		{
			Date = ((DatePickerSelector)Content).Date;
		}

		protected override Control CreatePresenter()
		{
			var presenter = new DatePickerFlyoutPresenter()
			{
				Style = FlyoutPresenterStyle,
				Content = Content
			};

			AttachAcceptCommand(presenter);

			return presenter;
		}

		private void AttachAcceptCommand(IFrameworkElement rootControl)
		{
			var acceptButton = rootControl.FindName("AcceptButton") as Button;
			if (acceptButton != null && acceptButton.Command == null)
			{
				acceptButton.Command = new DelegateCommand(Hide);
			}
		}

		private void BindToContent(string propertyName)
		{
			this.Binding(propertyName, propertyName, Content, BindingMode.TwoWay);
		}
	}
}

#endif