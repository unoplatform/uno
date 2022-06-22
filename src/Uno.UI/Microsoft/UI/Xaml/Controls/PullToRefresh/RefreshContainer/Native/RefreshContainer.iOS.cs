#nullable enable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
	private NativeRefreshControl _refreshControl = null!;
	private bool _managedIsRefreshing = false;

	private void InitializePlatform()
	{
		_refreshControl = new NativeRefreshControl();
		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;
	}

	private void RequestRefreshPlatform()
	{
		if (!_refreshControl.Refreshing)
		{
			_refreshControl.BeginRefreshing();
		}
	}

	partial void OnApplyTemplatePartial()
	{
		base.OnApplyTemplate();		
	}
		
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_refreshControl.Superview != null)
		{
			_refreshControl.RemoveFromSuperview();
		}

		// Inject the UIRefreshControl into the first scrollable element found in the hierarchy
		if (this.FindFirstChild<UIScrollView>() is { } scrollView)
		{
			SetRefreshControlOnNativeView(scrollView);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"No {nameof(UIScrollView)} found to host refresh indicator; swipe to refresh will not be available.");
		}

		_refreshControl.ValueChanged += OnRefreshControlValueChanged;
		_refreshSubscription.Disposable = Disposable.Create(() => _refreshControl.ValueChanged -= OnRefreshControlValueChanged);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_refreshControl.EndRefreshing();
		_refreshSubscription.Disposable = null;
	}

	private void OnRefreshControlValueChanged(object sender, EventArgs e)
	{
		if (_refreshControl.Refreshing && !_managedIsRefreshing)
		{
			_managedIsRefreshing = true;
			RefreshRequested?.Invoke(this, new RefreshRequestedEventArgs(new Deferral(() =>
			{
				_refreshControl.EndRefreshing();
				_managedIsRefreshing = false;
			}))); // TODO:MZ
		}
	}

	private void SetRefreshControlOnNativeView(UIScrollView targetView)
	{
		foreach (var existingRefresh in targetView.Subviews.OfType<NativeRefreshControl>())
		{
			// We can get a scroll view that already has a NativeSwipeRefresh due to template reuse. 
			existingRefresh.RemoveFromSuperview();
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
		{
			targetView.RefreshControl = _refreshControl;
		}
		else
		{
			targetView.AddSubview(_refreshControl);
		}

		// Setting AlwaysBounceVertical allows the refresh to work even when the scroll view is not scrollable (ie its content
		// fits entirely in its visible bounds)
		targetView.AlwaysBounceVertical = true;
	}

}
