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
			if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				((DatePickerSelector)Content).Date = Date;
			}
			else
			{
				// This is a workaround for the datepicker crashing on iOS 9 with this error:
				// NSInternalInconsistencyException Reason: UITableView dataSource is not set.
				this.Dispatcher.RunAsync(Core.CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(100);
					((DatePickerSelector)Content).Date = Date;
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