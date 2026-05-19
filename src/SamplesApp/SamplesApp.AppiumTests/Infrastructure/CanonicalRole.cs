#nullable enable

using System.Collections.Generic;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Maps platform-specific role strings to a canonical lowercase token so
/// snapshots can be diffed across Win32 UIA, macOS NSAccessibility, and the
/// WASM ARIA bridge. Mapping is intentionally narrow: anything unmapped
/// falls through verbatim (after lowercase + trim) so we surface unknowns
/// in the snapshot rather than silently collapsing them.
/// </summary>
internal static class CanonicalRole
{
	/// <summary>
	/// Canonical roles seeded from <c>Microsoft.UI.Xaml.Automation.Peers.AutomationControlType</c>.
	/// Keep aligned with WinUI's enum so a future WinUI 3 baseline diff stays apples-to-apples.
	/// </summary>
	private static readonly Dictionary<string, string> s_win32 = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["button"] = "button",
		["calendar"] = "calendar",
		["checkbox"] = "checkbox",
		["check box"] = "checkbox",
		["combobox"] = "combobox",
		["combo box"] = "combobox",
		["edit"] = "edit",
		["hyperlink"] = "hyperlink",
		["image"] = "image",
		["listitem"] = "listitem",
		["list item"] = "listitem",
		["list"] = "list",
		["menu"] = "menu",
		["menubar"] = "menubar",
		["menu bar"] = "menubar",
		["menuitem"] = "menuitem",
		["menu item"] = "menuitem",
		["progressbar"] = "progressbar",
		["progress bar"] = "progressbar",
		["radiobutton"] = "radiobutton",
		["radio button"] = "radiobutton",
		["scrollbar"] = "scrollbar",
		["scroll bar"] = "scrollbar",
		["slider"] = "slider",
		["spinner"] = "spinner",
		["statusbar"] = "statusbar",
		["status bar"] = "statusbar",
		["tab"] = "tab",
		["tabitem"] = "tabitem",
		["tab item"] = "tabitem",
		["text"] = "text",
		["toolbar"] = "toolbar",
		["tool bar"] = "toolbar",
		["tooltip"] = "tooltip",
		["tool tip"] = "tooltip",
		["tree"] = "tree",
		["treeitem"] = "treeitem",
		["tree item"] = "treeitem",
		["custom"] = "custom",
		["group"] = "group",
		["thumb"] = "thumb",
		["datagrid"] = "datagrid",
		["data grid"] = "datagrid",
		["dataitem"] = "dataitem",
		["data item"] = "dataitem",
		["document"] = "document",
		["splitbutton"] = "splitbutton",
		["split button"] = "splitbutton",
		["window"] = "window",
		["pane"] = "pane",
		["header"] = "header",
		["headeritem"] = "headeritem",
		["header item"] = "headeritem",
		["table"] = "table",
		["titlebar"] = "titlebar",
		["title bar"] = "titlebar",
		["separator"] = "separator",
		["semanticzoom"] = "semanticzoom",
		["semantic zoom"] = "semanticzoom",
		["appbar"] = "appbar",
		["app bar"] = "appbar",
	};

	// NSAccessibility roles are typically "AXButton", "AXTextField", etc.
	private static readonly Dictionary<string, string> s_macos = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["axbutton"] = "button",
		["axcheckbox"] = "checkbox",
		["axradiobutton"] = "radiobutton",
		["axpopupbutton"] = "combobox",
		["axcombobox"] = "combobox",
		["axtextfield"] = "edit",
		["axtextarea"] = "edit",
		["axstatictext"] = "text",
		["axslider"] = "slider",
		["axscrollbar"] = "scrollbar",
		["axscrollarea"] = "scrollarea",
		["axlist"] = "list",
		["axrow"] = "listitem",
		["axoutline"] = "tree",
		["axtable"] = "table",
		["axmenubar"] = "menubar",
		["axmenu"] = "menu",
		["axmenuitem"] = "menuitem",
		["axwindow"] = "window",
		["axgroup"] = "group",
		["axtoolbar"] = "toolbar",
		["axlink"] = "hyperlink",
		["axheading"] = "header",
		["aximage"] = "image",
		["axprogressindicator"] = "progressbar",
		["axsplitter"] = "separator",
		["axsplitgroup"] = "group",
		["axtabgroup"] = "tab",
		["axdisclosuretriangle"] = "splitbutton",
	};

	// ARIA roles already lowercase + close to canonical; collapse a few synonyms.
	private static readonly Dictionary<string, string> s_wasm = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["button"] = "button",
		["checkbox"] = "checkbox",
		["radio"] = "radiobutton",
		["radiogroup"] = "group",
		["combobox"] = "combobox",
		["listbox"] = "combobox",
		["textbox"] = "edit",
		["searchbox"] = "edit",
		["spinbutton"] = "spinner",
		["slider"] = "slider",
		["link"] = "hyperlink",
		["heading"] = "header",
		["img"] = "image",
		["progressbar"] = "progressbar",
		["scrollbar"] = "scrollbar",
		["list"] = "list",
		["listitem"] = "listitem",
		["menu"] = "menu",
		["menuitem"] = "menuitem",
		["menubar"] = "menubar",
		["separator"] = "separator",
		["status"] = "statusbar",
		["tab"] = "tab",
		["tablist"] = "tab",
		["tabpanel"] = "tabitem",
		["textbox.multiline"] = "edit",
		["tree"] = "tree",
		["treeitem"] = "treeitem",
		["document"] = "document",
		["group"] = "group",
		["region"] = "group",
		["main"] = "group",
		["navigation"] = "group",
		["search"] = "group",
		["form"] = "group",
		["tooltip"] = "tooltip",
	};

	public static string Normalize(string? rawRole, AppiumPlatform platform)
	{
		if (string.IsNullOrWhiteSpace(rawRole))
		{
			return string.Empty;
		}

		var key = rawRole.Trim();
		var table = platform switch
		{
			AppiumPlatform.Windows => s_win32,
			AppiumPlatform.Mac => s_macos,
			AppiumPlatform.Wasm => s_wasm,
			_ => null,
		};

		if (table is not null && table.TryGetValue(key, out var mapped))
		{
			return mapped;
		}

		return key.ToLowerInvariant();
	}
}
