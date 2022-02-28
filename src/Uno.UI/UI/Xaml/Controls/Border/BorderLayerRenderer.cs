#nullable enable

using System;
namespace Windows.UI.Xaml.Controls;

internal partial class BorderLayerRenderer
{
	private readonly UIElement _owner;

	public BorderLayerRenderer(UIElement owner, Action update)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));

		_owner.Loaded += (s, e) => Update();
		_owner.Unloaded += (s, e) => Clear();
		_owner.LayoutUpdated += (s, e) => Update();
	}

	partial void Update();

	partial void Clear();
}
