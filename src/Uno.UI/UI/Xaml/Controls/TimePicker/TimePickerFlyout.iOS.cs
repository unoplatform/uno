
using Uno.UI.Common;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyout : Flyout
	{
		internal protected TimePickerSelector _timeSelector;
		internal protected TimePickerFlyoutPresenter _timePickerPresenter;
		internal protected FrameworkElement _headerUntapZone;

		public TimePickerFlyout()
		{
			_timeSelector = new TimePickerSelector()
			{
				BorderThickness = Thickness.Empty,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch
			};

			Content = _timeSelector;

			this.Binding(nameof(Time), nameof(Time), Content, BindingMode.TwoWay);
			this.Binding(nameof(ClockIdentifier), nameof(ClockIdentifier), Content, BindingMode.TwoWay);
		}

		protected override Control CreatePresenter()
		{
			_timePickerPresenter = new TimePickerFlyoutPresenter() { Content = Content };

			void onLoad(object sender, RoutedEventArgs e)
			{
				_headerUntapZone = _timePickerPresenter?.FindName("HeaderUntapableZone") as FrameworkElement;

				AttachAcceptCommand(_timePickerPresenter);
				AttachDismissCommand(_timePickerPresenter);

				if (_timePickerPresenter != null)
				{
					_timePickerPresenter.Loaded -= onLoad;
				}
			}

			if (_timePickerPresenter != null)
			{
				_timePickerPresenter.Loaded += onLoad;
			}

			return _timePickerPresenter;
		}

		private void OnTap(object sender, Input.PointerRoutedEventArgs e) => e.Handled = true;

		protected internal override void Open()
		{
			_timeSelector?.Initialize();

			//Gobbling pressed tap on the flyout header background so that it doesn't close the flyout popup. 
			if (_headerUntapZone != null)
			{
				_headerUntapZone.PointerPressed += OnTap;
			}

			base.Open();
		}

		protected internal override void Close()
		{
			if (_headerUntapZone != null)
			{
				_headerUntapZone.PointerPressed -= OnTap;
			}

			_timeSelector?.Cancel();

			base.Close();
		}

		private void AttachAcceptCommand(IFrameworkElement control)
		{
			if (control?.FindName("AcceptButton") is Button b && b.Command == null)
			{
				b.Command = new DelegateCommand(Accept);
			}
		}

		private void AttachDismissCommand(IFrameworkElement control)
		{
			if (control?.FindName("DismissButton") is Button b && b.Command == null)
			{
				b.Command = new DelegateCommand(Dismiss);
			}
		}

		private void Accept()
		{
			_timeSelector?.SaveTime();
			Hide(false);
		}

		private void Dismiss()
		{
			Hide(false);
		}
	}
}
