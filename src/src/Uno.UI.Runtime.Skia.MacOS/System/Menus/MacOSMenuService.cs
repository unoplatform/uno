using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// Public API to configure the macOS system menu (NSMenu) via UnoNativeMac bridge.
/// </summary>
public static class MacOSMenuService
{
	private static readonly Dictionary<string, MenuItemDefinition> _definitionsById = new();
	private static Action<string>? _onItemInvoked;

	public sealed class MenuItemDefinition
	{
		public string Id { get; init; } = Guid.NewGuid().ToString("N");
		public string Title { get; init; } = string.Empty;
		public string? KeyEquivalent { get; init; }
		public bool Enabled { get; set; } = true;
		public bool IsSeparator { get; init; } = false;
		public IList<MenuItemDefinition> Children { get; } = new List<MenuItemDefinition>();
	}

	/// <summary>
	/// Sets the main application menu. All previous items created by this service are removed.
	/// </summary>
	/// <param name="topLevel">Top level items to include in the main menu bar.</param>
	/// <param name="onItemInvoked">Callback invoked with the Id of the clicked menu item.</param>
	public static void SetMainMenu(IEnumerable<MenuItemDefinition> topLevel, Action<string>? onItemInvoked = null)
	{
		_onItemInvoked = onItemInvoked;

		_definitionsById.Clear();
		foreach (var t in topLevel)
		{
			IndexDefinitions(t);
		}

		MacOSMenu.SetMainMenu(topLevel, id =>
		{
			if (_definitionsById.TryGetValue(id, out var def) && def.Enabled)
			{
				_onItemInvoked?.Invoke(id);
			}
		});
	}

	private static void IndexDefinitions(MenuItemDefinition def)
	{
		_definitionsById[def.Id] = def;
		foreach (var c in def.Children)
		{
			IndexDefinitions(c);
		}
	}

	/// <summary>
	/// Enables or disables a menu item by its Id.
	/// </summary>
	public static void SetEnabled(string id, bool enabled)
	{
		if (_definitionsById.TryGetValue(id, out var def))
		{
			def.Enabled = enabled;
		}
		MacOSMenu.SetEnabled(id, enabled);
	}
}
