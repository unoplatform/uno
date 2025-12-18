#nullable enable

using System.Collections.Generic;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Interface for platform-specific native menu bar implementations.
/// </summary>
public interface INativeMenuBarProvider
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

/// <summary>
/// Support class for registering platform-specific native menu bar implementations for Skia targets.
/// </summary>
public static class NativeMenuBarSkiaSupport
{
	private static INativeMenuBarProvider? _provider;

	/// <summary>
	/// Registers a platform-specific native menu bar provider.
	/// </summary>
	/// <param name="provider">The provider to register.</param>
	public static void RegisterExtension(INativeMenuBarProvider provider)
	{
		_provider = provider;
	}

	internal static INativeMenuBarProvider? GetProvider() => _provider;
}
