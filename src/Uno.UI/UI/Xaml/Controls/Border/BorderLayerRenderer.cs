#nullable enable

using System;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides Border and Background rendering capabilities to a UI element.
/// </summary>
internal partial class BorderLayerRenderer
{
	private readonly FrameworkElement _owner;
	private readonly IBorderInfoProvider? _borderInfoProvider;

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

	internal void Update() => UpdateLayer();

	internal void Clear() => ClearLayer();

	partial void UpdateLayer();

	partial void ClearLayer();
}
