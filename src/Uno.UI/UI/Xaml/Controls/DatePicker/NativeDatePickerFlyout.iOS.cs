using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Common;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Controls
{
	//TODO#2780: support Day/Month/YearVisible
	public partial class NativeDatePickerFlyout : DatePickerFlyout
	{
		#region Template Parts
		public const string AcceptButtonPartName = "AcceptButton";
		public const string DismissButtonPartName = "DismissButton";
		#endregion

		private readonly SerialDisposable _presenterLoadedDisposable = new SerialDisposable();
		private readonly SerialDisposable _presenterUnloadedDisposable = new SerialDisposable();
		private bool _isInitialized;
		internal DatePickerSelector _selector;

		private NativeDatePickerFlyoutPresenter _presenter
		{
			get => _tpPresenter as NativeDatePickerFlyoutPresenter;
			set => _tpPresenter = value;
		}

		internal bool IsNativeDialogOpen { get; private set; }

		public static DependencyProperty UseNativeMinMaxDatesProperty { get; } = DependencyProperty.Register(
			"UseNativeMinMaxDates",
			typeof(bool),
			typeof(NativeDatePickerFlyout),
			new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Setting this to true will interpret MinYear/MaxYear as MinDate and MaxDate.
		/// </summary>
		public bool UseNativeMinMaxDates
		{
			get => (bool)GetValue(UseNativeMinMaxDatesProperty);
			set => SetValue(UseNativeMinMaxDatesProperty, value);
		}

		public NativeDatePickerFlyout()
		{
			Opening += DatePickerFlyout_Opening;
			Closed += DatePickerFlyout_Closed;

			RegisterPropertyChangedCallback(DateProperty, OnDateChanged);
		}

		protected override void InitializePopupPanel()
		{
			_popup.PopupPanel = new PickerFlyoutPopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent
			};
		}

		/// <summary>
		/// This method sets the Content property of the Flyout.
		/// </summary>
		/// <remarks>
		/// Note that for performance reasons, we don't call it in the constructor. Instead, we wait for the popup to be opening.
		/// The native UIDatePicker contained in the DatePickerSelector is known for being slow in general (https://bugzilla.xamarin.com/show_bug.cgi?id=49469).
		/// Using this strategy means that a page containing a DatePicker will no longer be slowed down by this initialization during the page creation.
		/// Instead, you'll see the delay when opening the DatePickerFlyout for the first time.
		/// This is most notable on pages containing multiple DatePicker controls.
		/// </remarks>
		private void InitializeContent()
		{
			if (_isInitialized)
			{
				return;
			}

			_isInitialized = true;

			Content = _selector = new DatePickerSelector();

			BindToContent("UseNativeMinMaxDates");
			BindToContent("MinYear");
			BindToContent("MaxYear");
		}

		protected override Control CreatePresenter()
		{
			_presenter = new NativeDatePickerFlyoutPresenter() { Content = _selector };

			_presenterLoadedDisposable.Disposable = Disposable.Create(() => _presenter.Loaded -= OnPresenterLoaded);
			_presenterUnloadedDisposable.Disposable = Disposable.Create(() => _presenter.Unloaded -= OnPresenterUnloaded);

			_presenter.Loaded += OnPresenterLoaded;
			_presenter.Unloaded += OnPresenterUnloaded;

			return _presenter;
		}

		#region Content DependencyProperty
		internal IFrameworkElement Content
		{
			get { return (IFrameworkElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		internal static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(
				"Content",
				typeof(IFrameworkElement),
				typeof(NativeDatePickerFlyout),
				new FrameworkPropertyMetadata(default(IFrameworkElement), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.ValueInheritsDataContext, OnContentChanged));

		private static void OnContentChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var flyout = dependencyObject as NativeDatePickerFlyout;

			if (flyout._presenter != null)
			{
				if (args.NewValue is IDependencyObjectStoreProvider binder)
				{
					binder.Store.SetValue(binder.Store.TemplatedParentProperty, flyout.TemplatedParent, DependencyPropertyValuePrecedences.Local);
					binder.Store.SetValue(binder.Store.DataContextProperty, flyout.DataContext, DependencyPropertyValuePrecedences.Local);
				}

				flyout._presenter.Content = args.NewValue;
			}
		}
		#endregion

		private void DatePickerFlyout_Opening(object sender, object e)
		{
			InitializeContent();
			var date = Date;
			// If we're setting the date to the null sentinel value,
			// we'll instead set it to the current date for the purposes
			// of where to place the user's position in the looping selectors.
			if (date.Ticks == DatePicker.DEFAULT_DATE_TICKS)
			{
				var temp = new global::Windows.Globalization.Calendar();
				var calendar = new global::Windows.Globalization.Calendar(
					temp.Languages,
					CalendarIdentifier,
					temp.GetClock());
				calendar.SetToNow();
				date = calendar.GetDateTime();
			}
			UpdateSelectorDate(date);
		}

		private void DatePickerFlyout_Closed(object sender, object e)
		{
			_selector.Cancel();
		}

		private void OnPresenterLoaded(object sender, RoutedEventArgs e)
		{
			AttachFlyoutCommand(AcceptButtonPartName, x => x.Accept());
			AttachFlyoutCommand(DismissButtonPartName, x => x.Dismiss());

			_presenterLoadedDisposable.Disposable = null;
			IsNativeDialogOpen = true;
		}

		private void OnPresenterUnloaded(object sender, RoutedEventArgs e)
		{
			_presenterLoadedDisposable.Disposable = null;
			_presenterUnloadedDisposable.Disposable = null;
			IsNativeDialogOpen = false;
		}

		private void OnDateChanged(DependencyObject sender, DependencyProperty dp)
		{
			UpdateSelectorDate(Date);
		}

		internal void Accept()
		{
			_selector.SaveValue();
			Hide(false);

			var oldDate = Date;
			Date = _selector.Date;

			_datePicked?.Invoke(this, new DatePickedEventArgs(Date, oldDate));
		}

		private void Dismiss()
		{
			_selector.Cancel();
			Hide(false);
		}

		private void UpdateSelectorDate(DateTimeOffset value)
		{
			if (!_isInitialized) return;

			// The date coerced by UIDatePicker doesn't propagate back to DatePickerSelector (#137137)
			// When the user selected an invalid date, a `ValueChanged` will be raised after coercion to propagate the coerced date.
			// However, when the `Date` is set below, there no `ValueChanged` to propagate the coerced date.
			// To address this, we clamp the date between the valid range.
			var validDate = value;
			validDate = validDate > MaxYear ? MaxYear : validDate;
			validDate = validDate < MinYear ? MinYear : validDate;

			if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				_selector.Date = validDate;
			}
			else
			{
				// This is a workaround for the datepicker crashing on iOS 9 with this error:
				// NSInternalInconsistencyException Reason: UITableView dataSource is not set.
				_ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(100);
					_selector.Date = validDate;
				});
			}
		}

		private IDisposable AttachFlyoutCommand(string targetButtonName, Action<NativeDatePickerFlyout> action)
		{
			if (_presenter.FindName(targetButtonName) is Button button)
			{
				if (button.Command == null)
				{
					var self = WeakReferencePool.RentSelfWeakReference(this);
					button.Command = new DelegateCommand(() => (self.Target as NativeDatePickerFlyout)?.Apply(action));

					return Disposable.Create(() =>
					{
						button.Command = null;
						self.Dispose();
					});
				}
			}

			return Disposable.Empty;
		}

		private void BindToContent(string propertyName)
		{
			Content.Binding(propertyName, propertyName, this, BindingMode.OneWay);
		}
	}
}
