#if XAMARIN_IOS

using Uno.UI.Common;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyout : Flyout
	{
		public TimePickerFlyout()
		{
			InitializeContent();
		}

		private void OnTap(object sender, Input.PointerRoutedEventArgs e) => e.Handled = true;

		protected internal override void Close()
		{
			var timePickerPresenter = _presenter as TimePickerFlyoutPresenter;
			var headerUntapZone = timePickerPresenter?.FindName("HeaderUntapableZone") as FrameworkElement;

			if (headerUntapZone != null)
			{
				headerUntapZone.PointerPressed -= OnTap;
			}

			var timeSelector = Content as TimePickerSelector;

			if (timeSelector != null)
			{
				timeSelector.Cancel();
			}

			base.Close();
		}

		protected internal override void Open()
		{
			base.Open();

			var timePickerPresenter = _presenter as TimePickerFlyoutPresenter;
			var headerUntapZone = timePickerPresenter?.FindName("HeaderUntapableZone") as FrameworkElement;

			if (headerUntapZone != null)
			{
				//Gobbling pressed tap on the flyout header background so that it doesn't close the flyout popup. 
				headerUntapZone.PointerPressed += OnTap;
			}
		}

		private void InitializeContent()
		{
			Content = new TimePickerSelector()
			{
				BorderThickness = Thickness.Empty,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch
			};

			BindToContent(nameof(Time));
			BindToContent(nameof(ClockIdentifier));
		}

		protected override Control CreatePresenter()
		{
			var timePickerPresenter = new TimePickerFlyoutPresenter() { Content = Content };

			void onLoad(object sender, RoutedEventArgs e)
			{
				AttachAcceptCommand(timePickerPresenter);
				AttachDismissCommand(timePickerPresenter);
				timePickerPresenter.Loaded -= onLoad;
			}

			if (timePickerPresenter != null)
			{
				timePickerPresenter.Loaded += onLoad;
			}

			return timePickerPresenter;
		}

		private void BindToContent(string propertyName)
		{
			this.Binding(propertyName, propertyName, Content, BindingMode.TwoWay);
		}

		private void AttachAcceptCommand(IFrameworkElement rootControl)
		{
			var acceptButton = rootControl.FindName("AcceptButton") as Button;

			if (acceptButton != null && acceptButton.Command == null)
			{
				acceptButton.Command = new DelegateCommand(Accept);
			}
		}

		private void AttachDismissCommand(IFrameworkElement rootControl)
		{
			var dismissButton = rootControl.FindName("DismissButton") as Button;

			if (dismissButton != null && dismissButton.Command == null)
			{
				dismissButton.Command = new DelegateCommand(Dismiss);
			}
		}

		private void Accept()
		{
			var timeSelector = Content as TimePickerSelector;

			if (timeSelector != null)
			{
				timeSelector.SaveTime();
			}

			Hide(false);
		}

		private void Dismiss()
		{
			Hide(false);
		}
	}
}

#endif