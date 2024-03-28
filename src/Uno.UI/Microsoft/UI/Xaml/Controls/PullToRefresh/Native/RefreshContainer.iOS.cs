#nullable enable

using System;
using System.Linq;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
	private readonly SerialDisposable _nativeScrollViewAttachment = new SerialDisposable();
	private readonly SerialDisposable _refreshVisualizerSubscriptions = new SerialDisposable();
	private NativeRefreshControl? _refreshControl = null;
	private UIScrollView? _ownerScrollView = null;

	partial void InitializePlatformPartial()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	internal void RequestRefreshPlatform()
	{
		if (_refreshControl is null)
		{
			return;
		}

		if (!_refreshControl.Refreshing)
		{
			_refreshControl.BeginRefreshing();
			var offsetPoint = new CGPoint(0, -_refreshControl.Frame.Size.Height);
			_ownerScrollView?.SetContentOffset(offsetPoint, animated: true);
			OnNativeRefreshingChanged();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		InitializeRefreshControl();
		OnRefreshVisualizerChangedPartial();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) => CleanupRefreshControl();

	private void OnRefreshControlValueChanged(object? sender, EventArgs e) => OnNativeRefreshingChanged();

	internal void EndNativeRefreshing()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.EndRefreshing();
		}
	}

	private void InitializeRefreshControl()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.EndRefreshing();
			_refreshSubscription.Disposable = null;
		}

		_refreshControl = new NativeRefreshControl();
		if (_refreshControl.Superview != null)
		{
			_refreshControl.RemoveFromSuperview();
		}

		AttachToNativeScrollView();

		_refreshControl.ValueChanged += OnRefreshControlValueChanged;
		_refreshSubscription.Disposable = Disposable.Create(() => _refreshControl.ValueChanged -= OnRefreshControlValueChanged);
	}

	private void CleanupRefreshControl()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.EndRefreshing();
			_refreshSubscription.Disposable = null;
		}
		_nativeScrollViewAttachment.Disposable = null;
	}

	protected override void OnContentChanged(object oldValue, object newValue)
	{
		base.OnContentChanged(oldValue, newValue);

		_nativeScrollViewAttachment.Disposable = null;

		AttachToNativeScrollView();
	}

	private void AttachToNativeScrollView()
	{
		if (_refreshControl is null)
		{
			return;
		}

		// Inject the UIRefreshControl into the first scrollable element found in the hierarchy		
		if (this.FindFirstChild<UIScrollView>() is { } scrollView)
		{
			_nativeScrollViewAttachment.Disposable = null;

			_ownerScrollView = scrollView;

			foreach (var existingRefresh in scrollView.Subviews.OfType<NativeRefreshControl>())
			{
				// We can get a scroll view that already has a refresh control due to template reuse. 
				existingRefresh.RemoveFromSuperview();
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				scrollView.RefreshControl = _refreshControl;
			}
			else
			{
				scrollView.AddSubview(_refreshControl);
			}

			// Setting AlwaysBounceVertical allows the refresh to work even when the scroll view is not scrollable (ie its content
			// fits entirely in its visible bounds)
			var originalBounceSetting = scrollView.AlwaysBounceVertical;
			scrollView.AlwaysBounceVertical = true;

			_nativeScrollViewAttachment.Disposable = Disposable.Create(() =>
			{
				_ownerScrollView = null;
				if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
				{
					scrollView.RefreshControl = null;
				}
				else
				{
					_refreshControl.RemoveFromSuperview();
				}
				scrollView.AlwaysBounceVertical = originalBounceSetting;
			});
		}
	}

	partial void OnRefreshVisualizerChangedPartial()
	{
		_refreshVisualizerSubscriptions.Disposable = null;

		if (_refreshControl is null || Visualizer is null)
		{
			return;
		}

		var visualizer = Visualizer;
		var compositeDisposable = new CompositeDisposable();
		compositeDisposable.Add(visualizer.RegisterDisposablePropertyChangedCallback(RefreshVisualizer.ForegroundProperty, OnVisualizerPropertyChanged));
		compositeDisposable.Add(visualizer.RegisterDisposablePropertyChangedCallback(RefreshVisualizer.BackgroundProperty, OnVisualizerPropertyChanged));
		_refreshVisualizerSubscriptions.Disposable = compositeDisposable;
	}

	private void OnVisualizerPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (_refreshControl is null || Visualizer is not { } visualizer)
		{
			return;
		}

		if (visualizer.Foreground is SolidColorBrush foregroundBrush)
		{
			_refreshControl.TintColor = foregroundBrush.ColorWithOpacity;
		}

		if (visualizer.Background is SolidColorBrush backgroundBrush)
		{
			_refreshControl.BackgroundColor = backgroundBrush.ColorWithOpacity;
		}
	}
}
