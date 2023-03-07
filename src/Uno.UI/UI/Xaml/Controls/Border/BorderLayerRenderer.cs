#nullable enable

using System;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides Border and Background rendering capabilities to a UI element.
/// </summary>
internal partial class BorderLayerRenderer
{
	private readonly FrameworkElement _owner;
	private readonly IBorderInfoProvider _borderInfoProvider;
	private readonly SerialDisposable _borderBrushSubscription = new();
	private readonly SerialDisposable _backgroundBrushSubscription = new();

	private BorderLayerState _lastState;

	public BorderLayerRenderer(FrameworkElement owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		if (owner is not IBorderInfoProvider borderInfoProvider)
		{
			throw new InvalidOperationException("BorderLayerRenderer requires an owner which implements IBorderInfoProvider");
		}

		_borderInfoProvider = borderInfoProvider;

		_owner.Loaded += (s, e) => UpdateLayer();
		_owner.Unloaded += (s, e) => ClearLayer();
		_owner.LayoutUpdated += (s, e) => UpdateLayer();
	}

	/// <summary>
	/// Updates the border.
	/// </summary>
	internal void Update()
	{
		// Subscribe to brushes to observe their changes.
		if (_lastState.BorderBrush != _borderInfoProvider.BorderBrush)
		{
			_borderBrushSubscription.Disposable = null;
			if (_borderInfoProvider.BorderBrush is { } brush)
			{
				_borderBrushSubscription.Disposable = brush.RegisterDisposablePropertyChangedCallback(OnBorderBrushChanged);
			}
		}

		if (_lastState.Background != _borderInfoProvider.Background)
		{
			_backgroundBrushSubscription.Disposable = null;
			if (_borderInfoProvider.Background is { } background)
			{
				_backgroundBrushSubscription.Disposable = background.RegisterDisposablePropertyChangedCallback(OnBackgroundBrushChanged);
			}
		}

		if (_owner.IsLoaded)
		{
			UpdateLayer();
		}
	}

	private void OnBorderBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
	{
		_lastState.BorderBrush = null;
		Update();
	}

	private void OnBackgroundBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
	{
		_lastState.Background = null;
		Update();
	}

	/// <summary>
	/// Removes added layers and subscriptions.
	/// </summary>
	internal void Clear()
	{
		_borderBrushSubscription.Disposable = null;
		_backgroundBrushSubscription.Disposable = null;
		ClearLayer();
	}

	partial void UpdateLayer();

	partial void ClearLayer();
}
