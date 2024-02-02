using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI;
using Uno.Disposables;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Provides Border and Background rendering capabilities to a UI element.
/// </summary>
internal partial class BorderLayerRenderer
{
	private readonly FrameworkElement _owner;
	private readonly IBorderInfoProvider _borderInfoProvider;
	private readonly SerialDisposable _borderBrushSubscription = new();
	private readonly SerialDisposable _backgroundBrushSubscription = new();

	private BorderLayerState _currentState;

	public BorderLayerRenderer(FrameworkElement owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		if (owner is not IBorderInfoProvider borderInfoProvider)
		{
			throw new InvalidOperationException("BorderLayerRenderer requires an owner which implements IBorderInfoProvider");
		}

		_borderInfoProvider = borderInfoProvider;

		_owner.Loaded += (s, e) => Update();
		_owner.Unloaded += (s, e) => Clear();
		_owner.LayoutUpdated += (s, e) => Update();
	}

	/// <summary>
	/// Updates the border.
	/// </summary>
	internal void Update()
	{
		if (_owner.IsLoaded)
		{
			// Subscribe to brushes to observe their changes.
			if (_currentState.BorderBrush != _borderInfoProvider.BorderBrush)
			{
				_borderBrushSubscription.Disposable = null;
				if (_borderInfoProvider.BorderBrush is { } borderBrush)
				{
					borderBrush.InvalidateRender += OnBorderBrushChanged;
					_borderBrushSubscription.Disposable = Disposable.Create(() => borderBrush.InvalidateRender -= OnBorderBrushChanged);
				}
			}

			if (_currentState.Background != _borderInfoProvider.Background)
			{
				_backgroundBrushSubscription.Disposable = null;
				if (_borderInfoProvider.Background is { } backgroundBrush)
				{
					backgroundBrush.InvalidateRender += OnBackgroundBrushChanged;
					_backgroundBrushSubscription.Disposable = Disposable.Create(() => backgroundBrush.InvalidateRender -= OnBackgroundBrushChanged);
				}
			}

			UpdatePlatform();
		}
	}

	private void OnBorderBrushChanged()
	{
		// Force the border to be recreated during update.
		_currentState.BorderBrush = null;
		Update();
	}

	private void OnBackgroundBrushChanged()
	{
		// Force the background to be recreated during update.
		_currentState.Background = null;
		Update();
	}

	/// <summary>
	/// Removes added layers and subscriptions.
	/// </summary>
	internal void Clear()
	{
		UnsubscribeBrushChanges();
		ClearPlatform();
	}

	private void UnsubscribeBrushChanges()
	{
		_borderBrushSubscription.Disposable = null;
		_backgroundBrushSubscription.Disposable = null;
	}

	partial void UpdatePlatform();

	partial void ClearPlatform();
}
