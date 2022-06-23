#nullable enable

using System;
using System.Linq;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
	private readonly SerialDisposable _nativeScrollViewAttachment = new SerialDisposable();
	private NativeRefreshControl _refreshControl = null!;
	private UIScrollView? _ownerScrollView = null;
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
			var offsetPoint = new CGPoint(0, -_refreshControl.Frame.Size.Height);
			_ownerScrollView?.SetContentOffset(offsetPoint, animated: true);
			OnNativeRefreshingChanged();
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


		AttachToNativeScrollView();

		_refreshControl.ValueChanged += OnRefreshControlValueChanged;
		_refreshSubscription.Disposable = Disposable.Create(() => _refreshControl.ValueChanged -= OnRefreshControlValueChanged);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_refreshControl.EndRefreshing();
		_refreshSubscription.Disposable = null;
	}

	private void OnRefreshControlValueChanged(object sender, EventArgs e) => OnNativeRefreshingChanged();

	private bool IsNativeRefreshing => _refreshControl.Refreshing;
					
	private void EndNativeRefreshing() => _refreshControl.EndRefreshing();

	protected override void OnContentChanged(object oldValue, object newValue)
	{
		base.OnContentChanged(oldValue, newValue);
		
		_nativeScrollViewAttachment.Disposable = null;

		AttachToNativeScrollView();
	}

	private void AttachToNativeScrollView()
	{
		// Inject the UIRefreshControl into the first scrollable element found in the hierarchy		
		if (this.FindFirstChild<UIScrollView>() is { } scrollView)
		{
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
}
