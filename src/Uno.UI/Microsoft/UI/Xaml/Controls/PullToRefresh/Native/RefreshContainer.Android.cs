#nullable enable

using System;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
	private readonly SerialDisposable _refreshVisualizerSubscriptions = new SerialDisposable();
	private NativeRefreshControl? _refreshControl = null;

	partial void InitializePlatformPartial()
	{
		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;
	}

	partial void OnApplyTemplatePartial()
	{
		_refreshControl =
			GetTemplateChild<NativeRefreshControl>("NativeRefreshControl") ??
			throw new NotSupportedException(
				$"RefreshContainer requires a control of type " +
				$"{nameof(NativeRefreshControl)} in its hierarchy.");

		SubscribeToRefresh();
		OnRefreshVisualizerChangedPartial();
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

	internal void EndNativeRefreshing()
	{
		if (_refreshControl is not null)
		{
			_refreshControl.Refreshing = false;
		}
	}

	private void OnNativeRefresh(object? sender, EventArgs e)
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

	internal void RequestRefreshPlatform()
	{
		if (_refreshControl != null)
		{
			_refreshControl.Refreshing = true;
			OnNativeRefreshingChanged();
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
			var androidIndicatorColor = (int)(Android.Graphics.Color)foregroundBrush.ColorWithOpacity;
			var indicatorColorScheme = new[] { androidIndicatorColor };
			_refreshControl.SetColorSchemeColors(indicatorColorScheme);
		}

		if (visualizer.Background is SolidColorBrush backgroundBrush)
		{
			var androidBackgroundColor = (int)(Android.Graphics.Color)backgroundBrush.ColorWithOpacity;
			_refreshControl.SetProgressBackgroundColorSchemeColor(androidBackgroundColor);
		}
	}
}
