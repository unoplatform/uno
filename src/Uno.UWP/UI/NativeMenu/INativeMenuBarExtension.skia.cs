#if __SKIA__
#nullable enable

using System.Collections.Generic;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Interface for platform-specific native menu bar implementations for Skia targets.
/// </summary>
internal interface INativeMenuBarExtension
{
	/// <summary>
	/// Gets a value indicating whether native menu bar is supported on this platform.
	/// </summary>
	bool IsSupported { get; }

	/// <summary>
	/// Applies the menu items to the native menu bar.
	/// </summary>
	/// <param name="items">The collection of top-level menu items to apply.</param>
	void Apply(IList<NativeMenuItem> items);
}
#endif
