using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// Managed API over the native NSMenu bridge (UNONative).
/// </summary>
internal static unsafe class MacOSMenu
{
	private static Action<string>? _invoked;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static void OnClick(sbyte* id)
	{
		var str = Marshal.PtrToStringUTF8((nint)id);
		if (str is not null)
		{
			_invoked?.Invoke(str);
		}
	}

	public static void SetMainMenu(IEnumerable<MacOSMenuService.MenuItemDefinition> topLevel, Action<string>? onInvoked)
	{
		_invoked = onInvoked;
		NativeUno.uno_menu_set_click_callback(&OnClick);

		NativeUno.uno_menu_begin();
		foreach (var top in topLevel)
		{
			NativeUno.uno_menu_begin_top(top.Id, top.Title);
			EmitChildren(top.Children);
			NativeUno.uno_menu_end_top();
		}
		NativeUno.uno_menu_commit();
	}

	private static void EmitChildren(IList<MacOSMenuService.MenuItemDefinition> items)
	{
		foreach (var item in items)
		{
			if (item.IsSeparator)
			{
				NativeUno.uno_menu_add_separator();
				continue;
			}

			if (item.Children.Count > 0)
			{
				NativeUno.uno_menu_begin_submenu(item.Id, item.Title);
				EmitChildren(item.Children);
				NativeUno.uno_menu_end_submenu();
			}
			else
			{
				NativeUno.uno_menu_add_item(item.Id, item.Title, item.KeyEquivalent);
			}
		}
	}

	public static void SetEnabled(string id, bool enabled) => NativeUno.uno_menu_set_enabled(id, enabled);
}
