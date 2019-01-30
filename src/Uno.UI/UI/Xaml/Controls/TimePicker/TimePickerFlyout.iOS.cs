
using System;
using Uno.Disposables;
using Uno.UI.Common;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyout : Flyout
	{
		private readonly SerialDisposable _onLoad = new SerialDisposable();
		private readonly SerialDisposable _onUnloaded = new SerialDisposable();
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

				_onLoad.Disposable = null;
			}

			void onUnload(object sender, RoutedEventArgs e)
			{
				_onUnloaded.Disposable = null;
				_onLoad.Disposable = null;
			}
			
			if (_timePickerPresenter != null)
			{
				_onLoad.Disposable = Disposable.Create(() => _timePickerPresenter.Loaded -= onLoad);
				_onUnloaded.Disposable = Disposable.Create(() => _timePickerPresenter.Unloaded -= onUnload);
				_timePickerPresenter.Loaded += onLoad;
				_timePickerPresenter.Unloaded += onUnload;
			}
			
			return _timePickerPresenter;
		}

		private void OnTap(object sender, Input.PointerRoutedEventArgs e) => e.Handled = true;

		protected internal override void Open()
		{
			_timeSelector.Initialize();

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

			_timeSelector.Cancel();

			base.Close();
		}

		private void AttachAcceptCommand(IFrameworkElement control)
		{
			var b = control.FindName("AcceptButton") as Button;

			if (b != null && b.Command == null)
			{
				var wr = WeakReferencePool.RentWeakReference(this, this);
				b.Command = new DelegateCommand(() => (wr.Target as TimePickerFlyout)?.Accept());
			}
		}

		private void AttachDismissCommand(IFrameworkElement control)
		{
			var b = control.FindName("DismissButton") as Button;

			if (b != null && b.Command == null)
			{
				var wr = WeakReferencePool.RentWeakReference(this, this);
				b.Command = new DelegateCommand(() => (wr.Target as TimePickerFlyout)?.Dismiss());
			}
		}

		private void Accept()
		{
			_timeSelector.SaveTime();
			Hide(false);
		}

		private void Dismiss()
		{
			Hide(false);
		}
	}
}
