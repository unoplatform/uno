#nullable enable

using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Common;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

partial class NativeTimePickerFlyout
{
	private readonly SerialDisposable _onLoad = new SerialDisposable();
	private readonly SerialDisposable _onUnloaded = new SerialDisposable();
	internal protected TimePickerSelector? _timeSelector;
	internal protected FrameworkElement? _headerUntapZone;
	private FlyoutPresenter? _timePickerPresenter;
	private bool _isInitialized;

	protected override void InitializePopupPanel()
	{
		_popup.PopupPanel = new PickerFlyoutPopupPanel(this)
		{
			Visibility = Visibility.Collapsed,
			Background = SolidColorBrushHelper.Transparent,
			AutoresizingMask = UIViewAutoresizing.All,
			Frame = new CGRect(CGPoint.Empty, ViewHelper.GetMainWindowSize())
		};
	}

	/// <summary>
	/// This method sets the Content property of the Flyout.
	/// </summary>
	/// <remarks>
	/// Note that for performance reasons, we don't call it in the constructor. Instead, we wait for the popup to be opening.
	/// The native UIDatePicker contained in the TimePickerSelector is known for being slow in general (https://bugzilla.xamarin.com/show_bug.cgi?id=49469).
	/// Using this strategy means that a page containing a TimePicker will no longer be slowed down by this initialization during the page creation.
	/// Instead, you'll see the delay when opening the TimePickerFlyout for the first time.
	/// This is most notable on pages containing multiple TimePicker controls.
	/// </remarks>
	private void InitializeContent()
	{
		if (_isInitialized)
		{
			return;
		}

		_isInitialized = true;

		_timeSelector = new TimePickerSelector()
		{
			BorderThickness = Thickness.Empty,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Time = Time,
			ClockIdentifier = ClockIdentifier,
		};

		Content = _timeSelector;

		this.Binding(nameof(Time), nameof(Time), Content, BindingMode.TwoWay);
		this.Binding(nameof(MinuteIncrement), nameof(MinuteIncrement), Content, BindingMode.TwoWay);
		this.Binding(nameof(ClockIdentifier), nameof(ClockIdentifier), Content, BindingMode.TwoWay);
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
			typeof(NativeTimePickerFlyout),
			new FrameworkPropertyMetadata(default(IFrameworkElement), FrameworkPropertyMetadataOptions.AffectsMeasure, OnContentChanged));

	private static void OnContentChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var flyout = dependencyObject as NativeTimePickerFlyout;

		if (flyout?._timePickerPresenter != null)
		{
			if (args.NewValue is IDependencyObjectStoreProvider binder)
			{
				binder.Store.SetValue(binder.Store.TemplatedParentProperty, flyout.TemplatedParent, DependencyPropertyValuePrecedences.Local);
			}

			flyout._timePickerPresenter.Content = args.NewValue;
		}
	}
	#endregion

	protected override Control CreatePresenter()
	{
		_timePickerPresenter = new NativeTimePickerFlyoutPresenter()
		{
			Content = Content,
		};

		if (TimePickerFlyoutPresenterStyle is { } style)
		{
			_timePickerPresenter.Style = style;
		}

		void onLoad(object sender, RoutedEventArgs e)
		{
			_headerUntapZone = _timePickerPresenter?.FindName("HeaderUntapableZone") as FrameworkElement;

			if (_timePickerPresenter != null)
			{
				AttachAcceptCommand(_timePickerPresenter);
				AttachDismissCommand(_timePickerPresenter);
			}

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

		return _timePickerPresenter!;
	}

	private void OnTap(object sender, Input.PointerRoutedEventArgs e) => e.Handled = true;

	protected internal override void Open()
	{
		InitializeContent();

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
		var b = control.FindName("AcceptButton") as Button;

		if (b != null && b.Command == null)
		{
			var wr = WeakReferencePool.RentWeakReference(this, this);
			b.Command = new DelegateCommand(() => (wr.Target as NativeTimePickerFlyout)?.Accept());
		}
	}

	private void AttachDismissCommand(IFrameworkElement control)
	{
		var b = control.FindName("DismissButton") as Button;

		if (b != null && b.Command == null)
		{
			var wr = WeakReferencePool.RentWeakReference(this, this);
			b.Command = new DelegateCommand(() => (wr.Target as NativeTimePickerFlyout)?.Dismiss());
		}
	}

	private void Accept()
	{
		var oldTime = Time;
		_timeSelector?.SaveTime();
		OnTimePicked(new TimePickedEventArgs(oldTime, Time));
		Hide(false);
	}

	private void Dismiss()
	{
		Hide(false);
	}
}
