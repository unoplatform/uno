#nullable enable

using System;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
	private readonly SerialDisposable _nativeScrollViewAttachment = new SerialDisposable();
	private NativeRefreshControl? _refreshControl = null;
	private ContentPresenter? _contentPresenter = null;
	private bool _managedIsRefreshing = false;

	private void InitializePlatform()
	{
		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;
	}

	partial void OnApplyTemplatePartial()
	{
		_refreshControl = GetTemplateChild<NativeRefreshControl>("NativeRefreshControl");

		if (_refreshControl == null)
		{
			throw new NotSupportedException($"RefreshContainer requires a control of type {nameof(NativeRefreshControl)} in its hierarchy.");
		}

		_contentPresenter = GetTemplateChild<ContentPresenter>("ContentPresenter");


		SubscribeToRefresh();
		//_refreshControl.Content = Content as Android.Views.View;
	}

	protected override Size MeasureOverride(Size availableSize)
	{		
		var size = base.MeasureOverride(availableSize);
		return size;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{		
		var size = base.ArrangeOverride(finalSize);
		return size;
	}

	private void OnLoaded(object sender, EventArgs e)
	{
		if (_refreshControl != null)
		{
			SubscribeToRefresh();
		}
	}

	private void OnUnloaded(object sender, EventArgs e)
	{
		_refreshSubscription.Disposable = null;
	}

	private void SubscribeToRefresh()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.Refresh += OnNativeRefresh;

			_refreshSubscription.Disposable = Disposable.Create(() => _refreshControl.Refresh -= OnNativeRefresh);
		}
	}

	private bool IsNativeRefreshing => _refreshControl?.Refreshing ?? false;

	private void EndNativeRefreshing()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.Refreshing = false;
		}
	}

	private void OnNativeRefresh(object sender, EventArgs e)
	{
		OnNativeRefreshingChanged();
	}

	protected override void OnContentChanged(object oldValue, object newValue)
	{
		base.OnContentChanged(oldValue, newValue);

		if (_refreshControl != null)
		{
			_refreshControl.Content = Content as Android.Views.View;
		}
	}

	private void RequestRefreshPlatform()
	{
		if (_refreshControl != null)
		{
			_refreshControl.Refreshing = true;
			OnNativeRefreshingChanged();
		}
	}

	private void SetIndicatorOffset(Point newValue)
	{
		if (newValue.Y != 0)
		{
			//the hardcoded 64 here is the constant height of the indicator
			_refreshControl?.SetProgressViewEndTarget(true, ViewHelper.LogicalToPhysicalPixels(newValue.Y + 64));
		}
	}
}
