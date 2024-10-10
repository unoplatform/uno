#if !UNO_HAS_BORDER_VISUAL
using System;
using Windows.UI.Xaml;
using Uno.Disposables;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Provides Border and Background rendering capabilities to a UI element.
/// </summary>
internal partial class BorderLayerRenderer
{
	private readonly FrameworkElement _owner;
	private readonly IBorderInfoProvider _borderInfoProvider;

#pragma warning disable CS0414 // _currentState is not used on reference build
	private BorderLayerState _currentState;
#pragma warning restore CS0414

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
#if UNO_HAS_ENHANCED_LIFECYCLE
		_owner.SizeChanged += (_, _) => Update();
#else
		// Using SizeChanged on other platforms SHOULD work. But it didn't work on Android
		// for unknown reason. For now, we are using SizeChanged only on enhanced lifecycle
		// platforms where we are sure it works correctly.
		_owner.LayoutUpdated += (_, _) => Update();
#endif
	}

	/// <summary>
	/// Updates the border.
	/// </summary>
	internal void Update()
	{
		if (_owner.IsLoaded)
		{
			UpdatePlatform();
		}
	}

	/// <summary>
	/// Removes added layers and subscriptions.
	/// </summary>
	internal void Clear()
	{
		ClearPlatform();
		_currentState = default;
	}

	partial void UpdatePlatform();

	partial void ClearPlatform();
}
#endif
