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
		["edit"] = "textbox",
		["hyperlink"] = "link",
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
		["radiobutton"] = "radio",
		["radio button"] = "radio",
		["scrollbar"] = "scrollbar",
		["scroll bar"] = "scrollbar",
		["slider"] = "slider",
		["spinner"] = "spinner",
		["statusbar"] = "status",
		["status bar"] = "status",
		["tab"] = "tablist",
		["tabitem"] = "tab",
		["tab item"] = "tab",
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
		["header"] = "group",
		["headeritem"] = "group",
		["header item"] = "group",
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
		["xcuielementtypebutton"] = "button",
		["axcheckbox"] = "checkbox",
		["xcuielementtypecheckbox"] = "checkbox",
		["axradiobutton"] = "radio",
		["xcuielementtyperadiobutton"] = "radio",
		["axpopupbutton"] = "combobox",
		["xcuielementtypepopupbutton"] = "combobox",
		["axcombobox"] = "combobox",
		["xcuielementtypecombobox"] = "combobox",
		["axtextfield"] = "textbox",
		["axtokentextfield"] = "textbox",
		["axtextarea"] = "textbox",
		["xcuielementtypetextfield"] = "textbox",
		["xcuielementtypesecuretextfield"] = "textbox",
		["xcuielementtypetextview"] = "textbox",
		["xcuielementtypesearchfield"] = "textbox",
		["axstatictext"] = "text",
		["xcuielementtypestatictext"] = "text",
		["axslider"] = "slider",
		["xcuielementtypeslider"] = "slider",
		["axscrollbar"] = "scrollbar",
		["axscrollarea"] = "scrollarea",
		["axlist"] = "list",
		["xcuielementtypelist"] = "list",
		["axrow"] = "listitem",
		["xcuielementtyperow"] = "listitem",
		["axoutline"] = "tree",
		["axtable"] = "table",
		["axmenubar"] = "menubar",
		["axmenu"] = "menu",
		["axmenuitem"] = "menuitem",
		["axwindow"] = "window",
		["xcuielementtypewindow"] = "window",
		["axgroup"] = "group",
		["xcuielementtypegroup"] = "group",
		["axtoolbar"] = "toolbar",
		["axlink"] = "link",
		["xcuielementtypelink"] = "link",
		["axheading"] = "heading",
		["aximage"] = "image",
		["xcuielementtypeimage"] = "image",
		["axprogressindicator"] = "progressbar",
		["axsplitter"] = "separator",
		["axsplitgroup"] = "group",
		["axtabgroup"] = "tablist",
		["axdisclosuretriangle"] = "button",
		["xcuielementtypeswitch"] = "switch",
	};

	// ARIA roles already lowercase + close to canonical; collapse a few synonyms.
	private static readonly Dictionary<string, string> s_wasm = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["button"] = "button",
		["checkbox"] = "checkbox",
		["radio"] = "radio",
		["radiogroup"] = "group",
		["combobox"] = "combobox",
		["listbox"] = "combobox",
		["textbox"] = "textbox",
		["searchbox"] = "textbox",
		["textarea"] = "textbox",
		["textbox.multiline"] = "textbox",
		["spinbutton"] = "spinner",
		["slider"] = "slider",
		["link"] = "link",
		["heading"] = "heading",
		["img"] = "image",
		["progressbar"] = "progressbar",
		["scrollbar"] = "scrollbar",
		["list"] = "list",
		["listitem"] = "listitem",
		["menu"] = "menu",
		["menuitem"] = "menuitem",
		["menubar"] = "menubar",
		["separator"] = "separator",
		["status"] = "status",
		["tab"] = "tab",
		["tablist"] = "tablist",
		["tabpanel"] = "tabpanel",
		["tree"] = "tree",
		["treeitem"] = "treeitem",
		["document"] = "document",
		["group"] = "group",
		["select"] = "combobox",
		["region"] = "group",
		["main"] = "group",
		["navigation"] = "group",
		["search"] = "group",
		["form"] = "group",
		["tooltip"] = "tooltip",
		["switch"] = "switch",
		["input"] = "textbox",
		["h1"] = "heading",
		["h2"] = "heading",
		["h3"] = "heading",
		["h4"] = "heading",
		["h5"] = "heading",
		["h6"] = "heading",
	};

	public static string Normalize(string? rawRole, AppiumPlatform platform, int? level = null, string? landmark = null)
	{
		if (level is not null)
		{
			return "heading";
		}

		if (!string.IsNullOrWhiteSpace(landmark))
		{
			return "landmark";
		}

		if (string.IsNullOrWhiteSpace(rawRole))
		{
			return string.Empty;
		}

		var key = rawRole.Trim();
		if (platform == AppiumPlatform.Windows &&
			key.StartsWith("ControlType.", System.StringComparison.OrdinalIgnoreCase))
		{
			key = key["ControlType.".Length..];
		}

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
