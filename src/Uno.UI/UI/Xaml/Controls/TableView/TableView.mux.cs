// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using ThemeSettings = Microsoft.UI.System.ThemeSettings;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	private const string s_RowsRepeaterPartName = "PART_RowsRepeater";
	private const string s_HeaderRowPartName = "PART_HeaderRow";
	private const string s_HeaderHostPartName = "PART_HeaderHost";
	private const string s_EmptyStatePresenterPartName = "PART_EmptyStatePresenter";
	private const string s_ResizeGripperWidthKey = "TableViewResizeGripperWidth";
	// Matches TableViewResizeGripperWidth in the theme dictionaries; used when that key is missing or
	// unusable.
	private const double c_resizeGripperWidthFallback = 8.0;

	// Fallbacks mirroring the resources shipped in TableView.xaml.
	private const double c_defaultRowIndentSize = 16.0;
	private const double c_defaultRowExpanderSize = 24.0;
	private const double c_defaultGroupExpanderSize = 24.0;
	private const double c_defaultGroupExpanderSpacing = 8.0;
	private const string s_SortIndicatorSizeKey = "SortIndicatorSize";
	// Matches SortIndicatorSize in SortIndicator_themeresources.xaml; used only when that key is
	// missing or unusable.
	private const double c_sortIndicatorSizeFallback = 16.0;

	// cppwinrt's == compares raw ABI pointers, which can differ for the same object across a QI,
	// so fall back to canonical IUnknown identity.
	private static bool IsSameObject(object? left, object? right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		if (left is null || right is null)
		{
			return false;
		}

		// TODO Uno: .NET has no separate COM identity; reference identity is the canonical one.
		// Original C++: return left.as<IUnknown>() == right.as<IUnknown>();
		return ReferenceEquals(left, right);
	}

	private static ScrollViewer? FindScrollViewerAncestor(DependencyObject? start, DependencyObject? boundary)
	{
		var node = start;
		while (node is not null && !IsSameObject(node, boundary))
		{
			if (node is ScrollViewer sv)
			{
				return sv;
			}
			node = VisualTreeHelper.GetParent(node);
		}
		return null;
	}

	// Misses are the common case here; HasKey does not originate an error on a miss the way
	// Lookup (and therefore TryLookup) does.
	private static object? QuietLookup(IDictionary<object, object>? map, object boxedKey)
	{
		if (map is not null && map.ContainsKey(boxedKey))
		{
			return map[boxedKey];
		}
		return null;
	}

	// Reference-typed resources keep their identity across lookups; value-typed ones (x:Double,
	// Thickness, Color, ...) come back as a fresh box every time, so they compare by value.
	private static bool IsSameResource(object? left, object? right)
	{
		if (left is null || right is null)
		{
			return false;
		}
		if (IsSameObject(left, right))
		{
			return true;
		}

		// TODO Uno: IPropertyValue projection - a boxed value projects as its .NET type, so the runtime type is PropertyType.
		if (left.GetType() != right.GetType())
		{
			return false;
		}

		switch (left)
		{
			case bool leftValue: return leftValue == (bool)right;
			case double leftValue: return leftValue == (double)right;
			case float leftValue: return leftValue == (float)right;
			case int leftValue: return leftValue == (int)right;
			case uint leftValue: return leftValue == (uint)right;
			case long leftValue: return leftValue == (long)right;
			case string leftValue: return leftValue == (string)right;
			default: break;
		}

		if (left is Thickness leftThickness)
		{
			return leftThickness.Equals((Thickness)right);
		}
		if (left is Color leftColor)
		{
			return leftColor.Equals((Color)right);
		}
		if (left is CornerRadius leftCornerRadius)
		{
			return leftCornerRadius.Equals((CornerRadius)right);
		}
		if (left is global::Windows.UI.Text.FontWeight leftFontWeight)
		{
			return leftFontWeight.Weight == ((global::Windows.UI.Text.FontWeight)right).Weight;
		}
		return false;
	}

	// Every key EnsureActiveThemeDictionary can select.
	private const string s_highContrastWhiteKey = "HighContrastWhite";
	private const string s_highContrastBlackKey = "HighContrastBlack";
	private const string s_highContrastCustomKey = "HighContrastCustom";

	// SystemThemingInterop::GetSystemHighContrastTheme: the variant comes from the system window
	// colours (including Xbox's video-safe pair). Empty when High Contrast is off.
	private static string SystemHighContrastVariantKey()
	{
		// TODO Uno: SPI_GETHIGHCONTRAST and GetSysColor have no cross-platform equivalent, so no system variant is
		// reported and High Contrast lookups start at the "HighContrast" dictionary.
#if !HAS_UNO
		HIGHCONTRASTW highContrast{};
		highContrast.cbSize = sizeof(highContrast);
		if (!::SystemParametersInfoW(SPI_GETHIGHCONTRAST, sizeof(highContrast), &highContrast, 0) ||
			!(highContrast.dwFlags & HCF_HIGHCONTRASTON))
		{
			return {};
		}

		const auto foreground = static_cast<unsigned int>(::GetSysColor(COLOR_WINDOWTEXT));
		const auto background = static_cast<unsigned int>(::GetSysColor(COLOR_WINDOW));
		if ((background == 0x00FFFFFF && foreground == 0x0) || (background == 0x00EBEBEB && foreground == 0x00101010))
		{
			return s_highContrastWhiteKey;
		}
		if ((background == 0x0 && foreground == 0x00FFFFFF) || (background == 0x00101010 && foreground == 0x00EBEBEB))
		{
			return s_highContrastBlackKey;
		}
		return s_highContrastCustomKey;
#else
		_ = s_highContrastCustomKey;
		return "";
#endif
	}

	// EnsureActiveThemeDictionary's selection order: the HC variant (a subtree-requested Light/Dark
	// maps to HighContrastWhite/Black, otherwise the system variant), then "HighContrast", then the
	// base theme, then "Default".
	private static List<string> ThemeDictionaryKeys(
		bool highContrast,
		string highContrastVariant,
		bool isLight)
	{
		List<string> keys = new();
		if (highContrast)
		{
			if (!string.IsNullOrEmpty(highContrastVariant))
			{
				keys.Add(highContrastVariant);
			}
			keys.Add("HighContrast");
		}
		keys.Add(isLight ? "Light" : "Dark");
		keys.Add("Default");
		return keys;
	}

	// A class rather than a struct: C++ only passes it by reference and GlobalValue caches into it.
	private sealed class ElementResourceQuery
	{
		public object boxedKey = "";
		// Candidate theme-dictionary keys for the ELEMENT's theme, in selection order.
		public List<string> themeKeys = new();
		// The same for the APP-wide theme, which is what a code-issued Lookup selects with.
		public List<string> appThemeKeys = new();
		// Global theme resources for this key, which Lookup appends to every dictionary's scope.
		// Resolved lazily through an empty dictionary, which has no scope of its own.
		public object? globalValue = null;
		public bool globalResolved = false;

		public object? GlobalValue()
		{
			if (!globalResolved)
			{
				globalResolved = true;
				globalValue = QuietLookup(new ResourceDictionary(), boxedKey);
			}
			return globalValue;
		}
	}

	private static ResourceDictionary? SelectThemeDictionary(ResourceDictionary dict, List<string> themeKeys)
	{
		if (dict.ThemeDictionaries is { } themeDicts)
		{
			foreach (var themeKey in themeKeys)
			{
				if (QuietLookup(themeDicts, themeKey) is ResourceDictionary themed)
				{
					return themed;
				}
			}
		}
		return null;
	}

	// Lookup/HasKey are full-scope (own, merged, APP-theme dictionary, global), and enumerating a
	// dictionary forces CResourceDictionary::EnsureAll (realizes every deferred resource - the whole
	// WinUI theme). So "own entry" is decided by comparing the hit with the one value that full-scope
	// Lookup would have returned had there been no own entry: the nearest merged scope (last-to-first),
	// then the app-theme dictionary, then global. Comparing with ONE candidate rather than any scope
	// keeps an own entry that merely equals an unselected merged or theme value.
	//
	// Approximations, both deliberate:
	// - A merged hit equal to global is skipped as the global fallback. GetKeyNoRefImpl actually lets
	//   the last merged dictionary answer with global first, but Tabular keys are global (the dll's
	//   theme resources), so mirroring that sends nearly every lookup down through the app's merged
	//   dictionaries (XamlControlsResources included) - prohibitively expensive.
	// - An own value-typed entry equal to the global default is indistinguishable from the fallback
	//   (value types come back as fresh boxes) and is treated as not own; only an ancestor override of
	//   the same key can then differ from what the framework returns.
	private static bool IsOwnEntry(ResourceDictionary dict, object found, ElementResourceQuery query)
	{
		// GetKeyNoRefImpl searches the LAST merged dictionary with global scope, so when the key
		// exists globally that first search always answers and the loop stops; only a key with no
		// global value reaches earlier merged dictionaries, and there a full-scope Lookup is exact.
		object? fallback = null;
		var global = query.GlobalValue();
		if (dict.MergedDictionaries is { } merged)
		{
			for (var i = merged.Count; i-- > 0 && fallback is null;)
			{
				// Every Lookup ends at global; only a hit that differs from it is the merged scope's.
				var value = QuietLookup(merged[i], query.boxedKey);
				if (value is not null && !IsSameResource(value, global))
				{
					fallback = value;
				}
			}
		}
		if (fallback is null)
		{
			if (SelectThemeDictionary(dict, query.appThemeKeys) is { } themed)
			{
				var value = QuietLookup(themed, query.boxedKey);
				if (value is not null && !IsSameResource(value, global))
				{
					fallback = value;
				}
			}
		}
		if (fallback is null)
		{
			fallback = query.GlobalValue();
		}

		return !IsSameResource(found, fallback);
	}

	// Framework (GetKeyNoRefImpl) order: own entries, MergedDictionaries last-to-first, then the
	// active theme dictionary - chosen from the ELEMENT's theme, which a code-issued Lookup cannot see.
	private static object? LookupInDictionaryScope(ResourceDictionary? dict, ElementResourceQuery query)
	{
		if (dict is null)
		{
			return null;
		}

		if (QuietLookup(dict, query.boxedKey) is { } ownFound)
		{
			if (IsOwnEntry(dict, ownFound, query))
			{
				return ownFound;
			}
		}

		if (dict.MergedDictionaries is { } merged)
		{
			for (var i = merged.Count; i-- > 0;)
			{
				if (LookupInDictionaryScope(merged[i], query) is { } found)
				{
					return found;
				}
			}
		}

		if (SelectThemeDictionary(dict, query.themeKeys) is { } themed)
		{
			return LookupInDictionaryScope(themed, query);
		}

		return null;
	}

	// Resolves a key the way {ThemeResource} would from `start`: ancestors nearest first, then
	// Application.Resources, then global theme resources, with theme dictionaries selected as
	// EnsureActiveThemeDictionary does (see ThemeDictionaryKeys).
	private static object? LookupElementResource(FrameworkElement? start, string key, bool highContrast = false)
	{
		ElementResourceQuery query = new();
		query.boxedKey = key;

		var systemHighContrastVariant = SystemHighContrastVariantKey();

		// Theme-scoped resources must resolve against the element's ActualTheme.
		// High Contrast is orthogonal to ActualTheme; callers pass cached HC state for hot-path brush lookups.
		var theme = start is not null ? start.ActualTheme : ElementTheme.Default;
		var elementHighContrastVariant = systemHighContrastVariant;
		if (highContrast)
		{
			// A subtree that requested a theme selects the matching HC variant, not the system's.
			for (var requester = start; requester is not null; requester = requester.Parent as FrameworkElement)
			{
				if (requester.RequestedTheme != ElementTheme.Default)
				{
					elementHighContrastVariant = theme == ElementTheme.Light ? s_highContrastWhiteKey : s_highContrastBlackKey;
					break;
				}
			}
		}
		query.themeKeys = ThemeDictionaryKeys(highContrast, elementHighContrastVariant, theme == ElementTheme.Light);

		// What a code-issued Lookup selects: no subtree theme, so the app-wide base and system HC.
		var appIsLight = theme == ElementTheme.Light;
		if (Application.Current is { } app)
		{
			appIsLight = app.RequestedTheme == ApplicationTheme.Light;
		}
		query.appThemeKeys = ThemeDictionaryKeys(!string.IsNullOrEmpty(systemHighContrastVariant), systemHighContrastVariant, appIsLight);

		var walker = start;
		while (walker is not null)
		{
			if (LookupInDictionaryScope(walker.Resources, query) is { } found)
			{
				return found;
			}
			walker = walker.Parent as FrameworkElement;
		}

		if (Application.Current is { } currentApp)
		{
			if (LookupInDictionaryScope(currentApp.Resources, query) is { } found)
			{
				return found;
			}
		}

		return query.GlobalValue();
	}

	private static bool WantsHorizontalLines(TableViewGridLinesVisibility visibility) =>
		visibility == TableViewGridLinesVisibility.Horizontal ||
		visibility == TableViewGridLinesVisibility.All;

	private static double TerminalEdgeTolerance(FrameworkElement? element)
	{
		double scale = 1.0;
		try
		{
			if (element is not null)
			{
				if (element.XamlRoot is { } root)
				{
					var rasterizationScale = root.RasterizationScale;
					if (double.IsFinite(rasterizationScale) && rasterizationScale > 0.0)
					{
						scale = rasterizationScale;
					}
				}
			}
		}
		catch (Exception)
		{
		}

		// Half a physical pixel, widened slightly because transformed bounds are float-backed and
		// can land microscopically beyond that boundary after layout rounding.
		const double layoutEpsilonPixels = 1.0 / 64.0;
		return (0.5 + layoutEpsilonPixels) / scale;
	}

	private static bool TryGetBoundsRelativeTo(
		FrameworkElement? element,
		UIElement? relativeTo,
		out Rect bounds)
	{
		bounds = default;
		try
		{
			if (element is null || relativeTo is null || !element.IsLoaded ||
				element.ActualWidth <= 0.0 || element.ActualHeight <= 0.0)
			{
				return false;
			}

			bounds = element.TransformToVisual(relativeTo).TransformBounds(
				new Rect(0.0f, 0.0f, (float)element.ActualWidth, (float)element.ActualHeight));
		}
		catch (Exception)
		{
			return false;
		}

		return double.IsFinite(bounds.X) &&
			double.IsFinite(bounds.Y) &&
			double.IsFinite(bounds.Width) &&
			double.IsFinite(bounds.Height);
	}

	private static TableViewResourceCache GetTableViewResourceCache(TableView owner)
	{
		// Per-instance member (not a process-global map) so multi-UI-thread instances never share state.
		return owner.GetResourceCacheInternal();
	}

	private static void InvalidateTableViewResourceCache(TableView owner)
	{
		var cache = owner.GetResourceCacheInternal();
		cache.density.hasRowMinHeight = false;
		cache.density.hasCellPadding = false;
		cache.density.hasHeaderCellPadding = false;
		cache.font.hasCellFontSize = false;
		cache.hierarchy.hasRowIndentSize = false;
		cache.hierarchy.hasRowExpanderSize = false;
		cache.hierarchy.hasGroupHeaderContentOffset = false;
		cache.gridLine.hasBrush = false;
	}

	private static bool ShouldRefreshFrozenColumnsForScroll(TableView owner, double horizontalOffset)
	{
		var cache = GetTableViewResourceCache(owner);
		if (!cache.hasLastFrozenColumnsHorizontalOffset ||
			Math.Abs(cache.lastFrozenColumnsHorizontalOffset - horizontalOffset) >= 0.5)
		{
			cache.hasLastFrozenColumnsHorizontalOffset = true;
			cache.lastFrozenColumnsHorizontalOffset = horizontalOffset;
			return true;
		}
		return false;
	}

	private static double DensityRowMinHeightFallback(TableViewDensity density)
	{
		switch (density)
		{
			case TableViewDensity.Compact: return 30.0;
			case TableViewDensity.Comfortable: return 48.0;
			default: return 40.0; // Standard
		}
	}

	private static Thickness DensityCellPaddingFallback(TableViewDensity density)
	{
		switch (density)
		{
			case TableViewDensity.Compact: return ThicknessHelper.FromLengths(8, 2, 8, 2);
			case TableViewDensity.Comfortable: return ThicknessHelper.FromLengths(8, 8, 8, 8);
			default: return ThicknessHelper.FromLengths(8, 4, 8, 4); // Standard
		}
	}

	// The stock style always supplies TabularSurfaceGridLineBrush; on a miss draw only in High Contrast.
	private static Brush? CreateGridLineFallbackBrush(FrameworkElement start, bool highContrast)
	{
		if (!highContrast)
		{
			return null;
		}

		return new SolidColorBrush(
			LookupElementResource(start, "SystemColorWindowTextColor", true) is Color resolvedColor
				? resolvedColor
				: Colors.White);
	}

	internal Brush? GetGridLineBrush()
	{
		var cache = GetTableViewResourceCache(this);
		var highContrast = IsHighContrast();
		var theme = ActualTheme;
		if (cache.gridLine.hasBrush &&
			cache.gridLine.theme == theme &&
			cache.gridLine.highContrast == highContrast)
		{
			return cache.gridLine.brush;
		}

		var brush = LookupElementResource(this, "TabularSurfaceGridLineBrush", highContrast) as Brush;
		if (brush is null)
		{
			brush = CreateGridLineFallbackBrush(this, highContrast);
		}

		cache.gridLine.hasBrush = true;
		cache.gridLine.theme = theme;
		cache.gridLine.highContrast = highContrast;
		cache.gridLine.brush = brush;
		return brush;
	}

#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
	// The selector's templates are per-instance, so the template/RecyclePool/row cycle is collected
	// by the GC and Detach is not needed for lifetime.

	// Original destructor logic (not executed):
	// TableView::~TableView()
	// {
	//     IgnoreTip(TableViewTip::IgnoreReason::Unloaded);
	//     // Unloaded can run after the window is destroyed, or not at all; TableView_Instance goes out at most once
	//     // either way.
	//     TableViewTelemetry::EndSession(m_telemetrySession);
	//     // Must run while this control still holds the selector: once anything has been recycled the
	//     // pools hang off the cached templates and close a cycle the reference tracker cannot walk.
	//     // Destructor runs off the reference tracker's teardown path (e.g. UIAffinityReleaseQueue),
	//     // not necessarily via a direct Release() call, so plain get() can observe the tracker handle
	//     // already invalidated and assert/fail-fast in chk builds. safe_get() is the documented-safe
	//     // accessor for tracker_ref from a destructor (see tracker_ref.h and ItemsView/ScrollView).
	//     if (auto const selector = m_rowTemplateSelector.safe_get())
	//     {
	//         winrt::get_self<::TableViewRowTemplateSelector>(selector)->Detach();
	//     }
	//
	//     // Unloaded may not have run; release ThemeSettings and drop the Destroying subscription.
	//     ReleaseThemeSettings();
	// }
#endif

	public TableView()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_TableView);

		this.SetTabularDefaultStyleKey();

		// Construction is only a setup heuristic; equivalent resources or custom templates are valid.
		// TODO Uno: TabularControlsResources is not reachable from Uno.UI, so the setup hint is not emitted.
#if !HAS_UNO
		static thread_local bool s_reportedMissingResources{ false };
		if (!TabularControlsResources::HasBeenCreatedOnCurrentThread() && !s_reportedMissingResources)
		{
			s_reportedMissingResources = TVDiag::LogDebugger(
				L"TableView setup: if using the default template, merge "
				L"<TabularControlsResources xmlns=\"using:Microsoft.UI.Xaml.Controls.Tabular\" /> into "
				L"App.xaml > Application.Resources.MergedDictionaries after XamlControlsResources, before overrides. "
				L"Equivalent resources or a complete custom template are also supported.");
		}
#endif

		// Columns must be observable; OnColumnsPropertyChanged owns the VectorChanged subscription to avoid duplicate callbacks.
		var columns = new ObservableVector<TableViewColumn>();
		Columns = columns;

		WeakReference<TableView> weakThis = new(this);

		// Use bubbling KeyDown so focused editors can consume typing keys before row navigation.
		m_keyDownHandler = new KeyEventHandler(
			(object sender, KeyRoutedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					if (strongThis.m_navKeyHandledEarly)
					{
						strongThis.m_navKeyHandledEarly = false;
						return;
					}
					strongThis.OnKeyDownForNavigation(sender, args);
				}
			});
		// Ancestor PART_BodyScroller marks nav keys Handled before they bubble here; handledEventsToo:true lets us still act on them.
		// AddHandler takes the handler as IInspectable, so the delegate must be boxed (see RoutedEventHelpers.h).
		AddHandler(UIElement.KeyDownEvent, m_keyDownHandler, true /* handledEventsToo */);

		m_keyUpHandler = new KeyEventHandler(
			(object sender, KeyRoutedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnKeyUpForHeaderSort(sender, args);
				}
			});
		// Space on a focused header arms on KeyDown and sorts on an unhandled KeyUp; handledEventsToo
		// lets a handled KeyUp leave the arm intact rather than consuming it.
		AddHandler(UIElement.KeyUpEvent, m_keyUpHandler, true /* handledEventsToo */);

		RoutedEventHandler headerSortLostFocusHandler =
			(object _, RoutedEventArgs _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_headerSortSpaceArmedColumn = null;
				}
			};
		LostFocus += headerSortLostFocusHandler;
		m_headerSortLostFocusRevoker.Disposable = Disposable.Create(() => LostFocus -= headerSortLostFocusHandler);
		// Tunneling PreviewKeyDown runs before the framework's built-in focus navigation; snapshot the
		// currently focused row there so OnKeyDownForNavigation anchors on the pre-move index.
		m_previewKeyDownHandler = new KeyEventHandler(
			(object sender, KeyRoutedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnPreviewKeyDownForNavigation(sender, args);
				}
			});
		AddHandler(UIElement.PreviewKeyDownEvent, m_previewKeyDownHandler, false /* handledEventsToo */);

		TypedEventHandler<UIElement, GettingFocusEventArgs> tableGettingFocusHandler =
			(UIElement sender, GettingFocusEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					try
					{
						strongThis.OnTableGettingFocus(sender, args);
					}
					catch (Exception ex)
					{
						TVDiag.LogRetailF("[TableView] Resolving the focus target failed (HRESULT 0x%08X).", unchecked((uint)ex.HResult));
					}
				}
			};
		GettingFocus += tableGettingFocusHandler;
		m_tableGettingFocusRevoker.Disposable = Disposable.Create(() => GettingFocus -= tableGettingFocusHandler);

		// handledEventsToo: rows and headers mark their presses handled, and a press anywhere in the
		// table is the user taking focus over from a pending re-shape restore.
		AddHandler(
			UIElement.PointerPressedEvent,
			new PointerEventHandler(
				(object _, PointerRoutedEventArgs _) =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						strongThis.m_cellIntentColumn = -1;
						strongThis.m_drillInOffset = -1.0;
						strongThis.m_drillRestoreOffset = -1.0;
						strongThis.CancelReshapeFocusRestoreForUserInput();
					}
				}),
			true /* handledEventsToo */);

		// The body scroller handles the wheel. Any wheel scroll is the user's own, so Left no longer undoes
		// the drill-in's reveal; Ctrl+wheel zooms rather than scrolls.
		AddHandler(
			UIElement.PointerWheelChangedEvent,
			new PointerEventHandler(
				(object _, PointerRoutedEventArgs args) =>
				{
					if (weakThis.TryGetTarget(out var strongThis) &&
						(args.KeyModifiers & global::Windows.System.VirtualKeyModifiers.Control) != global::Windows.System.VirtualKeyModifiers.Control)
					{
						strongThis.m_drillInOffset = -1.0;
						strongThis.m_drillRestoreOffset = -1.0;
					}
				}),
			true /* handledEventsToo */);

		// Headers need no direction stamp (see RebuildHeaders); the rebuild keeps them in step with the body.
		RegisterPropertyChangedCallback(
			FrameworkElement.FlowDirectionProperty,
			(DependencyObject _, DependencyProperty _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.QueueRebuildHeaders();
					// Body cells carry the same stamp as a one-sided BorderThickness, and realized rows
					// are not rebuilt by the header pass - refresh them or the body grid lines stay on
					// the edge the previous direction chose.
					strongThis.RefreshGridLinesOnRealizedRows();
					strongThis.QueueTerminalGridLineRefresh();
				}
			});

		// Editing gestures. Separate from the navigation handlers above; the key sets are disjoint.
		// handledEventsToo is required because a single-line TextBox marks Enter handled and the commit
		// must still run - each key case in OnKeyDownForEditing owns its own Handled policy.
		m_editingKeyDownHandler = new KeyEventHandler(
			(object sender, KeyRoutedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnKeyDownForEditing(sender, args);
				}
			});
		AddHandler(UIElement.KeyDownEvent, m_editingKeyDownHandler, true /* handledEventsToo */);

		// Commit when focus leaves the open editor. Uses the typed LosingFocus event so the incoming
		// focus target is known - a plain LostFocus cannot tell "moved inside the editor" from
		// "clicked another cell".
		TypedEventHandler<UIElement, LosingFocusEventArgs> losingFocusHandler =
			(UIElement sender, LosingFocusEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnLosingFocusForEditing(sender, args);
				}
			};
		LosingFocus += losingFocusHandler;
		m_editingLosingFocusRevoker.Disposable = Disposable.Create(() => LosingFocus -= losingFocusHandler);

		// ThemeSettings needs a WindowId, so it can only be created once we are in a tree with a XamlRoot.
		// Create it (and subscribe to Changed) on Loaded; its Changed event is raised on this UI thread,
		// which is why the old AccessibilitySettings dispatcher-marshaling plumbing is gone.
		RoutedEventHandler loadedHandler =
			(object sender, RoutedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnTableViewLoaded(sender, args);
				}
			};
		Loaded += loadedHandler;
		m_loadedRevoker.Disposable = Disposable.Create(() => Loaded -= loadedHandler);

		// Null ItemsSource on unload so queued repeater work cannot run on a detached subtree.
		RoutedEventHandler unloadedHandler =
			(object _, RoutedEventArgs _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnTableViewUnloaded();
				}
			};
		Unloaded += unloadedHandler;
		m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= unloadedHandler);
	}

	private void OnTableViewLoaded(object sender, RoutedEventArgs args)
	{
		TableViewTelemetry.BeginSession(m_telemetrySession);
		TableViewTelemetry.ReportFirstLoad();
		ReclaimItemsSource();
		// The first of the session's two samples; a session that ends without Unloaded carries this one alone.
		SampleInstanceTelemetry();

		// ThemeSettings requires a WindowId, so it can only be created once we have a XamlRoot.
		if (m_themeSettings is not null)
		{
			return; // already created for this hosting session
		}

		var xamlRoot = XamlRoot;
		if (xamlRoot is null)
		{
			return;
		}

		try
		{
			// ContentIslandEnvironment can be null during teardown / unusual hosts.
			if (xamlRoot.ContentIslandEnvironment is { } env)
			{
				var themeSettings = ThemeSettings.CreateForWindowId(env.AppWindowId);
				m_themeSettings = themeSettings;
				m_isHighContrast = themeSettings.HighContrast;
				// Changed is raised on this UI thread, so the handler can touch XAML directly.
				WeakReference<TableView> weakThis = new(this);
				TypedEventHandler<ThemeSettings, object?> changedHandler =
					(ThemeSettings s, object? a) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnThemeSettingsChanged(s, a);
						}
					};
				themeSettings.Changed += changedHandler;
				m_themeSettingsChangedRevoker.Disposable = Disposable.Create(() => themeSettings.Changed -= changedHandler);

				// Destroying is raised on the window's thread; a cross-thread host keeps the Unloaded-path release.
				// TODO Uno: AppWindow.Destroying is not implemented, so ThemeSettings is released on Unloaded only.
#if !HAS_UNO
				auto const appWindowId = env.AppWindowId();
				auto const hwnd = winrt::Microsoft::UI::GetWindowFromWindowId(appWindowId);
				if (::GetWindowThreadProcessId(hwnd, nullptr) != ::GetCurrentThreadId())
				{
					return;
				}
				if (auto appWindow = winrt::Microsoft::UI::Windowing::AppWindow::GetFromWindowId(appWindowId))
				{
					m_appWindowDestroyingToken = appWindow.Destroying(
						[weakThis = get_weak()](auto const&, auto const&)
						{
							if (auto strongThis = weakThis.get())
							{
								strongThis->ReleaseThemeSettings();
							}
						});
					m_appWindow = std::move(appWindow);
				}
#endif
			}
		}
		catch (Exception)
		{
			// Best-effort; IsHighContrast falls back to a one-shot AccessibilitySettings read.
		}
	}

	private void OnThemeSettingsChanged(ThemeSettings sender, object? args)
	{
		// Raised on the control's UI thread (no marshaling required). HC can toggle without a theme
		// change, so invalidate the HC-dependent resource cache and refresh realized visuals directly.
		try
		{
			m_isHighContrast = sender.HighContrast;
		}
		catch (Exception)
		{
			// Best-effort during teardown; keep the previous HC state if the read fails.
		}

		InvalidateTableViewResourceCache(this);
		if (IsLoaded)
		{
			RebuildHeaders();
			RefreshGridLinesOnRealizedRows();
			QueueTerminalGridLineRefresh();
			// Lead-cell padding and indent are imperative; re-derive from the new metrics.
			RefreshRealizedRowHierarchyState();
		}
	}

	protected override void OnApplyTemplate()
	{
		try
		{
			m_headerSortSpaceArmedColumn = null;
			IgnoreTip(TableViewTip.IgnoreReason.Retemplated);
			RevokeTipInitializationLayout();
			TableViewTip.ResetInitialization(m_tip);
			m_templateApplied = true;
			m_templateIncomplete = false;
			// The template is expanded before this runs, so with a source attached Initialize's inputs are ready.
			if (m_activeSource is not null)
			{
				StartTipInitialization();
			}
			base.OnApplyTemplate();
			InvalidateTableViewResourceCache(this);
			CancelHeaderFocusRestore();

			if (m_pendingGroupFocusLayoutToken.Disposable is not null)
			{
				m_pendingGroupFocusLayoutToken.Disposable = null;
			}
			if (m_pendingGroupRowRefreshLayoutToken.Disposable is not null)
			{
				m_pendingGroupRowRefreshLayoutToken.Disposable = null;
			}
			if (m_terminalGridLinesLayoutToken.Disposable is not null)
			{
				m_terminalGridLinesLayoutToken.Disposable = null;
			}
			if (m_terminalGridLineRow?.TryGetTarget(out var terminalRow) == true)
			{
				terminalRow.SetTerminalGridLineSuppression(new());
			}
			if (m_terminalGridLineGroupHeader?.TryGetTarget(out var terminalHeader) == true)
			{
				terminalHeader.SetTerminalBottomGridLineSuppression(false);
			}
			m_terminalGridLineRowSizeChangedRevoker.Disposable = null;
			m_terminalGridLineRow = null;
			m_terminalGridLineGroupHeader = null;
			m_suppressTrailingGridLine = false;
			m_suppressBottomGridLine = false;
			m_terminalGridLineGeometryRetryAvailable = true;
			m_terminalGridLineColumnIndex = -1;
			m_terminalGridLineHorizontalOffset = double.NaN;
			m_terminalGridLineVerticalOffset = double.NaN;
			if (m_rowsRepeater is { } oldRepeater)
			{
				// Drop per-template Loaded handlers so old elements cannot keep this alive.
				if (m_rowsRepeaterLoadedToken.Disposable is not null)
				{
					m_rowsRepeaterLoadedToken.Disposable = null;
				}

				// Release realized rows before detaching ElementClearing so rows can clear their owner.
				try
				{
					oldRepeater.ItemsSource = null;
				}
				catch (Exception)
				{
				}

				m_rowElementPreparedToken.Disposable = null;
				m_rowElementClearingToken.Disposable = null;
				m_rowElementIndexChangedToken.Disposable = null;
			}
			if (m_headerHost is { } oldHeaderHost)
			{
				// The abandoned band still parents its tooltips and UIElement Headers.
				ReleaseHeaderCells(oldHeaderHost);

				// Mirror the rowsRepeater Loaded cleanup for the header host.
				if (m_headerHostLoadedToken.Disposable is not null)
				{
					m_headerHostLoadedToken.Disposable = null;
				}

				// Auto-revoke would also release this on reassignment below, but the old band must not
				// raise focus events into a control whose template has already been swapped.
				m_headerHostGotFocusRevoker.Disposable = null;
			}
			m_headerContextRequestedRevoker.Disposable = null;
			m_templateRootContextRequestedRevoker.Disposable = null;

			WeakReference<TableView> weakThis = new(this);

			// Below the TableView so TableView.ContextFlyout (and any ancestor's) stays closed while a cell
			// is editing; the framework shows an element's ContextFlyout before its own handlers run.
			m_templateRootContextRequestedRevoker.Disposable = TableViewDetails.HookTemplateRootContextRequested(this,
				(UIElement _, ContextRequestedEventArgs args) =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						strongThis.SuppressContextRequestWhileEditing(args);
					}
				});
			if (m_bodyScroller is not null)
			{
				m_bodyScrollerViewChangedToken.Disposable = null;
				m_bodyScrollerSizeChangedRevoker.Disposable = null;
			}

			// Reset resolved-on-Loaded refs so re-templating re-resolves them against the new tree.
			m_headerRow = null;
			m_headerScroller = null;
			m_bodyScroller = null;

			m_bodyBringIntoViewRevoker.Disposable = null;
			m_rowsRepeater = GetTemplateChild(s_RowsRepeaterPartName) as ItemsRepeater;
			m_headerRow = GetTemplateChild(s_HeaderRowPartName) as FrameworkElement;
			m_headerHost = GetTemplateChild(s_HeaderHostPartName) as Panel;
			m_emptyStatePresenter = GetTemplateChild(s_EmptyStatePresenterPartName) as ContentControl;
			// Every part is looked up as optional, so nothing else reports a template without one. TableView's own template
			// has all three, so a missing one is the app's template.
			if (m_rowsRepeater is null || m_headerRow is null || m_headerHost is null)
			{
				m_templateIncomplete = true;
				TVDiag.LogRetailF("[TableView] The control template lacks PART_RowsRepeater, PART_HeaderRow or PART_HeaderHost.");
				StartTipInitialization();
				TableViewTip.IgnoreInitialization(m_tip, TableViewTip.IgnoreReason.AppTemplateIncomplete);
			}

			// Defer ScrollViewer ancestor lookup until Loaded because template parts are not fully connected here.
			if (m_headerHost is { } headerHost)
			{
				// Keeps XAML's tab walk from stopping on each header; the table is the single tab stop.
				headerHost.TabFocusNavigation = KeyboardNavigationMode.Once;

				// Update the shared column cursor whenever a header actually takes focus.
				RoutedEventHandler headerHostGotFocusHandler =
					(object sender, RoutedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnHeaderHostGotFocus(sender, args);
						}
					};
				headerHost.GotFocus += headerHostGotFocusHandler;
				m_headerHostGotFocusRevoker.Disposable = Disposable.Create(() => headerHost.GotFocus -= headerHostGotFocusHandler);

				TypedEventHandler<UIElement, ContextRequestedEventArgs> headerContextRequestedHandler =
					(UIElement _, ContextRequestedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnHeaderContextRequested(args);
						}
					};
				headerHost.ContextRequested += headerContextRequestedHandler;
				m_headerContextRequestedRevoker.Disposable = Disposable.Create(() => headerHost.ContextRequested -= headerContextRequestedHandler);

				// Focus on an off-screen header must not scroll PART_HeaderScroller: header/body sync is
				// one-way, so the band would end up offset from the columns it labels.
				TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> bringIntoViewHandler =
					(UIElement _, BringIntoViewRequestedEventArgs args) =>
					{
						if (!weakThis.TryGetTarget(out var strongThis))
						{
							return;
						}
						strongThis.OnHeaderBringIntoViewRequested(args);
					};
				headerHost.BringIntoViewRequested += bringIntoViewHandler;
				m_headerBringIntoViewRevoker.Disposable = Disposable.Create(() => headerHost.BringIntoViewRequested -= bringIntoViewHandler);

				if (headerHost is FrameworkElement headerHostFE)
				{
					RoutedEventHandler headerHostLoadedHandler =
						(object sender, RoutedEventArgs args) =>
						{
							if (weakThis.TryGetTarget(out var strongThis))
							{
								strongThis.OnHeaderHostLoaded(sender, args);
							}
						};
					headerHostFE.Loaded += headerHostLoadedHandler;
					m_headerHostLoadedToken.Disposable = Disposable.Create(() => headerHostFE.Loaded -= headerHostLoadedHandler);
				}

				QueueTerminalGridLineRefresh();
			}

			if (m_rowsRepeater is { } repeater)
			{
				// Assigned here rather than in the template: the selector needs an owning TableView to map
				// an item to its row kind, and XAML has no way to hand it one.
				var selector = new TableViewRowTemplateSelector();
				selector.SetOwningTableViewInternal(this);
				m_rowTemplateSelector = selector;
				repeater.ItemTemplate = selector;

				TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> bodyBringIntoViewHandler =
					(UIElement _, BringIntoViewRequestedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnBodyBringIntoViewRequested(args);
						}
					};
				repeater.BringIntoViewRequested += bodyBringIntoViewHandler;
				m_bodyBringIntoViewRevoker.Disposable = Disposable.Create(() => repeater.BringIntoViewRequested -= bodyBringIntoViewHandler);

				TypedEventHandler<ItemsRepeater, ItemsRepeaterElementPreparedEventArgs> elementPreparedHandler =
					(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnRowElementPrepared(sender, args);
						}
					};
				repeater.ElementPrepared += elementPreparedHandler;
				m_rowElementPreparedToken.Disposable = Disposable.Create(() => repeater.ElementPrepared -= elementPreparedHandler);

				TypedEventHandler<ItemsRepeater, ItemsRepeaterElementClearingEventArgs> elementClearingHandler =
					(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnRowElementClearing(sender, args);
						}
					};
				repeater.ElementClearing += elementClearingHandler;
				m_rowElementClearingToken.Disposable = Disposable.Create(() => repeater.ElementClearing -= elementClearingHandler);

				TypedEventHandler<ItemsRepeater, ItemsRepeaterElementIndexChangedEventArgs> elementIndexChangedHandler =
					(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnRowElementIndexChanged(sender, args);
						}
					};
				repeater.ElementIndexChanged += elementIndexChangedHandler;
				m_rowElementIndexChangedToken.Disposable = Disposable.Create(() => repeater.ElementIndexChanged -= elementIndexChangedHandler);

				if (repeater is FrameworkElement repeaterFE)
				{
					RoutedEventHandler repeaterLoadedHandler =
						(object sender, RoutedEventArgs args) =>
						{
							if (weakThis.TryGetTarget(out var strongThis))
							{
								strongThis.OnRowsRepeaterLoaded(sender, args);
							}
						};
					repeaterFE.Loaded += repeaterLoadedHandler;
					m_rowsRepeaterLoadedToken.Disposable = Disposable.Create(() => repeaterFE.Loaded -= repeaterLoadedHandler);
				}
			}

			// Drive the repeater from the active source only after its item template and lifecycle hooks
			// are wired. ItemsRepeater may react to ItemsSource immediately; doing this earlier leaves it
			// briefly sourced without the selector/ElementPrepared owner hookup TableView rows require.
			RefreshRowsPipeline();

			// Track the body cursor as focus moves: re-shape notifications arrive after ItemsRepeater has
			// recycled the focused row, so the pre-reshape position is only observable here.
			RoutedEventHandler bodyFocusGotFocusHandler =
				(object sender, RoutedEventArgs args) => OnTableViewGotFocusForBodyTracking(sender, args);
			GotFocus += bodyFocusGotFocusHandler;
			m_bodyFocusTrackerGotFocusRevoker.Disposable = Disposable.Create(() => GotFocus -= bodyFocusGotFocusHandler);
			TypedEventHandler<UIElement, LosingFocusEventArgs> bodyFocusLosingFocusHandler =
				(UIElement sender, LosingFocusEventArgs args) => OnTableViewLosingFocusForBodyTracking(sender, args);
			LosingFocus += bodyFocusLosingFocusHandler;
			m_bodyFocusTrackerLosingFocusRevoker.Disposable = Disposable.Create(() => LosingFocus -= bodyFocusLosingFocusHandler);

			// Body horizontal scrolling drives the header ScrollViewer; vertical stickiness is structural.

			RebuildHeaders();
			UpdateHeaderVisibility();

			// Theme switches require refreshing imperatively-resolved grid-line brushes.
			if (m_actualThemeChangedToken.Disposable is not null)
			{
				try
				{
					m_actualThemeChangedToken.Disposable = null;
				}
				catch (Exception)
				{
				}
			}

			{
				WeakReference<TableView> weakThis2 = new(this);
				TypedEventHandler<FrameworkElement, object> actualThemeChangedHandler =
					(FrameworkElement _, object _) =>
					{
						if (!weakThis2.TryGetTarget(out var strongThis))
						{
							return;
						}
						if (!strongThis.IsLoaded)
						{
							return;
						}
						try
						{
							InvalidateTableViewResourceCache(strongThis);
							// Rebuild headers and realized rows so grid-line brushes re-resolve.
							strongThis.RebuildHeaders();
							strongThis.RefreshGridLinesOnRealizedRows();
							strongThis.QueueTerminalGridLineRefresh();
							// Re-apply indent and lead-cell reservation against the new theme's metrics.
							strongThis.RefreshRealizedRowHierarchyState();
						}
						catch (Exception)
						{
							// Theme-switch refresh is best-effort.
						}
					};
				ActualThemeChanged += actualThemeChangedHandler;
				m_actualThemeChangedToken.Disposable = Disposable.Create(() => ActualThemeChanged -= actualThemeChangedHandler);
			}
			CompleteTipInitializationIfEmpty();
		}
		catch (Exception ex)
		{
			// The control, row and cell templates are expanded outside this method; an exception from app code is the
			// app's, and any other throw fails Initialize unless it has its verdict.
			FailTipInitialization(TableViewTip.Stage.Template, TableViewTip.CurrentFault(ex));
			throw;
		}
	}

	private void OnHeaderHostLoaded(object sender, RoutedEventArgs args)
	{
		// Header template content exists only after the first layout. App sort-key overrides can throw.
		try
		{
			RefreshHeaderTabStopsInternal();
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Refreshing header tab stops failed.");
		}
		if (m_headerScroller is not null)
		{
			return; // already resolved
		}
		if (m_headerHost is { } headerHost)
		{
			ScrollViewer? scroller = null;
			try
			{
				scroller = FindScrollViewerAncestor(headerHost, this);
			}
			catch (Exception)
			{
			}
			m_headerScroller = scroller;
			// Header pans can transiently desync; reverse-sync can clamp when header/body extents differ.
			UpdateHeaderVisibility();
		}
	}

	private void OnRowsRepeaterLoaded(object sender, RoutedEventArgs args)
	{
		if (m_rowsSourceDrained)
		{
			// Only re-source cached pages after Unloaded actually drained the repeater.
			m_rowsSourceDrained = false;
			RefreshRowsPipeline();
		}

		if (m_bodyScroller is not null)
		{
			return; // already resolved
		}
		if (m_rowsRepeater is { } repeater)
		{
			ScrollViewer? scroller = null;
			try
			{
				scroller = FindScrollViewerAncestor(repeater, this);
			}
			catch (Exception)
			{
			}
			m_bodyScroller = scroller;
			if (m_bodyScroller is { } bodyScroller)
			{
				WeakReference<TableView> weakThis = new(this);
				EventHandler<ScrollViewerViewChangedEventArgs> viewChangedHandler =
					(object? sender, ScrollViewerViewChangedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnBodyScrollerViewChanged(sender, args);
						}
					};
				bodyScroller.ViewChanged += viewChangedHandler;
				m_bodyScrollerViewChangedToken.Disposable = Disposable.Create(() => bodyScroller.ViewChanged -= viewChangedHandler);

				// Viewport resize (ViewChanged only covers scroll/zoom) must rerun table-level measure
				// so Star widths resolve after the subtree has refreshed its measured-width caches.
				SizeChangedEventHandler sizeChangedHandler =
					(object _, SizeChangedEventArgs sizeArgs) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							if (sizeArgs.NewSize.Width != sizeArgs.PreviousSize.Width)
							{
								strongThis.ExpireLockedColumnHolds();
							}
							strongThis.InvalidateMeasure();
							strongThis.RefreshFrozenColumns();
							strongThis.QueueTerminalGridLineRefresh();
						}
					};
				bodyScroller.SizeChanged += sizeChangedHandler;
				m_bodyScrollerSizeChangedRevoker.Disposable = Disposable.Create(() => bodyScroller.SizeChanged -= sizeChangedHandler);

				// Resolve during the next table measure now that the viewport is known (initial layout).
				InvalidateMeasure();
				RefreshFrozenColumns();
				QueueTerminalGridLineRefresh();
			}
		}
	}

	internal bool HasRowsViewport()
	{
		// The scroller is resolved when the rows repeater loads, which a repeater in a collapsed body never does.
		var scroller = m_bodyScroller;
		var repeater = m_rowsRepeater;
		if (scroller is null && repeater is not null)
		{
			scroller = FindScrollViewerAncestor(repeater, this);
		}
		return scroller is not null
			? scroller.ViewportWidth > 0 && scroller.ViewportHeight > 0
			: repeater is not null && repeater.ActualWidth > 0 && repeater.ActualHeight > 0;
	}

	private void OnBodyScrollerViewChanged(
		object? sender,
		ScrollViewerViewChangedEventArgs args)
	{
		var bodyScroller = m_bodyScroller;
		if (bodyScroller is null)
		{
			return;
		}

		if (m_drillRevealPending && !args.IsIntermediate)
		{
			m_drillRevealPending = false;
			m_drillSettledOffset = bodyScroller.HorizontalOffset;
		}

		// A keyboard move's ChangeView has applied; its pending focus may re-measure positions now.
		m_requestedBodyVerticalOffset = null;

		var bodyHOffset = bodyScroller.HorizontalOffset;

		// Re-pin leading-frozen columns only when horizontal scroll moves.
		if (ShouldRefreshFrozenColumnsForScroll(this, bodyHOffset))
		{
			RefreshFrozenColumns();
		}

		var bodyVOffset = bodyScroller.VerticalOffset;
		var horizontalMoved =
			!double.IsFinite(m_terminalGridLineHorizontalOffset) ||
			Math.Abs(m_terminalGridLineHorizontalOffset - bodyHOffset) >= 0.25;
		var verticalMoved =
			!double.IsFinite(m_terminalGridLineVerticalOffset) ||
			Math.Abs(m_terminalGridLineVerticalOffset - bodyVOffset) >= 0.25;

		if (horizontalMoved || verticalMoved)
		{
			m_terminalGridLineHorizontalOffset = bodyHOffset;
			m_terminalGridLineVerticalOffset = bodyVOffset;
			m_terminalGridLineGeometryRetryAvailable = true;
			RefreshTerminalGridLines();
		}

		var headerScroller = m_headerScroller;
		if (headerScroller is null)
		{
			return;
		}

		if (Math.Abs(headerScroller.HorizontalOffset - bodyHOffset) < 0.5)
		{
			// Skip near-equal offsets to avoid ViewChanged ping-pong.
			return;
		}

		// Instant tracking keeps header and body visually glued.
		headerScroller.ChangeView(bodyHOffset, null, null, true);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new TableViewAutomationPeer(this);

	private void OnItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Ignore same-reference ItemsSource updates to avoid a no-op row rebuild.
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		// Until Initialize has its verdict, attaching a source is part of initialization rather than a tracked swap.
		var initializing = !m_tip.initializationDone;
		var tipGeneration = BeginTipOperation(TableViewTip.Operation.ReplaceSource, !initializing);
		var completed = false;
		TableViewTip.Fault? failure = null;
		using var tipCompletion = Disposable.Create(() =>
		{
			if (initializing)
			{
				if (completed)
				{
					NoteTipInitializationSource();
				}
				else
				{
					FailTipInitialization(TableViewTip.Stage.Source, failure ?? new TableViewTip.Fault());
				}
			}
			else if (!completed)
			{
				FailTipOperation(tipGeneration, TableViewTip.Stage.Source, failure);
			}
			else
			{
				CompleteTipOperation(tipGeneration);
			}
			EndTipOperation();
		});

		try
		{
			// The edited item is about to leave the control. Forced, because the data set is already gone
			// by the time a handler could react, so the close cannot be vetoed. No-focus teardown: this is
			// a dependency-property change callback, and moving focus from one re-enters the framework.
			if (IsEditing)
			{
				TerminateEditWithoutVisualRestore(false /* insideLayoutPass */, true /* sourceReplaced */);
			}

			// The current cell belongs to the old data set. Left alone, CurrentItem keeps returning an item
			// that is not in the new source, BeginEdit fails with no diagnostic, and the discarded item
			// stays rooted by the tracker. WPF DataGrid likewise resets CurrentItem/CurrentCell here.
			//
			// Per-row begin-edit press state needs no reset: it is compared by item identity, so an entry
			// left over from the previous data set can never match an item from the new one.
			SetCurrentCell(null, null);

			// The column set changed; reset the shared cursor so first header entry does not skip an
			// unvisited column.
			ResetColumnCursorInternal();

			// New data set: clear the grow-only Auto accumulators so widths recompute from scratch. The next
			// table measure pass pulls measured widths from the by-then re-realized rows, so the outgoing rows'
			// stale content no longer pins the columns.
			ResetColumnDesiredWidths();

			// A different collection invalidates any projection we synthesized over the old one, so the
			// shaping state that lived in it goes with it. AdoptItemsSource mints a fresh projection here
			// (the active source is still available to it as the previous source to detach), and
			// RefreshRowsPipeline then pushes the new view into the repeater. This is the only path that
			// reassigns the active source - every other re-entry keeps it and only refreshes the pipeline.

			// PART_RowsRepeater is driven from the flat ItemsSource DP. The sort belonged to the discarded
			// projection, so the reported sort state and the chevron go with it - otherwise they describe
			// an order the new rows are not in.
			ResetSortStateForNewItemsSource();
			// Deferred reshapes target the discarded data set.
			m_pendingEditReshapes.Clear();
			m_reshapesAfterEditCloses.Clear();
			AdoptItemsSource();
			RefreshRowsPipeline();
		}
		catch (Exception ex)
		{
			// App code's exceptions (the new collection, its contract, a callout) carry their kind.
			failure = TableViewTip.CurrentFault(ex);
			throw;
		}
		completed = true;
	}

	private void OnHeadersVisibilityPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		if (!ShouldShowColumnHeaders())
		{
			MoveFocusOffCollapsingHeaders();
		}

		UpdateHeaderVisibility();
		if (ShouldShowColumnHeaders())
		{
			// Hidden headers are inactive; restore the tab stops stamped while they were hidden.
			RefreshHeaderTabStopsInternal();
		}
		InvalidateMeasure();
		QueueTerminalGridLineRefresh();

		// UIA: every realized cell's name and header items follow whether column headers are shown.
		var columnBit = (uint)TableViewHeadersVisibility.Column;
		var showedColumnHeaders =
			((uint)(TableViewHeadersVisibility)args.OldValue & columnBit) != 0;
		if (showedColumnHeaders != ShouldShowColumnHeaders() &&
			AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			ForEachRealizedRow(row =>
			{
				row.RaiseCellAutomationNamesChangedInternal();
			});
		}
	}

	private void OnGridLinesVisibilityPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		ApplyGridLinesToHeader();
		RefreshGridLinesOnRealizedRows();
		QueueTerminalGridLineRefresh();
	}

	private void OnRowBackgroundPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		// Re-tint realized rows so opt-in banding refreshes.
		RefreshRowBackgroundsOnRealizedRows();
	}

	private void OnAlternatingRowBackgroundPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		RefreshRowBackgroundsOnRealizedRows();
	}

	// Header lines are always drawn; only the last separator yields to the outer border.
	private void ApplyGridLinesToHeader()
	{
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		var headerCells = host.Children;
		var headerCellCount = headerCells.Count;
		var lastVisibleHeaderCell = headerCellCount;
		for (var i = headerCellCount; i > 0; --i)
		{
			if (headerCells[i - 1] is FrameworkElement headerCell)
			{
				var column = headerCell.Tag as TableViewColumn;
				if (headerCell.Visibility == Visibility.Visible &&
					column is not null &&
					column.ActualWidth > 0.0)
				{
					lastVisibleHeaderCell = i - 1;
					break;
				}
			}
		}

		for (var i = 0; i < headerCellCount; ++i)
		{
			if (headerCells[i] is TableViewColumnHeader header)
			{
				if (header.GetGridLineInternal() is { } gridLine)
				{
					gridLine.Visibility =
						!(m_suppressTrailingGridLine && i == lastVisibleHeaderCell)
							? Visibility.Visible
							: Visibility.Collapsed;
				}
			}
		}
	}

	internal void OnColumnHeaderTemplateApplied(TableViewColumnHeader header)
	{
		if (!m_rebuildingHeaders)
		{
			ApplyGridLinesToHeader();
			RefreshResizeGripperAvailability();
		}
	}

	private void ForEachRealizedRow(Action<TableViewRow> fn)
	{
		if (m_rowsRepeater is { } repeater)
		{
			var childCount = VisualTreeHelper.GetChildrenCount(repeater);
			for (var i = 0; i < childCount; ++i)
			{
				if (VisualTreeHelper.GetChild(repeater, i) is TableViewRow row)
				{
					fn(row);
				}
			}
		}
	}

	private void RefreshGridLinesOnRealizedRows()
	{
		TableViewRow? terminalRow = null;
		m_terminalGridLineRow?.TryGetTarget(out terminalRow);
		ForEachRealizedRow(row =>
		{
			var suppressBottom = terminalRow is not null && IsSameObject(row, terminalRow) && m_suppressBottomGridLine;
			row.SetTerminalGridLineSuppression(new()
			{
				suppressTrailing = m_suppressTrailingGridLine,
				suppressBottom = suppressBottom
			});
		});
	}

	private void QueueTerminalGridLineRefresh(bool isGeometryRetry = false)
	{
		if (!isGeometryRetry)
		{
			m_terminalGridLineGeometryRetryAvailable = true;
		}

		if (m_terminalGridLinesLayoutToken.Disposable is not null)
		{
			return;
		}

		if (isGeometryRetry)
		{
			if (!m_terminalGridLineGeometryRetryAvailable)
			{
				return;
			}
			m_terminalGridLineGeometryRetryAvailable = false;
		}

		WeakReference<TableView> weakThis = new(this);
		EventHandler<object> layoutUpdatedHandler =
			(object? _, object _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					if (strongThis.m_terminalGridLinesLayoutToken.Disposable is not null)
					{
						strongThis.m_terminalGridLinesLayoutToken.Disposable = null;
					}

					strongThis.RefreshTerminalGridLines();
				}
			};
		LayoutUpdated += layoutUpdatedHandler;
		m_terminalGridLinesLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= layoutUpdatedHandler);
	}

	private bool? ShouldSuppressTrailingGridLine()
	{
		var border = BorderThickness;
		// Both edges are read in the panel's logical coordinate space, which XAML mirrors wholesale
		// under RTL, so the logical trailing edge meets BorderThickness.Right in either direction.
		var outerThickness = Math.Max(0.0, border.Right);
		if (outerThickness <= 0.0 || ActualWidth <= 0.0)
		{
			return false;
		}

		FrameworkElement? candidate = null;
		ForEachRealizedRow(row =>
		{
			if (candidate is null)
			{
				candidate = row.GetLastVisibleCellInternal();
			}
		});

		if (candidate is null && ShouldShowColumnHeaders())
		{
			if (m_headerHost is { } host)
			{
				var cells = host.Children;
				for (var i = cells.Count; i > 0; --i)
				{
					if (cells[i - 1] is FrameworkElement cell &&
						cell.Visibility == Visibility.Visible &&
						cell.ActualWidth > 0.0)
					{
						candidate = cell;
						break;
					}
				}
			}
		}

		if (candidate is null)
		{
			return false;
		}

		if (!TryGetBoundsRelativeTo(candidate, this, out var bounds))
		{
			return null;
		}

		// TransformToVisual reports the panel's logical coordinate space.
		var candidateEdge = bounds.X + bounds.Width;
		var outerEdge = ActualWidth - outerThickness;
		return Math.Abs(candidateEdge - outerEdge) <= TerminalEdgeTolerance(this);
	}

	private bool? ShouldSuppressBottomGridLine(
		FrameworkElement? element,
		bool hasBottomGridLine)
	{
		if (element is null || !hasBottomGridLine)
		{
			return false;
		}

		var bottomThickness = Math.Max(0.0, BorderThickness.Bottom);
		if (bottomThickness <= 0.0 || ActualHeight <= 0.0)
		{
			return false;
		}

		if (!TryGetBoundsRelativeTo(element, this, out var bounds))
		{
			return null;
		}

		var innerBottom = ActualHeight - bottomThickness;
		return Math.Abs(bounds.Y + bounds.Height - innerBottom) <= TerminalEdgeTolerance(this);
	}

	private void RefreshTerminalGridLines()
	{
		var terminalColumnIndex = -1;
		if (Columns is { } columns)
		{
			for (var i = columns.Count; i > 0; --i)
			{
				var column = columns[i - 1];
				if (column is not null &&
					column.Visibility == Visibility.Visible &&
					column.ActualWidth > 0.0)
				{
					terminalColumnIndex = i - 1;
					break;
				}
			}
		}
		var terminalColumnChanged = terminalColumnIndex != m_terminalGridLineColumnIndex;
		m_terminalGridLineColumnIndex = terminalColumnIndex;

		var trailingResult = ShouldSuppressTrailingGridLine();
		var suppressTrailing = trailingResult ?? m_suppressTrailingGridLine;
		var trailingChanged = suppressTrailing != m_suppressTrailingGridLine;
		m_suppressTrailingGridLine = suppressTrailing;

		FrameworkElement? terminalElement = null;
		TableViewRow? terminalRow = null;
		TableViewGroupHeader? terminalGroupHeader = null;
		var terminalGeometryUnavailable = false;
		if (m_rowsRepeater is { } repeater)
		{
			// Select by geometry rather than by item index: once content overflows and scrolls, the
			// container meeting the inner bottom edge is not the last item.
			var innerBottom = ActualHeight - Math.Max(0.0, BorderThickness.Bottom);
			var closestDistance = double.PositiveInfinity;
			var childCount = VisualTreeHelper.GetChildrenCount(repeater);
			for (var i = 0; i < childCount; ++i)
			{
				if (VisualTreeHelper.GetChild(repeater, i) is not FrameworkElement child)
				{
					continue;
				}

				if (!TryGetBoundsRelativeTo(child, this, out var bounds))
				{
					terminalGeometryUnavailable = true;
					continue;
				}

				var distance = Math.Abs(bounds.Y + bounds.Height - innerBottom);
				if (distance < closestDistance)
				{
					closestDistance = distance;
					terminalElement = child;
				}
			}

			terminalRow = terminalElement as TableViewRow;
			terminalGroupHeader = terminalElement as TableViewGroupHeader;
		}

		TableViewRow? previousTerminalRow = null;
		m_terminalGridLineRow?.TryGetTarget(out previousTerminalRow);
		TableViewGroupHeader? previousTerminalGroupHeader = null;
		m_terminalGridLineGroupHeader?.TryGetTarget(out previousTerminalGroupHeader);
		var terminalChanged =
			!IsSameObject(previousTerminalRow, terminalRow) ||
			!IsSameObject(previousTerminalGroupHeader, terminalGroupHeader);
		if (terminalChanged)
		{
			if (previousTerminalRow is not null)
			{
				previousTerminalRow.SetTerminalGridLineSuppression(new()
				{
					suppressTrailing = m_suppressTrailingGridLine,
					suppressBottom = false
				});
			}
			if (previousTerminalGroupHeader is not null)
			{
				previousTerminalGroupHeader.SetTerminalBottomGridLineSuppression(false);
			}

			m_terminalGridLineRowSizeChangedRevoker.Disposable = null;
			m_terminalGridLineRow = terminalRow is not null ? new WeakReference<TableViewRow>(terminalRow) : null;
			m_terminalGridLineGroupHeader =
				terminalGroupHeader is not null ? new WeakReference<TableViewGroupHeader>(terminalGroupHeader) : null;

			if (terminalElement is not null)
			{
				WeakReference<TableView> weakThis = new(this);
				SizeChangedEventHandler sizeChangedHandler =
					(object _, SizeChangedEventArgs _) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.QueueTerminalGridLineRefresh();
						}
					};
				var sizeChangedSource = terminalElement;
				sizeChangedSource.SizeChanged += sizeChangedHandler;
				m_terminalGridLineRowSizeChangedRevoker.Disposable = Disposable.Create(() => sizeChangedSource.SizeChanged -= sizeChangedHandler);
			}
		}

		var hasBottomGridLine =
			terminalRow is not null
				? WantsHorizontalLines(GridLinesVisibility)
				: terminalGroupHeader is not null && terminalGroupHeader.BorderThickness.Bottom > 0.0;
		bool? bottomResult = terminalElement is not null
			? ShouldSuppressBottomGridLine(terminalElement, hasBottomGridLine)
			: false;
		// A container that failed its transform could be the real edge container, so a negative
		// result is only trustworthy once every container was measurable.
		if (terminalGeometryUnavailable && !(bottomResult ?? false))
		{
			bottomResult = null;
		}
		var suppressBottom = bottomResult ?? m_suppressBottomGridLine;
		m_suppressBottomGridLine = suppressBottom;

		if (trailingChanged || terminalColumnChanged)
		{
			ApplyGridLinesToHeader();
			RefreshGridLinesOnRealizedRows();
		}

		// Push unconditionally rather than only on a detected change. A container can be recycled or
		// re-prepared while this state is applied, so its own copy can disagree with the table's; a
		// change-gated push would leave that disagreement permanent. This is also the only thing that
		// re-derives the overlay from the container's current BorderThickness.
		if (m_rowsRepeater is { } rowsRepeater)
		{
			var childCount = VisualTreeHelper.GetChildrenCount(rowsRepeater);
			for (var i = 0; i < childCount; ++i)
			{
				if (VisualTreeHelper.GetChild(rowsRepeater, i) is TableViewGroupHeader header &&
					!IsSameObject(header, terminalGroupHeader))
				{
					header.SetTerminalBottomGridLineSuppression(false);
				}
			}
		}
		if (terminalRow is not null)
		{
			terminalRow.SetTerminalGridLineSuppression(new()
			{
				suppressTrailing = m_suppressTrailingGridLine,
				suppressBottom = m_suppressBottomGridLine
			});
		}
		if (terminalGroupHeader is not null)
		{
			terminalGroupHeader.SetTerminalBottomGridLineSuppression(m_suppressBottomGridLine);
		}

		if (!trailingResult.HasValue || !bottomResult.HasValue)
		{
			QueueTerminalGridLineRefresh(true);
		}
		else
		{
			m_terminalGridLineGeometryRetryAvailable = true;
		}
	}

	private void RefreshRowBackgroundsOnRealizedRows()
	{
		ForEachRealizedRow(row =>
		{
			row.RefreshRowBackground();
		});
	}

	private void QueueGroupExpansionRowRefresh()
	{
		if (m_pendingGroupRowRefreshLayoutToken.Disposable is not null)
		{
			return;
		}

		WeakReference<TableView> weakThis = new(this);
		EventHandler<object> layoutUpdatedHandler =
			(object? _, object _) =>
			{
				if (!weakThis.TryGetTarget(out var strongThis))
				{
					return;
				}

				if (strongThis.m_pendingGroupRowRefreshLayoutToken.Disposable is not null)
				{
					strongThis.m_pendingGroupRowRefreshLayoutToken.Disposable = null;
				}

				try
				{
					strongThis.RefreshRealizedRowsAfterGroupExpansion();
				}
				catch (Exception)
				{
					// Best-effort repair after a deferred grouped reshape.
				}
			};
		LayoutUpdated += layoutUpdatedHandler;
		m_pendingGroupRowRefreshLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= layoutUpdatedHandler);
	}

	private void RefreshRealizedRowsAfterGroupExpansion()
	{
		// A reshape can rebind a pooled header in place without ElementPrepared/ElementIndexChanged,
		// leaving a stale IsExpanded (read by the ExpandCollapse peer and chevron states).
		RefreshRealizedGroupHeadersAfterExpansion();

		ForEachRealizedRow(row =>
		{
			var rowImpl = row;
			rowImpl.EnsureOwningTableViewInternal(this);
			RefreshRowSelectionState(row);
		});

		InvalidateMeasure();
	}

	private void AdoptItemsSource(bool reclaimActiveSource = false)
	{
		var itemsSource = ItemsSource;
		// Reclaiming keeps the projection this table already minted, and with it the shaping state.
		var tableViewSource = reclaimActiveSource ? m_activeSource : null;
		if (tableViewSource is null)
		{
			tableViewSource = itemsSource as TableViewSource;
			m_telemetrySourceKinds = tableViewSource is not null ? TableViewTelemetry.SourceKind.AppSupplied : 0u;
		}

		// Normalize the source. When the app hands us a plain collection, project it through a
		// TableViewSource of our own so the control has exactly one row pipeline rather than a shaped
		// path and a raw one. This mirrors ItemsControl, which always routes ItemsSource through a
		// collection view, so the grid above it never has to ask what kind of source it was given.
		//
		// This runs only when ItemsSource actually changes, so there is never a prior projection to
		// reuse here - a different collection invalidated it, and the re-entries that must keep the
		// active projection (OnApplyTemplate, repeater Loaded, a shaping verb) go through
		// RefreshRowsPipeline and never reach this method.
		if (tableViewSource is null && itemsSource is not null)
		{
			// Deliberately unguarded. The shaping engine accepts exactly the collection interfaces
			// ItemsSourceView does, so a source it refuses is one XAML cannot project either, and
			// ItemsRepeater raises that as an error rather than degrading. Let it surface.
			tableViewSource = TableViewSource.From(itemsSource);
			m_telemetrySourceKinds = TableViewTelemetry.SourceKind.Projected;
		}

		StopAwaitingSource();
		if (tableViewSource is not null && !tableViewSource.SetOwningTableView(this))
		{
			TVDiag.LogRetailF("[TableView] ItemsSource is a TableViewSource already bound to another TableView; ignored.");
			// Retry when the owner lets go; OnTableViewLoaded retries too.
			WeakReference<TableView> weakThisForRelease = new(this);
			tableViewSource.AddOwnerReleasedHandler(this, () =>
			{
				if (weakThisForRelease.TryGetTarget(out var strongThis) && strongThis.IsLoaded)
				{
					strongThis.ReclaimItemsSource();
				}
			});
			m_awaitedSource = new(tableViewSource);
			tableViewSource = null;
		}

		// Publish the new source before releasing the previous one: the release synchronously runs
		// other tables' waiters, and an app handler reached from there can assign this table's
		// ItemsSource again. That nested adoption must see coherent state and must win.
		var previouslyOwned = m_activeSource;
		m_activeSource = tableViewSource;
		m_activeSourceReleased = false;

		// A remembered body position names a row of the old source.
		ForgetBodyFocusPosition();
		m_reshapeFocusValid = false;
		m_reshapeFocusRow = -1;
		CancelReshapeFocusRestore();

		if (tableViewSource is not null)
		{
			var sourceImpl = tableViewSource;

			// The source raises this rather than calling back into TableView by name, so the shaping
			// stack stays below the control. Weak, because the control owns the source through
			// m_activeSource and a strong capture would be a cycle.
			WeakReference<TableView> weakThis = new(this);
			sourceImpl.SetProjectionChangedHandler(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnTableViewSourceProjectionChanged();
				}
			});
			sourceImpl.SetShapingChangedHandler((bool reorderOnly) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnTableViewSourceShapingChanged(reorderOnly);
				}
			});
			AttachSourceTip(tableViewSource);
			// Live reshape must not end the user's edit; DrainCoalescedEditReshape resumes it.
			sourceImpl.SetLiveShapingHold(() =>
			{
				return weakThis.TryGetTarget(out var strongThis) && strongThis.IsEditing;
			});
			// CustomSortComparer ranks are frozen at header sort; re-place the edited row.
			sourceImpl.SetLiveItemChangedHook((item, reshapePending) =>
			{
				return weakThis.TryGetTarget(out var strongThis) && strongThis.RepositionCustomSortItem(item, reshapePending);
			});
			sourceImpl.SetRowsReleasedHook((item, liveRows) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.ReleaseCustomSortRows(item, liveRows);
				}
			});
			sourceImpl.SetLiveItemReshapeHandler(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					TableViewTelemetry.MarkUsed(strongThis.m_telemetrySession, strongThis.m_telemetrySession.generation,
						TableViewTelemetry.Feature.LiveShaping);
				}
			});

			if (IsLoaded)
			{
				// The source may have been sorted while another table owned it.
				m_rerankCustomSortOnReconcile = reclaimActiveSource;
				QueueReconcileSortStateWithSource();
			}
			else
			{
				// No Unloaded will come to release it; OnTableViewLoaded reclaims it.
				m_activeSourceReleased = true;
				sourceImpl.ReleaseOwningTableView(this);
			}
		}

		// Detach the previous source last. Swapping ItemsSource between two TableViewSources leaves
		// the old one alive and still subscribed to the app's collection, so without this it keeps a
		// back-pointer to this control and a later rebuild of that discarded source would drive a
		// TableView it no longer belongs to. A nested adoption may have made it active again.
		if (previouslyOwned is not null && previouslyOwned != tableViewSource && previouslyOwned != m_activeSource)
		{
			previouslyOwned.ReleaseOwningTableView(this);
		}
	}

	private void RefreshRowsPipeline()
	{
		// Recompute the cached row view from whatever the active source currently projects. A shaping
		// verb can swap the projection underneath us, so this is refreshed on every re-entry, not just
		// on a source change.
		object? rowsSource = null;

		// Held so the generation bump below can tell a genuine provider swap from a re-entry that
		// merely re-reads the same one.
		var previousRowMetadata = m_tableViewSourceRowMetadata;
		m_tableViewSourceRowMetadata = null;

		if (m_activeSource is { } activeSource)
		{
			var sourceImpl = activeSource;
			// Straight through: for a TableViewSource the row view IS the projection's view.
			m_rowsItemsSourceView = sourceImpl.GetItemsSourceView();
			m_tableViewSourceRowMetadata = sourceImpl.GetRowMetadata();
			rowsSource = m_rowsItemsSourceView is not null ? m_rowsItemsSourceView : null;
		}
		else
		{
			// Null ItemsSource: nothing to project, so the repeater empties out below.
			m_rowsItemsSourceView = null;
		}

		// Bump only when the provider that produced previously handed-out row identities has actually
		// been replaced, so a request captured against the old one can tell it is stale. Identities are
		// value-based strings, so without the bump the same string could name an unrelated group in a
		// new projection. Bumping unconditionally is equally wrong in the other direction: this method
		// also runs on re-entries that keep the very same projection (OnApplyTemplate, a Loaded repump
		// after an unload drain, an applied sort), and a bump there silently discards a queued group
		// toggle that is still perfectly valid.
		if (!ReferenceEquals(m_tableViewSourceRowMetadata, previousRowMetadata))
		{
			++m_rowMetadataGeneration;
		}

		if (m_rowsRepeater is { } repeater)
		{
			// Subscribe BEFORE the repeater adopts the view: handlers run in subscription order, so we
			// see a Reset while the focused row is still realized.
			UpdateReshapeFocusDetectorSubscription(m_rowsItemsSourceView);

			// ItemsRepeater has no identity short-circuit: re-assigning the same source tears down
			// every container and resets scroll. Guard so a theme-change or Loaded repump does not
			// blow away realized rows.
			if (!IsSameObject(repeater.ItemsSource, rowsSource))
			{
				repeater.ItemsSource = rowsSource;
			}

			UpdateItemsSourceCollectionChangedSubscription();
			UpdateEmptyState();
			UpdateRowHierarchyResetSubscription();

			// Re-point selection at the new source. SelectionModel::Source clears unconditionally, so a
			// swap always drops the selection; then drain anything requested before a source existed.
			ResolveSelectionAfterSourceChange();
		}
		// else: OnApplyTemplate hasn't run yet; the repeater will be sourced from there.
	}

	private void OnTableViewSourceProjectionChanged()
	{
		SupersedeTipOperation();

		// The repeater's focus rescue during recycling must not be mistaken for the user's position.
		SuppressBodyFocusTrackingForThisTurn();

		// Capture first: RefreshRowsPipeline tears down realized rows synchronously, and
		// TerminateEditWithoutVisualRestore can move focus before that.
		CaptureBodyFocusForReshape();

		// A shaping verb swapped the projected shape after we bound, so the cached view and row
		// metadata describe the previous projection. Re-read them and re-drive the rows.
		if (IsEditing)
		{
			// The edited item may not exist in the new projection. Forced, for the same reason as an
			// ItemsSource swap: the shape is already gone by the time a handler could veto it.
			TerminateEditWithoutVisualRestore();
		}

		RefreshRowsPipeline();
		QueueGroupExpansionRowRefresh();

		// In-place projection rewrites (e.g. declaring a hierarchy over the same view) re-prepare no
		// rows, so restamp them. Deferred until the repeater reconciles.
		QueueRefreshRealizedRowHierarchyState();
	}

	private void UpdateRowHierarchyResetSubscription()
	{
		ItemsSourceView? view = null;
		if (m_rowsRepeater is { } repeater)
		{
			view = repeater.ItemsSourceView;
		}

		if (m_rowHierarchyResetRevoker.Disposable is not null && SameInspectableIdentity(view, m_rowHierarchyResetView))
		{
			return;
		}

		m_rowHierarchyResetRevoker.Disposable = null;
		m_rowHierarchyResetView = null;

		if (view is not null)
		{
			// TODO Uno: C++ captures a raw, non-owning this; a weak capture keeps a long-lived source from rooting the control.
			WeakReference<TableView> weakThis = new(this);
			NotifyCollectionChangedEventHandler handler = (s, a) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnRowsSourceResetForHierarchy(s, a);
				}
			};
			view.CollectionChanged += handler;
			m_rowHierarchyResetRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= handler);
			m_rowHierarchyResetView = view;
		}
	}

	private void OnRowsSourceResetForHierarchy(
		object? sender,
		NotifyCollectionChangedEventArgs? args)
	{
		// Reset only: splices restamp via ElementIndexChanged or the toggling verb. A Reset may
		// republish metadata for the same rows (e.g. a collapsed node losing its last child).
		if (args is null || args.Action != NotifyCollectionChangedAction.Reset)
		{
			return;
		}

		QueueRefreshRealizedRowHierarchyState();
	}

	private void QueueRefreshRealizedRowHierarchyState()
	{
		// Deferred and coalesced: raised mid-rebuild, before the repeater reconciles indices.
		if (m_rowHierarchyRefreshQueued)
		{
			return;
		}
		WeakReference<TableView> weakThis = new(this);
		var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
		// Unlatch if the post fails (queue shutting down) so later Resets can still queue a restamp.
		m_rowHierarchyRefreshQueued = queue is not null && queue.TryEnqueue(() =>
		{
			if (weakThis.TryGetTarget(out var strongThis))
			{
				strongThis.m_rowHierarchyRefreshQueued = false;
				strongThis.RefreshRealizedRowHierarchyState();
			}
		});
	}

	private void OnTableViewSourceShapingChanged(bool reorderOnly)
	{
		SupersedeTipOperation();
		if (!reorderOnly)
		{
			QueueGroupExpansionRowRefresh();
		}

		// The new shape is in place by now (re-order and rebuild have both run).
		RestoreBodyFocusAfterReshape();

		// The app may have declared or cleared a sort straight on the source, which the control has no
		// other way to learn about. Reconcile before anything else so the chevrons never outlive the
		// axis they describe.
		if (!m_isApplyingControlInitiatedSort)
		{
			QueueReconcileSortStateWithSource();
		}

		// A programmatic shaping verb rewrites the projection with no input event behind it, so
		// nothing else tells a UIA client that the rows it cached are stale. A pure re-order keeps the
		// same children in a new order; anything else can add or remove them.
		if (!AutomationPeer.ListenerExists(AutomationEvents.StructureChanged))
		{
			return;
		}

		var peer = FrameworkElementAutomationPeer.FromElement(this);
		if (peer is null)
		{
			return;
		}

		if (peer is TableViewAutomationPeer tableViewPeer)
		{
			var impl = tableViewPeer;
			if (reorderOnly)
			{
				impl.RaiseStructureChangedForSortChange();
			}
			else
			{
				impl.RaiseStructureChangedForVirtualizationReset();
			}
		}
	}

	private void OnEmptyTemplatePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Skip same-value sets (no re-subscription / re-evaluation), matching the other DP callbacks.
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		UpdateItemsSourceCollectionChangedSubscription();
		UpdateEmptyState();
	}

	private void UpdateItemsSourceCollectionChangedSubscription()
	{
		// Count changes also move the terminal row separator, so keep this subscription even when no
		// EmptyTemplate is configured.
		m_itemsSourceCollectionChangedRevoker.Disposable = null;
		if (m_rowsRepeater is { } repeater)
		{
			if (repeater.ItemsSourceView is { } view)
			{
				// TODO Uno: C++ captures a raw, non-owning this; a weak capture keeps a long-lived source from rooting the control.
				WeakReference<TableView> weakThis = new(this);
				global::System.Collections.Specialized.NotifyCollectionChangedEventHandler handler = (s, a) =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						strongThis.OnItemsSourceCollectionChanged(s, a);
					}
				};
				view.CollectionChanged += handler;
				m_itemsSourceCollectionChangedRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= handler);
			}
		}
	}

	private void OnItemsSourceCollectionChanged(object? sender, object? args)
	{
		SupersedeTipOperation();
		UpdateEmptyState();
		QueueTerminalGridLineRefresh();
	}

	private void UpdateReshapeFocusDetectorSubscription(ItemsSourceView? view)
	{
		if (m_reshapeFocusDetectorRevoker.Disposable is not null && SameInspectableIdentity(view, m_reshapeFocusDetectorView))
		{
			// Re-subscribing would move us behind the repeater.
			return;
		}

		m_reshapeFocusDetectorRevoker.Disposable = null;
		m_reshapeFocusDetectorView = null;

		if (view is not null)
		{
			// TODO Uno: C++ captures a raw, non-owning this; a weak capture keeps a long-lived source from rooting the control.
			WeakReference<TableView> weakThis = new(this);
			NotifyCollectionChangedEventHandler handler = (s, a) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnRowsSourceResetForFocus(s, a);
				}
			};
			view.CollectionChanged += handler;
			m_reshapeFocusDetectorRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= handler);
			m_reshapeFocusDetectorView = view;
		}
	}

	private void OnRowsSourceResetForFocus(
		object? sender,
		NotifyCollectionChangedEventArgs args)
	{
		// Only a Reset recycles every realized row; on Add/Remove focus rides along with its element.
		if (args.Action != NotifyCollectionChangedAction.Reset)
		{
			return;
		}

		SuppressBodyFocusTrackingForThisTurn();

		CaptureBodyFocusForReshape();

		// Close the edit before the repeater recycles the editor's container for another record.
		if (IsEditing)
		{
			TerminateEditWithoutVisualRestore();
		}
	}

	private void UpdateEmptyState()
	{
		var presenter = m_emptyStatePresenter;
		if (presenter is null)
		{
			// Template hasn't applied, or this template carries no empty-state part.
			return;
		}

		var emptyTemplate = EmptyTemplate;
		var repeater = m_rowsRepeater;

		if (emptyTemplate is null)
		{
			// Default opt-out keeps rows visible and never shows the empty surface.
			presenter.Visibility = Visibility.Collapsed;
			presenter.ContentTemplate = null;
			if (repeater is not null)
			{
				repeater.Visibility = Visibility.Visible;
			}
			return;
		}

		var isEmpty = true;
		if (repeater is not null)
		{
			if (repeater.ItemsSourceView is { } view)
			{
				isEmpty = view.Count == 0;
			}
		}

		if (isEmpty)
		{
			if (presenter.ContentTemplate != emptyTemplate)
			{
				presenter.ContentTemplate = emptyTemplate;
			}
			// Ensure the ContentControl inflates the template without a data item.
			if (presenter.Content is null)
			{
				presenter.Content = "";
			}
			presenter.Visibility = Visibility.Visible;
			if (repeater is not null)
			{
				repeater.Visibility = Visibility.Collapsed;
			}
		}
		else
		{
			presenter.Visibility = Visibility.Collapsed;
			if (repeater is not null)
			{
				repeater.Visibility = Visibility.Visible;
			}
		}
	}

	private static string DensitySuffix(TableViewDensity density)
	{
		switch (density)
		{
			case TableViewDensity.Compact: return "Compact";
			case TableViewDensity.Comfortable: return "Comfortable";
			default: return "";
		}
	}

	internal bool IsHighContrast()
	{
		// ActualTheme cannot report HC. Prefer the cached ThemeSettings value (kept fresh by Changed);
		// before Loaded (no WindowId yet) fall back to a one-shot AccessibilitySettings read.
		if (m_themeSettings is not null)
		{
			return m_isHighContrast;
		}
		try
		{
			return new global::Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
		}
		catch (Exception)
		{
			return false;
		}
	}

	internal double GetDensityRowMinHeight()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.density.hasRowMinHeight)
		{
			return cache.density.rowMinHeight;
		}

		var key = "TableViewRowMinHeight";
		key += DensitySuffix(Density);
		var fallback = DensityRowMinHeightFallback(Density);
		if (LookupElementResource(this, key) is { } raw)
		{
			cache.density.rowMinHeight = raw is double rawValue ? rawValue : fallback;
			cache.density.hasRowMinHeight = true;
			return cache.density.rowMinHeight;
		}
		// Resource-miss fallback mirrors Fluent density defaults so Standard stays taller than Compact.
		cache.density.rowMinHeight = fallback;
		cache.density.hasRowMinHeight = true;
		return cache.density.rowMinHeight;
	}

	internal Thickness GetDensityCellPadding()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.density.hasCellPadding)
		{
			return cache.density.cellPadding;
		}

		var key = "TableViewCellPadding";
		key += DensitySuffix(Density);
		var fallback = DensityCellPaddingFallback(Density);
		if (LookupElementResource(this, key) is { } raw)
		{
			cache.density.cellPadding = raw is Thickness rawValue ? rawValue : fallback;
			cache.density.hasCellPadding = true;
			return cache.density.cellPadding;
		}
		// Resource-miss fallback mirrors density padding presets; Standard keeps the legacy padding.
		cache.density.cellPadding = fallback;
		cache.density.hasCellPadding = true;
		return cache.density.cellPadding;
	}

	internal Thickness GetDensityHeaderCellPadding()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.density.hasHeaderCellPadding)
		{
			return cache.density.headerCellPadding;
		}

		var key = "TableViewHeaderCellPadding";
		key += DensitySuffix(Density);
		var fallback = DensityCellPaddingFallback(Density);
		if (LookupElementResource(this, key) is { } raw)
		{
			cache.density.headerCellPadding = raw is Thickness rawValue ? rawValue : fallback;
			cache.density.hasHeaderCellPadding = true;
			return cache.density.headerCellPadding;
		}
		// Resource-miss fallback mirrors cell-padding density presets.
		cache.density.headerCellPadding = fallback;
		cache.density.hasHeaderCellPadding = true;
		return cache.density.headerCellPadding;
	}

	internal double GetCellFontSize()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.font.hasCellFontSize)
		{
			return cache.font.cellFontSize;
		}

		const double fallback = 14.0;
		if (LookupElementResource(this, "TableViewCellFontSize") is { } raw)
		{
			cache.font.cellFontSize = raw is double rawValue ? rawValue : fallback;
			cache.font.hasCellFontSize = true;
			return cache.font.cellFontSize;
		}
		cache.font.cellFontSize = fallback;
		cache.font.hasCellFontSize = true;
		return cache.font.cellFontSize;
	}

	// The system focus visual takes its corners from the focused element's CornerRadius, but a rounded
	// root also clips its children and hit-testing, so round only while the focus rect shows. Programmatic
	// focus is coerced to Keyboard or Pointer by the last input device, so Keyboard is exactly that case.
	internal void AttachFocusCornerRadius(FrameworkElement target)
	{
		var radiusProperty = target is Control
			? Control.CornerRadiusProperty
			: Grid.CornerRadiusProperty;
		WeakReference<TableView> weakThis = new(this);
		target.RegisterPropertyChangedCallback(UIElement.FocusStateProperty,
			(DependencyObject sender, DependencyProperty _) =>
			{
				var element = (UIElement)sender;
				if (weakThis.TryGetTarget(out var self) && element.FocusState == FocusState.Keyboard)
				{
					// The TableView style sets CornerRadius from ControlCornerRadius, so the value stays in XAML.
					sender.SetValue(radiusProperty, self.CornerRadius);
				}
				else
				{
					sender.ClearValue(radiusProperty);
				}
			});
	}

	internal double GetRowIndentSize()
	{
		// Cached: hit per realized row on every pass, and resource lookup walks the whole tree. Cleared
		// by density/theme/HC invalidation; other runtime swaps need rows re-prepared.
		var cache = GetTableViewResourceCache(this);
		if (cache.hierarchy.hasRowIndentSize)
		{
			return cache.hierarchy.rowIndentSize;
		}

		var resolved = c_defaultRowIndentSize;
		if (LookupElementResource(this, "TableViewRowIndentSize") is { } raw)
		{
			var value = raw is double rawValue ? rawValue : c_defaultRowIndentSize;
			// Negative or non-finite indent would pull content under the chevron; fall back.
			if (double.IsFinite(value) && value >= 0.0)
			{
				resolved = value;
			}
		}

		cache.hierarchy.rowIndentSize = resolved;
		cache.hierarchy.hasRowIndentSize = true;
		return resolved;
	}

	internal double GetRowExpanderSize()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.hierarchy.hasRowExpanderSize)
		{
			return cache.hierarchy.rowExpanderSize;
		}

		var resolved = c_defaultRowExpanderSize;
		if (LookupElementResource(this, "TableViewRowExpanderSize") is { } raw)
		{
			var value = raw is double rawValue ? rawValue : c_defaultRowExpanderSize;
			if (double.IsFinite(value) && value >= 0.0)
			{
				resolved = value;
			}
		}

		cache.hierarchy.rowExpanderSize = resolved;
		cache.hierarchy.hasRowExpanderSize = true;
		return resolved;
	}

	// Grouped root-row indent: the group header's expander gutter plus TableViewGroupExpanderSpacing.
	// The header's Padding is not included, so a root chevron sits Padding.Left (8px by default) left of
	// the group key text.
	internal double GetGroupHeaderContentOffset()
	{
		var cache = GetTableViewResourceCache(this);
		if (!cache.hierarchy.hasGroupHeaderContentOffset)
		{
			double lookup(string key, double fallback)
			{
				if (LookupElementResource(this, key) is { } raw)
				{
					var value = raw is double rawValue ? rawValue : fallback;
					if (double.IsFinite(value) && value >= 0.0)
					{
						return value;
					}
				}
				return fallback;
			}

			cache.hierarchy.groupHeaderContentOffset =
				lookup("TableViewGroupExpanderSize", c_defaultGroupExpanderSize) +
				lookup("TableViewGroupExpanderSpacing", c_defaultGroupExpanderSpacing);
			cache.hierarchy.hasGroupHeaderContentOffset = true;
		}

		return cache.hierarchy.groupHeaderContentOffset;
	}

	private void OnDensityPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		InvalidateTableViewResourceCache(this);

		// Density changes require rebuilding headers and refreshing realized rows.
		RebuildHeaders();
		ForEachRealizedRow(row =>
		{
			row.RefreshDensity();
		});

		// Density changes only vertical padding and row height (horizontal cell padding is identical
		// across all presets), so it does not alter Auto content *width*. Re-measure for the new row
		// metrics and re-pin frozen columns (clips depend on row height); the grow-only Auto accumulator
		// is deliberately left intact since density is not a data-set change.
		InvalidateMeasure();
		RefreshFrozenColumns();
	}

	// A column becoming read-only must close an edit open ON THAT COLUMN, for the same reason the
	// control-level IsReadOnly does: otherwise the user keeps an editor, and a committable value, on a
	// cell that now reports it cannot be edited. Uses the no-focus teardown because this arrives from a
	// dependency-property change callback, where moving focus re-enters focus/layout processing.
	internal void OnColumnIsReadOnlyChanged(TableViewColumn? column)
	{
		RefreshCellFocusLevelsInternal();
		if (column is null || !IsEditing)
		{
			return;
		}

		if (m_currentEditColumn == column)
		{
			TerminateEditWithoutVisualRestore();
		}
	}

	// Read-only state decides which cells are actionable, so the cell tab stops are restamped. A focused
	// cell that stopped being actionable hands focus to its row.
	internal void RefreshCellFocusLevelsInternal()
	{
		ForEachRealizedRow(row =>
		{
			var rowImpl = row;
			rowImpl.SetCellLevelInternal(rowImpl.IsCellLevelInternal());
		});

		if (DispatcherQueue is { } queue)
		{
			WeakReference<TableView> weakThis = new(this);
			queue.TryEnqueue(() =>
			{
				weakThis.TryGetTarget(out var strongThis);
				try
				{
					if (strongThis is not null && strongThis.TryGetFocusedCell(out var rowIndex, out var column, true /* requireExactCell */))
					{
						var row = strongThis.GetRealizedRowAt(rowIndex);
						var rowImpl = row;
						if (row is not null && rowImpl is not null && !rowImpl.IsCellActionableInternal(rowImpl.GetVisibleCellInternal(column)))
						{
							strongThis.FocusRowContainerInternal(row);
						}
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Moving focus off an inactive cell failed.");
				}
			});
		}
	}

	private void OnIsReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		RefreshCellFocusLevelsInternal();
		// Turning the control read-only must close an open cell, or the user keeps an editor - and a
		// committable value - on a control that now reports it cannot be edited. Forced, because
		// read-only is a control-level statement a handler must not veto. Uses the no-focus teardown:
		// this runs from a DP change callback, where moving focus re-enters focus/layout processing.
		if (IsReadOnly)
		{
			if (IsEditing)
			{
				TerminateEditWithoutVisualRestore();
			}
		}
	}

	private bool ShouldShowColumnHeaders() =>
		((uint)HeadersVisibility & (uint)TableViewHeadersVisibility.Column) != 0;

	private void UpdateHeaderVisibility()
	{
		var showColumnHeaders = ShouldShowColumnHeaders();
		var columnVisibility = showColumnHeaders ?
			Visibility.Visible : Visibility.Collapsed;

		if (m_headerHost is { } headerHost)
		{
			headerHost.Visibility = columnVisibility;
		}
		if (m_headerScroller is { } headerScroller)
		{
			headerScroller.Visibility = columnVisibility;
		}
		if (m_headerRow is { } headerRow)
		{
			headerRow.Visibility = columnVisibility;
		}
	}

	private int GetItemsSourceCount()
	{
		if (m_rowsRepeater is { } repeater)
		{
			if (repeater.ItemsSourceView is { } sourceView)
			{
				return sourceView.Count;
			}
		}
		return 0;
	}

	private void OnRowElementPrepared(
		ItemsRepeater sender,
		ItemsRepeaterElementPreparedEventArgs args)
	{
		TableViewTelemetry.NoteContent(m_telemetrySession, TableViewTelemetry.ContentShown.Rows);
		if (args.Element is TableViewRow row)
		{
			var rowImpl = row;
			// Stage the suppression state first so the cell rebuild's chrome refresh paints it; refresh
			// grid lines and banding here only if the rebuild did not (each grid-line pass walks every cell).
			rowImpl.SetTerminalGridLineSuppression(new()
			{
				suppressTrailing = m_suppressTrailingGridLine,
				suppressBottom = false
			}, false /* refresh */);
			if (!rowImpl.EnsureOwningTableViewInternal(this, true /* skipIfCellsCurrent */))
			{
				rowImpl.RefreshGridLines();
				rowImpl.RefreshRowBackground();
			}
			rowImpl.RefreshCellToolTips();
			RefreshRowHierarchyState(row, args.Index);
			RefreshRowSelectionState(row);
			InvalidateMeasureForRowRealizationChange();
			QueueTerminalGridLineRefresh();
		}
		else if (args.Element is TableViewGroupHeader header)
		{
			// A pooled container must not carry a press that began on its previous group.
			header.ResetPointerStateInternal();
			PrepareGroupHeaderElement(header, args.Index);
			InvalidateMeasure();
			QueueTerminalGridLineRefresh();
		}
		if (m_tip.initializationPending && m_tipInitializationLayoutToken.Disposable is null)
		{
			ArmTipInitializationLayout();
		}
	}

	private void OnRowElementClearing(
		ItemsRepeater sender,
		ItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is TableViewRow row)
		{
			// A recycled row is about to be re-bound to a different item; an edit left open would keep
			// an editor over the new item, and the next commit would write into the wrong data object.
			// ElementClearing runs inside the repeater's measure pass, so this must not restore the
			// display visual.
			if (row == m_currentEditRow)
			{
				if (!TerminateEditWithoutVisualRestore(true /* insideLayoutPass */))
				{
					// The teardown was refused - the only way that happens here is an edit still in the
					// Beginning window, where app code (a BeginningEdit handler or the editing binding)
					// mutated the collection and recycled the row underneath us. The row
					// must not go back to the pool holding a live editor, so abandon it directly and
					// make BeginEdit unwind instead of promoting to Editing.
					row.AbandonCellEdit();
					m_abandonPendingBeginEdit = true;
					++m_editGeneration;
					m_currentEditRow = null;
					m_currentEditItem = null;
					m_currentEditColumn = null;
					m_editUneditedValue = null;

					TVDiag.LogRetailF(
						"[TableView] A row was recycled while an edit was still opening; the edit was abandoned.");
				}
			}

			var rowImpl = row;
			// No longer the drilled row; the cursor level and column survive for re-entry.
			if (m_cellLevelRow?.TryGetTarget(out var drilled) == true && IsSameObject(row, drilled))
			{
				m_cellLevelRow = null;
			}
			// Release app-supplied tooltip content rather than pinning it in the recycle pool.
			rowImpl.ReleaseCellToolTips();
			rowImpl.SetTerminalGridLineSuppression(new(), false /* refresh */);
			if (m_terminalGridLineRow?.TryGetTarget(out var terminalRow) == true && IsSameObject(row, terminalRow))
			{
				m_terminalGridLineRowSizeChangedRevoker.Disposable = null;
				m_terminalGridLineRow = null;
				m_suppressBottomGridLine = false;
			}
			rowImpl.SetOwningTableViewInternal(null);
			// Grouped reshapes can rebind a pooled row through DataContext without a fresh
			// ElementPrepared/ElementIndexChanged callback. Keep the weak owner available so that path
			// can rebuild cells and publish a live row peer; item-identity tracking still rejects stale
			// peers after the rebind.
			rowImpl.SetOwningTableViewInternal(this, false /* rebuildCells */);
			rowImpl.RefreshGridLines();
			InvalidateMeasureForRowRealizationChange();
			QueueTerminalGridLineRefresh();
		}
		else if (args.Element is TableViewGroupHeader header)
		{
			header.SetTerminalBottomGridLineSuppression(false);
			if (m_terminalGridLineGroupHeader?.TryGetTarget(out var terminalHeader) == true &&
				IsSameObject(header, terminalHeader))
			{
				m_terminalGridLineRowSizeChangedRevoker.Disposable = null;
				m_terminalGridLineGroupHeader = null;
				m_suppressBottomGridLine = false;
			}
			ClearGroupHeaderElement(header);
			InvalidateMeasure();
			QueueTerminalGridLineRefresh();
		}
	}

	private void OnRowElementIndexChanged(
		ItemsRepeater sender,
		ItemsRepeaterElementIndexChangedEventArgs args)
	{
		var row = args.Element as TableViewRow;
		if (row is null)
		{
			// A realized header keeps its element but moves to a new index, and its expansion state is
			// read from that index's metadata, so it has to be re-prepared.
			if (args.Element is TableViewGroupHeader header)
			{
				PrepareGroupHeaderElement(header, args.NewIndex);
				QueueTerminalGridLineRefresh();
			}
			return;
		}

		var rowImpl = row;
		// Realized rows can be preserved through grouped projection reshapes without a fresh
		// ElementPrepared callback, so keep the owner/cells invariant true on index changes too.
		rowImpl.SetOwningTableViewInternal(this);
		rowImpl.SetTerminalGridLineSuppression(new()
		{
			suppressTrailing = m_suppressTrailingGridLine,
			suppressBottom = false
		});
		QueueTerminalGridLineRefresh();

		// Realized rows keep their element but get a new index, so banding parity must refresh.
		if (RowBackground != null || AlternatingRowBackground != null)
		{
			rowImpl.RefreshRowBackground();
		}

		// ...and so must hierarchy state: a moved row may be at a new depth.
		RefreshRowHierarchyState(row, args.NewIndex);

		// ...and so must selected chrome. The element keeps its item here (only its index moved), so
		// this normally re-derives the same answer - it is the cheap guarantee that a row whose index
		// shifted under an insert cannot end up disagreeing with the model.
		RefreshRowSelectionState(row);
	}

	// Pushes this index's hierarchy metadata onto the row (Level 0 for flat/grouped sources).
	internal void RefreshRowHierarchyState(TableViewRow? row, int index)
	{
		if (row is null)
		{
			return;
		}

		TableViewRowInfo rowInfo = new();
		// Use only data-row metadata: a recycled row can be re-prepared at an index that just became
		// a group header (same guard as PrepareGroupHeaderElement).
		var hasRowInfo =
			TryGetTableViewSourceRowInfo(index, ref rowInfo) && rowInfo.Kind == TableViewRowKind.Data;

		var rowImpl = row;
		if (!hasRowInfo)
		{
			rowImpl.SetHierarchyStateInternal(0, false, false);
			return;
		}

		// Ask the source whether it is a tree: a leaf root (Level 1, not expandable) looks like a
		// grouped data row but must still reserve chevron width.
		var isHierarchicalRow =
			m_tableViewSourceRowMetadata is not null && m_tableViewSourceRowMetadata.IsHierarchicalSource();
		rowImpl.SetHierarchyStateInternal(
			isHierarchicalRow ? Math.Max(1, rowInfo.Level) : 0,
			rowInfo.IsExpandable,
			rowInfo.IsExpanded);
	}

	// Includes BasedOn styles.
	private static bool StyleSetsProperty(Style? style, DependencyProperty property)
	{
		for (var current = style; current is not null; current = current.BasedOn)
		{
			foreach (var setterBase in current.Setters)
			{
				if (setterBase is Setter setter && setter.Property == property)
				{
					return true;
				}
			}
		}
		return false;
	}

	// The setter a sealed Style applies: the last one in the most-derived Style that has one, even null.
	private static object? StyleSetterValue(Style? style, DependencyProperty property)
	{
		const int E_FAIL = unchecked((int)0x80004005);
		for (var current = style; current is not null; current = current.BasedOn)
		{
			var found = false;
			object? value = null;
			foreach (var setterBase in current.Setters)
			{
				if (setterBase is Setter setter && setter.Property == property)
				{
					found = true;
					value = null;
					try
					{
						value = setter.Value;
					}
					catch (Exception e) when (e.HResult == E_FAIL)
					{
						// Reading an {x:Null} Setter.Value of a typed property fails with E_FAIL, yet the Style applies null.
					}
				}
			}
			if (found)
			{
				return value;
			}
		}
		return null;
	}

	// Asked once, with the header's real content in place; the caller applies the result locally so the
	// framework does not ask the selector again with a possibly different answer.
	private static DataTemplate? SelectStyleHeaderTemplate(
		DataTemplateSelector styleSelector,
		ContentControl header,
		object? headerContent)
	{
		try
		{
			header.Content = headerContent;
			return styleSelector.SelectTemplate(headerContent, header);
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] A header Style's ContentTemplateSelector threw; using the default header text.");
			return null;
		}
	}

	// GetColumnHeaderText renders an untemplated IStringable for automation purposes, but only a genuine string
	// may be swapped for a TextBlock -- a UIElement or a type with an implicit DataTemplate must keep
	// the ContentPresenter's content model.
	private static bool IsPlainStringHeader(TableViewColumn column)
	{
		// TODO Uno: IPropertyValue projection - a boxed PropertyType::String projects as System.String.
		// Original C++: propValue && propValue.Type() == PropertyType::String
		return column.Header is string;
	}

	// Applying a Style whose TargetType the header is not, at any level of its BasedOn chain, would throw
	// out of RebuildHeaders.
	private Style? CompatibleHeaderStyle(Style? style)
	{
		if (style is null)
		{
			return null;
		}

		// Depth in TableViewColumnHeader's base chain, or -1 outside it. A BasedOn Style may only target
		// the same type as the Style it bases, or a base type. A Style without a TargetType hangs when
		// applied, so it is outside the chain too.
		static int targetDepth(Style styleLevel)
		{
			var targetType = styleLevel.TargetType;
			if (targetType == typeof(TableViewColumnHeader))
			{
				return 5;
			}
			if (targetType == typeof(ContentControl))
			{
				return 4;
			}
			if (targetType == typeof(Control))
			{
				return 3;
			}
			if (targetType == typeof(FrameworkElement))
			{
				return 2;
			}
			if (targetType == typeof(UIElement))
			{
				return 1;
			}
			if (targetType == typeof(DependencyObject))
			{
				return 0;
			}
			return -1;
		}

		var compatible = true;
		var derivedDepth = int.MaxValue;
		Style? level = style;
		// Bounded: a BasedOn cycle would otherwise never end.
		for (var guard = 0; level is not null && compatible && guard < 64; ++guard, level = level.BasedOn)
		{
			var depth = targetDepth(level);
			compatible = depth >= 0 && depth <= derivedDepth;
			derivedDepth = depth;
		}
		// A chain still going when the guard runs out loops, and applying it would throw.
		if (level is not null)
		{
			compatible = false;
		}
		if (compatible)
		{
			return style;
		}

		// Once per Style: a rebuild runs on every Density, sort or column change.
		var known = m_loggedIncompatibleHeaderStyles.Exists(
			weakStyle => weakStyle.TryGetTarget(out var knownStyle) && knownStyle == style);
		if (!known)
		{
			m_loggedIncompatibleHeaderStyles.RemoveAll(weakStyle => !weakStyle.TryGetTarget(out _));
			m_loggedIncompatibleHeaderStyles.Add(new(style));
			TVDiag.LogRetailF("[TableView] Ignored a column header Style whose TargetType or BasedOn chain is not TableViewColumnHeader or a base type.");
		}
		return null;
	}

	// A header cell keeps its column's UIElement Header and tooltip content parented until released, and
	// no other header can host them meanwhile. Every path that drops a header cell must release it first.
	private void ReleaseHeaderCells(Panel? host, HeaderRelease scope = HeaderRelease.All, TableViewColumn? column = null)
	{
		if (host is null)
		{
			return;
		}

		// Snapshot: Closed and the Content change run app code that can re-enter RebuildHeaders and
		// clear this collection.
		var children = host.Children;
		List<FrameworkElement> cells = new(children.Count);
		for (var i = 0; i < children.Count; ++i)
		{
			if (children[i] is FrameworkElement headerCell)
			{
				if (scope != HeaderRelease.All)
				{
					var cellColumn = headerCell.Tag as TableViewColumn;
					var released = scope == HeaderRelease.Column
						? cellColumn is not null && cellColumn == column
						: cellColumn is not null && cellColumn.GetOwningTableView() != this;
					if (!released)
					{
						continue;
					}
				}
				cells.Add(headerCell);
			}
		}
		if (cells.Count == 0)
		{
			return;
		}

		// Only a rebuild captures and restores header focus itself. Elsewhere the cell stays until the
		// queued rebuild, so park focus on it rather than lose it with the content. Focus leaves a removed
		// column's cell for the nearest remaining header, and stays parked on it only when none can take it.
		if (scope != HeaderRelease.All)
		{
			if (GetFocusedHeaderColumn(host, out var focusState) is { } focusedColumn)
			{
				var focusedCell = cells.Find(cell => cell.Tag as TableViewColumn == focusedColumn);
				if (focusedCell is not null)
				{
					if (scope == HeaderRelease.Column)
					{
						focusedCell.Focus(focusState);
					}
					else
					{
						MoveHeaderFocusOffRemovedColumn(host, focusedCell, focusedColumn, focusState);
					}
				}
			}
		}

		foreach (var headerCell in cells)
		{
			TableViewDetails.ClearOwnedToolTip(headerCell);
		}
		foreach (var headerCell in cells)
		{
			if (headerCell is ContentControl header)
			{
				header.ClearValue(ContentControl.ContentProperty);
			}
		}
	}

	private void MoveHeaderFocusOffRemovedColumn(
		Panel host,
		FrameworkElement removedCell,
		TableViewColumn removedColumn,
		FocusState focusState)
	{
		// Snapshot: Focus raises app focus handlers that can re-enter RebuildHeaders and clear Children.
		List<UIElement> cells;
		{
			var children = host.Children;
			var size = children.Count;
			cells = new(size);
			for (var i = 0; i < size; ++i)
			{
				cells.Add(children[i]);
			}
		}
		var removedIndex = cells.IndexOf(removedCell);
		if (removedIndex < 0)
		{
			return;
		}

		bool isHosted(UIElement cell)
		{
			return VisualTreeHelper.GetParent(cell) == host;
		}
		TableViewColumn? tryFocus(int index)
		{
			var cell = cells[index] as Control;
			TableViewColumn? column = null;
			if (cell is not null)
			{
				column = cell.Tag as TableViewColumn;
			}
			if (column is not null
				&& cell is not null
				&& column.GetOwningTableView() == this
				&& cell.Visibility == Visibility.Visible
				&& isHosted(cell)
				&& cell.Focus(focusState))
			{
				return column;
			}
			return null;
		}

		TableViewColumn? fallbackColumn = null;
		var count = cells.Count;
		for (var distance = 1; fallbackColumn is null && distance < count; ++distance)
		{
			if (removedIndex + distance < count)
			{
				fallbackColumn = tryFocus(removedIndex + distance);
			}
			if (fallbackColumn is null && removedIndex >= distance)
			{
				fallbackColumn = tryFocus(removedIndex - distance);
			}
		}

		// With no header to take it, focus parks on the removed cell until the queued rebuild, which
		// returns it to the column if it came back and otherwise moves it to the first header or the body.
		m_headerFocusRemovedColumn = new(removedColumn);
		m_headerFocusFallbackColumn = null;
		if (fallbackColumn is not null)
		{
			m_headerFocusFallbackColumn = new(fallbackColumn);
		}
		m_headerFocusRemovedState = focusState;
		if (fallbackColumn is null)
		{
			if (removedCell is Control parkCell && isHosted(parkCell))
			{
				parkCell.Focus(FocusState.Programmatic);
			}
		}
	}

	internal void RebuildHeaders()
	{
		// App code run while the old headers are released (focus, tooltip and content changes) can ask
		// for a rebuild. A nested one would build cells the outer one then drops without releasing.
		if (m_inRebuildHeaders)
		{
			m_rebuildHeadersRequested = true;
			return;
		}

		m_rebuildHeadersRequested = false;
		{
			m_inRebuildHeaders = true;
			using var inRebuildGuard = Disposable.Create(() => m_inRebuildHeaders = false);
			RebuildHeadersCore();
		}
		if (m_rebuildHeadersRequested)
		{
			m_rebuildHeadersRequested = false;
			QueueRebuildHeaders();
		}
	}

	private void RebuildHeadersCore()
	{
		m_headerSortSpaceArmedColumn = null;
		TableViewColumn? removedFocusColumn = null;
		m_headerFocusRemovedColumn?.TryGetTarget(out removedFocusColumn);
		TableViewColumn? fallbackFocusColumn = null;
		m_headerFocusFallbackColumn?.TryGetTarget(out fallbackFocusColumn);
		var removedFocusState = m_headerFocusRemovedState;
		m_headerFocusRemovedColumn = null;
		m_headerFocusFallbackColumn = null;
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		// Clearing the host drops focus with the cell; remember which header held it.
		var focusedHeaderColumn = GetFocusedHeaderColumn(host, out var focusedHeaderState);
		// A removed column that came back (a move) gets back the focus that left its header. Focus
		// still parked on a removed column that did not come back moves to the first header or the body.
		var focusBody = false;
		if (removedFocusColumn is not null && focusedHeaderColumn is not null
			&& (focusedHeaderColumn == fallbackFocusColumn || focusedHeaderColumn == removedFocusColumn))
		{
			if (removedFocusColumn.GetOwningTableView() == this)
			{
				focusedHeaderColumn = removedFocusColumn;
				focusedHeaderState = removedFocusState;
			}
			else if (focusedHeaderColumn == removedFocusColumn)
			{
				focusedHeaderColumn = null;
				focusBody = true;
			}
		}
		// Replacing the Columns vector skips MoveHeaderFocusOffRemovedColumn.
		if (focusedHeaderColumn is not null && focusedHeaderColumn.GetOwningTableView() != this)
		{
			removedFocusState = focusedHeaderState;
			focusedHeaderColumn = null;
			focusBody = true;
		}

		// Any focus move during the rebuild other than XAML's rescue off the removed cell belongs to
		// the app or user, and the restore below must not pull focus back from it.
		var headerFocusMovedAway = false;

		// Focus inside a header's content would leave with it, and an app moving focus as that content
		// is released could not be told from XAML's rescue. Park it on the header first; an app that
		// sends that move out of the header band has moved focus itself.
		if (focusedHeaderColumn is not null)
		{
			if (FocusManager.GetFocusedElement(XamlRoot!) is DependencyObject focused)
			{
				var headerCell = focused;
				for (var parent = VisualTreeHelper.GetParent(headerCell); parent is not null && parent != host;
					parent = VisualTreeHelper.GetParent(parent))
				{
					headerCell = parent;
				}
				if (headerCell != focused && VisualTreeHelper.GetParent(headerCell) == host)
				{
					if (headerCell is Control control)
					{
						control.Focus(FocusState.Programmatic);
						headerFocusMovedAway = GetFocusedHeaderColumn(host, out _) is null;
					}
				}
			}
		}

		// XAML rescues focus only while the focused element is being removed.
		var removingFocused = false;
		DependencyObject? focusedAtTeardown = null;
		using SerialDisposable headerFocusMoveRevoker = new();
		if (focusedHeaderColumn is not null || focusBody)
		{
			var root = XamlRoot;
			focusedAtTeardown = FocusManager.GetFocusedElement(root!) as DependencyObject;
			WeakReference<Panel> weakHost = new(host);
			var rescueSeen = false;
			EventHandler<GettingFocusEventArgs> headerFocusMoveHandler =
				(object? _, GettingFocusEventArgs args) =>
				{
					var newFocus = args.NewFocusedElement as UIElement;
					if (headerFocusMovedAway || newFocus is null || newFocus.XamlRoot != root)
					{
						return;
					}
					weakHost.TryGetTarget(out var liveHost);
					for (DependencyObject? current = newFocus; current is not null; current = VisualTreeHelper.GetParent(current))
					{
						if (liveHost is not null && current == liveHost)
						{
							return;
						}
					}
					if (removingFocused && !rescueSeen && focusedAtTeardown is not null && args.OldFocusedElement == focusedAtTeardown)
					{
						rescueSeen = true;
						return;
					}
					headerFocusMovedAway = true;
				};
			FocusManager.GettingFocus += headerFocusMoveHandler;
			headerFocusMoveRevoker.Disposable = Disposable.Create(() => FocusManager.GettingFocus -= headerFocusMoveHandler);
		}

		// Clearing the host under a live popup tears its child down reentrantly. Releasing the cells
		// removes only focus inside a header's content; clearing the host removes the headers too.
		removingFocused = focusedAtTeardown is not null && VisualTreeHelper.GetParent(focusedAtTeardown) != host;
		ReleaseHeaderCells(host);
		removingFocused = true;
		host.Children.Clear();
		removingFocused = false;

		m_rebuildingHeaders = true;
		using var rebuildingGuard = Disposable.Create(() => m_rebuildingHeaders = false);

		var cachedHeaderCellPadding = GetDensityHeaderCellPadding();
		var cachedRowMinHeight = GetDensityRowMinHeight();
		// unbox_value_or, not unbox_value: the key is app-overridable and a non-double would throw out
		// of RebuildHeaders; a non-positive or non-finite override would flow straight into Width().
		var cachedResizeGripperWidth =
			LookupElementResource(this, s_ResizeGripperWidthKey) is double resizeGripperWidth ? resizeGripperWidth : c_resizeGripperWidthFallback;
		if (!double.IsFinite(cachedResizeGripperWidth) || cachedResizeGripperWidth <= 0.0)
		{
			cachedResizeGripperWidth = c_resizeGripperWidthFallback;
		}
		var cachedSortIndicatorWidth =
			LookupElementResource(this, s_SortIndicatorSizeKey) is double sortIndicatorWidth ? sortIndicatorWidth : c_sortIndicatorSizeFallback;
		if (!double.IsFinite(cachedSortIndicatorWidth) || cachedSortIndicatorWidth <= 0.0)
		{
			cachedSortIndicatorWidth = c_sortIndicatorSizeFallback;
		}
		var canUserSortColumns = CanUserSortColumns;
		var tableHeaderStyle = CompatibleHeaderStyle(ColumnHeaderStyle);

		if (Columns is { } columns)
		{
			foreach (var column in columns)
			{
				if (column is null || column.GetOwningTableView() != this)
				{
					continue;
				}

				TableViewColumnHeader header = new();
				var headerImpl = header;
				headerImpl.SetOwnerInternal(this, column);
				header.Visibility = column.Visibility;

				var columnHeaderStyle = CompatibleHeaderStyle(column.HeaderStyle);
				var headerStyle = columnHeaderStyle is not null ? columnHeaderStyle : tableHeaderStyle;
				// Read the Setters before applying the Style seals it: reading a sealed BasedOn Style's
				// never-accessed Setters reseals it and re-merges its BasedOn setters.
				var styleSetsMinHeight = StyleSetsProperty(headerStyle, FrameworkElement.MinHeightProperty);
				var styleSetsPadding = StyleSetsProperty(headerStyle, Control.PaddingProperty);
				var styleSetsCornerRadius = StyleSetsProperty(headerStyle, Control.CornerRadiusProperty);
				var styleContentTemplate = StyleSetterValue(headerStyle, ContentControl.ContentTemplateProperty) as DataTemplate;
				var styleContentTemplateSelector = styleContentTemplate is not null ? null :
					StyleSetterValue(headerStyle, ContentControl.ContentTemplateSelectorProperty) as DataTemplateSelector;
				if (styleContentTemplateSelector is not null)
				{
					// Held off locally so the Style's selector is only asked by the probe below.
					header.ContentTemplateSelector = null;
				}
				if (headerStyle is not null)
				{
					header.Style = headerStyle;
				}

				var headerIsResizable = CanUserResizeColumns && column.CanResize;
				var headerIsSortable = canUserSortColumns && column.CanSort;
				if (!styleSetsCornerRadius)
				{
					AttachFocusCornerRadius(header);
				}
				var headerText = TableViewDetails.GetColumnHeaderText(column);
				if (!string.IsNullOrEmpty(headerText))
				{
					AutomationProperties.SetName(header, headerText);
				}
				AutomationProperties.SetAccessibilityView(header, AccessibilityView.Content);
				if (!styleSetsMinHeight)
				{
					header.MinHeight = cachedRowMinHeight;
				}
				if (!styleSetsPadding)
				{
					header.Padding = cachedHeaderCellPadding;
				}

				header.Tag = column;

				if (column.HeaderTemplateSelector is { } headerTemplateSelector)
				{
					header.Content = column.Header;
					// A local null keeps a Style's ContentTemplate from outranking the selector.
					header.ContentTemplate = null;
					header.ContentTemplateSelector = headerTemplateSelector;
				}
				else if (column.HeaderTemplate is { } headerTemplate)
				{
					header.Content = column.Header;
					header.ContentTemplate = headerTemplate;
				}
				else if (!string.IsNullOrEmpty(headerText) && IsPlainStringHeader(column) && styleContentTemplate is null)
				{
					DataTemplate? selected = null;
					if (styleContentTemplateSelector is not null)
					{
						selected = SelectStyleHeaderTemplate(styleContentTemplateSelector, header, column.Header);
					}
					if (selected is not null)
					{
						header.Content = column.Header;
						header.ContentTemplate = selected;
					}
					else
					{
						// A ContentPresenter renders a bare string through an implicit TextBlock carrying no
						// TextTrimming, so a too-wide header hard-clips mid-glyph while its cells ellipsize.
						TextBlock headerBlock = new();
						headerBlock.Text = headerText;
						headerBlock.TextTrimming = TextTrimming.CharacterEllipsis;
						headerBlock.VerticalAlignment = VerticalAlignment.Center;
						// The header's peer already announces this text.
						AutomationProperties.SetAccessibilityView(headerBlock, AccessibilityView.Raw);
						header.Content = headerBlock;
						headerImpl.SetOwnsContentInternal(true);
					}
				}
				else
				{
					if (styleContentTemplateSelector is not null)
					{
						header.ClearValue(ContentControl.ContentTemplateSelectorProperty);
					}
					header.Content = column.Header;
				}

				TableViewDetails.ApplyHeaderToolTip(header, column.HeaderToolTip);

				headerImpl.SetSortableInternal(headerIsSortable, cachedSortIndicatorWidth);
				headerImpl.SetSortDirectionInternal(column.SortDirection);

				if (headerIsSortable)
				{
					// Weak: the handler is owned by a visual the control also owns, so a strong
					// capture would keep the TableView alive through its own header.
					WeakReference<TableView> weakThis = new(this);
					var sortColumn = column;
					header.Tapped += (object _, TappedRoutedEventArgs args) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							if (strongThis.ToggleSortDirection(sortColumn))
							{
								args.Handled = true;
							}
						}
					};
				}

				// The gripper marks the press handled, which also keeps a resize drag from reaching the
				// header's Tapped handler and sorting the column.
				if (headerIsResizable)
				{
					headerImpl.SetResizeGripperInternal(
						CreateResizeGripper(header, column, cachedResizeGripperWidth, headerText));
				}

				host.Children.Add(header);
				// Template now so the parts exist for the passes below and for callers right after.
				header.ApplyTemplate();
			}
		}

		// Pin (or refresh) leading-frozen header cells at the current scroll offset.
		RefreshFrozenColumns();
		ApplyGridLinesToHeader();
		RefreshResizeGripperAvailability();

		RefreshHeaderTabStopsInternal();
		headerFocusMoveRevoker.Disposable = null;
		if ((focusedHeaderColumn is not null || focusBody) && !headerFocusMovedAway)
		{
			// The focused header if still actionable, else the first actionable header, else the body.
			TableViewColumn? target = null;
			TableViewColumn? first = null;
			foreach (var child in host.Children)
			{
				var cell = child as FrameworkElement;
				if (cell is null || !IsHeaderCellActionableInternal(cell))
				{
					continue;
				}
				var column = cell.Tag as TableViewColumn;
				first = first is not null ? first : column;
				target = column == focusedHeaderColumn ? column : target;
			}
			target = target is not null ? target : first;
			var state = focusedHeaderColumn is not null ? focusedHeaderState : removedFocusState;
			if (target is not null)
			{
				RestoreHeaderFocus(target, state);
			}
			else
			{
				FocusBodyEntryTarget(false /* select */, state);
			}
		}
	}

	private TableViewColumn? GetFocusedHeaderColumn(Panel? host, out FocusState focusState)
	{
		focusState = FocusState.Unfocused;
		var root = XamlRoot;
		if (host is null || root is null)
		{
			return null;
		}

		var focusedElement = FocusManager.GetFocusedElement(root);
		var current = focusedElement as DependencyObject;
		while (current is not null)
		{
			var parent = VisualTreeHelper.GetParent(current);
			if (parent == host)
			{
				TableViewColumn? column = null;
				if (current is FrameworkElement cell)
				{
					column = cell.Tag as TableViewColumn;
				}
				if (column is not null)
				{
					focusState = FocusState.Keyboard;
					if (focusedElement is UIElement ui)
					{
						focusState = ui.FocusState == FocusState.Keyboard ? FocusState.Keyboard : FocusState.Programmatic;
					}
				}
				return column;
			}
			current = parent;
		}

		return null;
	}

	private void RestoreHeaderFocus(TableViewColumn column, FocusState focusState)
	{
		CancelHeaderFocusRestore();
		if (TryFocusHeaderCell(column, focusState))
		{
			return;
		}

		// Whatever holds focus now is what the rebuild left behind; anything else later is a user/app move.
		WeakReference<DependencyObject>? weakFocusAtSchedule = null;
		if (XamlRoot is { } root)
		{
			if (FocusManager.GetFocusedElement(root) is DependencyObject focused)
			{
				weakFocusAtSchedule = new(focused);
			}
		}

		WeakReference<TableView> weakThis = new(this);
		WeakReference<TableViewColumn> weakColumn = new(column);
		EventHandler<object> layoutUpdatedHandler =
			(object? _, object _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.CancelHeaderFocusRestore();
					DependencyObject? focusAtSchedule = null;
					weakFocusAtSchedule?.TryGetTarget(out focusAtSchedule);
					if (!strongThis.CanRestoreHeaderFocus(focusAtSchedule))
					{
						return;
					}
					if (weakColumn.TryGetTarget(out var liveColumn))
					{
						strongThis.TryFocusHeaderCell(liveColumn, focusState);
					}
				}
			};
		LayoutUpdated += layoutUpdatedHandler;
		m_headerFocusRestoreLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= layoutUpdatedHandler);
	}

	// Focus the user or app moved elsewhere while the restore was pending is never pulled back.
	private bool CanRestoreHeaderFocus(DependencyObject? focusAtSchedule)
	{
		var root = XamlRoot;
		if (root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		if (focused is null || focused == focusAtSchedule)
		{
			return true;
		}

		var headerHost = m_headerHost;
		for (var current = focused; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (headerHost is not null && current == headerHost)
			{
				return true;
			}
		}
		return false;
	}

	private bool TryFocusHeaderCell(TableViewColumn column, FocusState focusState)
	{
		var cell = TableViewCellsPanel.CellForColumn(m_headerHost, column);
		return cell is not null && cell.Focus(focusState);
	}

	private void CancelHeaderFocusRestore()
	{
		if (m_headerFocusRestoreLayoutToken.Disposable is not null)
		{
			m_headerFocusRestoreLayoutToken.Disposable = null;
		}
	}

	internal static SortIndicatorDirection ToSortIndicatorDirection(SortDirection direction)
	{
		// Two distinct WinRT enums with matching numeric values; map explicitly rather than casting so
		// a future divergence is a compile error rather than a wrong glyph.
		switch (direction)
		{
			case SortDirection.Ascending:
				return SortIndicatorDirection.Ascending;
			case SortDirection.Descending:
				return SortIndicatorDirection.Descending;
			case SortDirection.None:
			default:
				return SortIndicatorDirection.None;
		}
	}

	internal void RefreshSortIndicators()
	{
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		foreach (var child in host.Children)
		{
			var header = child as TableViewColumnHeader;
			if (header is null)
			{
				continue;
			}

			if (header.Tag is TableViewColumn column)
			{
				header.SetSortDirectionInternal(column.SortDirection);
			}
		}
	}

	private void OnColumnHeaderStylePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Rebuild, not restyle in place: the style also decides whether density sets MinHeight/Padding.
		QueueRebuildHeaders();
	}

	private void OnCanUserSortColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// The chevron and the click handler are stamped at header-build time. Queued first so it still
		// runs when a Sorting or Sorted handler throws.
		QueueRebuildHeaders();

		// The gate turning off must also drop any sort it was responsible for; leaving the rows in a
		// sorted order with no affordance to change it would strand the user.
		if (!CanUserSortColumns)
		{
			CancelPendingSortRestore();
			// A sort cannot run while an edit is opening, and must not close it once open; clear once no edit is open.
			if (m_editState == EditState.Beginning)
			{
				QueueReshapeAfterEditCloses(() =>
				{
					// A source sort the app declared since the opt-out is newer: adopt it before clearing.
					if (m_sortReconcileQueued)
					{
						m_sortReconcileQueued = false;
						ReconcileSortStateWithSource();
					}
					if (!CanUserSortColumns)
					{
						ClearSortInternal(false);
					}
				});
			}
			else
			{
				ClearSortInternal(false);
			}
		}
	}

	internal void OnColumnCanSortChanged(TableViewColumn? column)
	{
		// The chevron and the click handler are stamped at header-build time. Queued first so it still
		// runs when a Sorting or Sorted handler throws.
		QueueRebuildHeaders();

		// A column that just opted out must not keep an active sort applied to it. One lit only by the
		// app's source sort keeps it: the app owns that axis.
		if (column is not null && !column.CanSort && column.SortDirection != SortDirection.None && !IsSortLitOnlyByApp())
		{
			if (m_editState == EditState.Beginning)
			{
				WeakReference<TableViewColumn> weakColumn = new(column);
				QueueReshapeAfterEditCloses(() =>
				{
					// A source sort the app declared since the opt-out is newer: adopt it before clearing.
					if (m_sortReconcileQueued)
					{
						m_sortReconcileQueued = false;
						ReconcileSortStateWithSource();
					}
					if (weakColumn.TryGetTarget(out var optedOut) && !optedOut.CanSort && !IsSortLitOnlyByApp())
					{
						SortByColumnInternal(optedOut, SortDirection.None, false, false);
					}
				});
			}
			else
			{
				// Bypasses the CanSort gate that just closed; the public methods keep refusing.
				SortByColumnInternal(column, SortDirection.None, false, false);
			}
		}
	}

	private double GetHeaderMeasuredWidthForColumn(TableViewColumn column)
	{
		// Own the header host's concrete panel type here so the layout engine (TableView_Layout.cpp)
		// pulls the header's measured width through this seam and never casts to TableViewCellsPanel.
		if (m_headerHost is { } headerHost)
		{
			if (headerHost is TableViewCellsPanel cellsPanel)
			{
				return cellsPanel.MeasuredWidthForColumn(column);
			}
		}

		return 0.0;
	}

	internal void QueueRebuildHeaders()
	{
		// Before the template applies there is no header host; OnApplyTemplate builds headers once, so a
		// rebuild queued now would be a wasted no-op (RebuildHeaders early-returns on a null host anyway).
		if (m_headerHost is null)
		{
			return;
		}

		// A rebuild is already scheduled for this tick -- collapse the burst into one.
		if (m_rebuildHeadersQueued)
		{
			return;
		}

		var dispatcher = DispatcherQueue;
		if (dispatcher is null)
		{
			// No dispatcher (teardown) -- rebuild synchronously so headers are not left stale.
			RebuildHeaders();
			return;
		}

		m_rebuildHeadersQueued = true;
		WeakReference<TableView> weakThis = new(this);
		if (!dispatcher.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_rebuildHeadersQueued = false;
					try
					{
						strongThis.RebuildHeaders();
						// Header sizes may have changed; re-resolve column widths against the new headers.
						strongThis.InvalidateMeasure();
					}
					catch (Exception)
					{
						TableViewTelemetry.ReportError(strongThis.m_telemetrySession, 0,
							TableViewTip.Operation.HeaderRefresh, TableViewTip.Stage.HeaderRefresh, true);
						// Coalesced header rebuild is best-effort; never fail-fast the dispatcher.
					}
				}
			}))
		{
			// Enqueue failed -- fall back to a synchronous rebuild so headers are not left stale.
			m_rebuildHeadersQueued = false;
			RebuildHeaders();
		}
	}

	private void RaiseColumnsStructureChanged()
	{
		// FromElement only: don't create a peer just to announce when no client is connected.
		if (FrameworkElementAutomationPeer.FromElement(this) is TableViewAutomationPeer peer)
		{
			peer.RaiseStructureChangedForColumnsChange();
		}
	}

	private void QueueRaiseColumnsStructureChanged()
	{
		if (!AutomationPeer.ListenerExists(AutomationEvents.StructureChanged))
		{
			return;
		}

		if (m_columnsStructureChangedQueued)
		{
			return;
		}

		var dispatcher = DispatcherQueue;
		if (dispatcher is null)
		{
			// No dispatcher (test host / teardown): nothing to coalesce against, so raise inline.
			RaiseColumnsStructureChanged();
			return;
		}

		// Deferred so a burst of Columns VectorChanged callbacks announces once, with the final shape.
		m_columnsStructureChangedQueued = true;
		WeakReference<TableView> weakThis = new(this);
		if (!dispatcher.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_columnsStructureChangedQueued = false;
					try
					{
						strongThis.RaiseColumnsStructureChanged();
					}
					catch (Exception)
					{
						// Best-effort announcement; never fail-fast the dispatcher.
					}
				}
			}))
		{
			m_columnsStructureChangedQueued = false;
			RaiseColumnsStructureChanged();
		}
	}

	private void StopAwaitingSource()
	{
		if (m_awaitedSource?.TryGetTarget(out var awaitedSource) == true)
		{
			awaitedSource.RemoveOwnerReleasedHandler(this);
		}
		m_awaitedSource = null;
	}

	private void ReclaimItemsSource()
	{
		if (m_activeSource is not null ? !m_activeSourceReleased : ItemsSource is not TableViewSource)
		{
			return;
		}

		AdoptItemsSource(true /* reclaimActiveSource */);
		if (!m_activeSourceReleased)
		{
			QueueDeferredEditReshapeDrain();
		}
		// While drained, OnRowsRepeaterLoaded re-sources the repeater.
		if (!m_rowsSourceDrained)
		{
			RefreshRowsPipeline();
		}
		// A source refused while another table owned it arrives here.
		NoteTipInitializationSource();
	}

	private void OnTableViewUnloaded()
	{
		// A re-parent can raise Loaded before the stale Unloaded; tearing down now would leave a live
		// table detached from its source, drained repeater, scrollers and ThemeSettings.
		if (IsLoaded)
		{
			return;
		}

		CancelColumnResizeDrag();
		m_headerSortSpaceArmedColumn = null;
		IgnoreTipInitializationUnloadedBeforeRender();
		IgnoreTip(TableViewTip.IgnoreReason.Unloaded);
		SampleInstanceTelemetry();
		TableViewTelemetry.EndSession(m_telemetrySession);
		// Neither a pending restore nor a remembered position may outlive this tree.
		CancelReshapeFocusRestore();
		ForgetBodyFocusPosition();
		m_reshapeFocusValid = false;
		m_reshapeFocusRow = -1;
		CancelHeaderFocusRestore();
		// A static FocusManager subscription; don't leave it to the destructor, which can run after XAML shutdown.
		m_headerCollapseFocusRevoker.Disposable = null;
		RevokeTipInitializationLayout();

		if (m_pendingGroupFocusLayoutToken.Disposable is not null)
		{
			m_pendingGroupFocusLayoutToken.Disposable = null;
		}
		if (m_pendingGroupRowRefreshLayoutToken.Disposable is not null)
		{
			m_pendingGroupRowRefreshLayoutToken.Disposable = null;
		}
		if (m_terminalGridLinesLayoutToken.Disposable is not null)
		{
			m_terminalGridLinesLayoutToken.Disposable = null;
		}
		if (m_terminalGridLineRow?.TryGetTarget(out var terminalRow) == true)
		{
			terminalRow.SetTerminalGridLineSuppression(new());
		}
		if (m_terminalGridLineGroupHeader?.TryGetTarget(out var terminalHeader) == true)
		{
			terminalHeader.SetTerminalBottomGridLineSuppression(false);
		}
		m_terminalGridLineRowSizeChangedRevoker.Disposable = null;
		m_terminalGridLineRow = null;
		m_terminalGridLineGroupHeader = null;
		m_suppressTrailingGridLine = false;
		m_suppressBottomGridLine = false;
		m_terminalGridLineGeometryRetryAvailable = true;
		m_terminalGridLineColumnIndex = -1;
		m_terminalGridLineHorizontalOffset = double.NaN;
		m_terminalGridLineVerticalOffset = double.NaN;
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;
		m_pendingGroupFocusFromPointerGesture = false;

		// Null ItemsSource on unload to release repeater cache work before it ticks on a detached subtree.
		// OnRowsRepeaterLoaded re-sources cached pages when they return.
		if (m_rowsRepeater is { } repeater)
		{
			// Re-sourcing on load hands SelectionModel a new view, and setting Source always clears.
			// Hold the selected item so the reload re-selects it instead of dropping it.
			StashSelectionForReload();

			try
			{
				repeater.ItemsSource = null;
			}
			catch (Exception)
			{
			}
			m_rowsSourceDrained = true; // Remember that Loaded must restore the source.
		}

		// An unloaded table may stay alive until GC; let a newly loaded one bind the source meanwhile.
		// OnTableViewLoaded reclaims it.
		// Flag first: the release runs other tables' waiters synchronously.
		if (m_activeSource is { } activeSource && !m_activeSourceReleased)
		{
			m_activeSourceReleased = true;
			activeSource.ReleaseOwningTableView(this);
		}
		StopAwaitingSource();

		// Detach ViewChanged so deferred scroll callbacks do not run after unload.
		if (m_bodyScroller is not null)
		{
			if (m_bodyScrollerViewChangedToken.Disposable is not null)
			{
				try
				{
					m_bodyScrollerViewChangedToken.Disposable = null;
				}
				catch (Exception)
				{
				}
			}
		}
		m_bodyScrollerSizeChangedRevoker.Disposable = null;
		m_bodyScroller = null;
		m_headerScroller = null;

		// Drop the ThemeSettings subscription and instance so a subsequent Loaded re-creates it against
		// the (possibly different) window's WindowId. IsHighContrast falls back to AccessibilitySettings
		// while detached.
		ReleaseThemeSettings();
	}

	private void ReleaseThemeSettings()
	{
		// TODO Uno: AppWindow.Destroying is not implemented, so nothing subscribes and there is nothing to drop here.
#if !HAS_UNO
		if (auto appWindow = std::exchange(m_appWindow, nullptr))
		{
			try
			{
				appWindow.Destroying(std::exchange(m_appWindowDestroyingToken, {}));
			}
			catch (...)
			{
			}
		}
#endif

		// Release can surface a teardown failure; contain it so this stays noexcept.
		// Release before revoke() so a dead object's stale weak_ref makes it a no-op.
		// TODO Uno: The revoker's closure holds the ThemeSettings strongly, so the unsubscribe always
		// runs here; a managed event removal cannot throw across a noexcept boundary.
		m_themeSettings = null;
		m_themeSettingsChangedRevoker.Disposable = null;
	}

	private void OnHeaderBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		var headerHost = m_headerHost;
		var target = args.TargetElement;
		var bodyScroller = m_bodyScroller;
		if (headerHost is null || target is null || bodyScroller is null)
		{
			return;
		}

		var viewport = bodyScroller.ViewportWidth;
		if (viewport <= 0.0)
		{
			return;
		}

		var targetRect = args.TargetRect;
		if (targetRect.Width <= 0.0 && targetRect.Height <= 0.0)
		{
			// Focus-driven BringIntoView passes an empty rect; using it as-is makes the right-edge
			// branch below under-scroll by the element's width.
			if (target is FrameworkElement targetFe)
			{
				targetRect = new Rect(0.0f, 0.0f,
					(float)targetFe.ActualWidth, (float)targetFe.ActualHeight);
			}
		}

		Rect bounds = default;
		try
		{
			bounds = target.TransformToVisual(headerHost).TransformBounds(targetRect);
		}
		catch (Exception)
		{
			return; // Not connected yet; a stale rect would scroll to the wrong place.
		}

		var current = bodyScroller.HorizontalOffset;
		var offset = current;
		if (bounds.X < current)
		{
			offset = bounds.X;
		}
		else if (bounds.X + bounds.Width > current + viewport)
		{
			offset = bounds.X + bounds.Width - viewport;
		}

		// Clamped before the comparison: a negative offset would otherwise mark the event handled
		// while the clamped scroll went nowhere.
		offset = StdMath.Max(0.0, offset);

		if (Math.Abs(offset - current) >= 0.5)
		{
			// Handled only when we actually redirect. Marking it unconditionally also silenced
			// ancestor scrollers, so a TableView below the fold never scrolled into view on header
			// focus. The header scroller cannot scroll itself anyway (HorizontalScrollMode=Disabled).
			args.Handled = true;
			bodyScroller.ChangeView(offset, null, null, true /* disableAnimation */);
		}
	}

	private static bool ApplyColumnResizeDrag(TableView? owner, TableViewColumn col, ColumnResizeDragState state, double totalDelta)
	{
		var lo = TableViewColumn.NonNegativeFiniteOrZero(col.MinWidth);
		var hi = TableViewColumn.MaxWidthForColumn(col, lo);

		// The upper bound never falls below the width the drag started from, so a table that
		// already overflows can still shrink.
		lo = StdMath.Max(lo, state.bounds.Min);
		hi = StdMath.Max(lo, StdMath.Min(hi, StdMath.Max(state.startValue, state.bounds.Max)));

		var next = StdMath.Clamp(state.startValue + totalDelta, lo, hi);

		// Pinned at a bound the pointer keeps moving but the width does not. Writing anyway would
		// re-run measure, and on a Star column it would also latch the width to pixels.
		var current = col.Width;
		var currentValue =
			current.GridUnitType == GridUnitType.Pixel ? current.Value : col.ActualWidth;
		if (Math.Abs(currentValue - next) < 0.0001 &&
			!(state.commitPixelWidth && current.GridUnitType != GridUnitType.Pixel))
		{
			return false;
		}

		var columnImpl = col;
		if (!state.didWrite && owner is not null)
		{
			owner.FreezeColumnsBeforeResize(col, state);
		}
		using var resizeScope = columnImpl.BeginUserResizeScope();
		col.Width = GridLengthHelper.FromPixels(next);
		state.didWrite = true;
		return true;
	}

	internal ResizeGripper CreateResizeGripper(
		FrameworkElement headerCell,
		TableViewColumn column,
		double gripperWidth,
		string headerText)
	{
		WeakReference<TableView> weakThis = new(this);
		ResizeGripper gripperVisual = new();
		gripperVisual.DragOrientation = Orientation.Horizontal;
		// Pointer affordance only here: the header cell owns keyboard focus, and one tab stop per
		// column would sit between the user and the data.
		gripperVisual.IsTabStop = false;
		AutomationProperties.SetAccessibilityView(gripperVisual, AccessibilityView.Raw);
		// Trailing edge in both flow directions; deliberately NOT swapped for RTL (see RebuildHeaders).
		gripperVisual.HorizontalAlignment = HorizontalAlignment.Right;
		gripperVisual.Width = gripperWidth;
		if (!string.IsNullOrEmpty(headerText))
		{
			gripperVisual.OwnerName = headerText;
		}

		// Deltas must be measured against the TableView: its frame does not move while a column
		// resizes, and unlike the XamlRoot it is below any scale an app applies above the control.
		gripperVisual.ManipulationContainer = this;

		WeakReference<FrameworkElement> weakHeaderCell = new(headerCell);
		gripperVisual.PointerEntered += (object _, PointerRoutedEventArgs _) =>
		{
			if (weakHeaderCell.TryGetTarget(out var cell))
			{
				TableViewDetails.SuppressOwnedToolTip(cell);
			}
		};
		// Exited alone can be missed (a gesture torn down, the header left from the gripper's edge).
		PointerEventHandler restoreHeaderToolTip = (object _, PointerRoutedEventArgs _) =>
		{
			if (weakHeaderCell.TryGetTarget(out var cell))
			{
				TableViewDetails.RestoreOwnedToolTip(cell);
			}
		};
		gripperVisual.PointerExited += restoreHeaderToolTip;
		gripperVisual.PointerCanceled += restoreHeaderToolTip;
		gripperVisual.PointerCaptureLost += restoreHeaderToolTip;
		headerCell.PointerExited += restoreHeaderToolTip;

		// shared_ptr so the handler closures keep it alive, and so Escape can reach the live drag.
		ColumnResizeDragState state = new();
		WeakReference<TableViewColumn> weakColumn = new(column);

		// The column owns the width: capture it when the drag starts, then apply the reported offset
		// against that anchor and clamp. Nothing is written until the user actually drags.
		gripperVisual.DragStarted +=
			(ResizeGripper sender, object _) =>
			{
				weakThis.TryGetTarget(out var strongThis);

				// Before the guard below: a stale didWrite would revert to the previous drag's start width.
				state.didWrite = false;
				state.didDelta = false;
				state.frozen.Clear();
				state.lockedHoldsBefore = null;
				state.lastPressTimestamp = 0;
				state.commitPixelWidth = false;

				// One resize at a time. Manipulation arbitrates per element, so a second contact on a
				// DIFFERENT gripper would otherwise run a concurrent drag that Escape could not reach.
				if (strongThis is not null)
				{
					if (strongThis.m_activeColumnResizeDrag is { } active)
					{
						if (active.gripper is not null && active.gripper.TryGetTarget(out var activeGripper) && activeGripper.IsDragging)
						{
							if (sender is ResizeGripper self)
							{
								self.EndDrag(true /* canceled */);
							}
							return;
						}
					}
				}

				if (weakColumn.TryGetTarget(out var col))
				{
					state.startValue = col.ActualWidth;
					// TODO Uno: ReadLocalValue returns the bound value, not the BindingExpression, so a
					// cancel restores a snapshot rather than the binding.
					state.startWidth = col.ReadLocalValue(TableViewColumn.WidthProperty);
					if (strongThis is not null)
					{
						state.bounds = strongThis.ResizeBoundsForColumn(col);
					}
				}

				// Published so Escape can find the gesture in flight; the gripper owns everything else
				// about it.
				if (strongThis is not null)
				{
					state.gripper = sender is ResizeGripper senderGripper ? new WeakReference<ResizeGripper>(senderGripper) : null;
					strongThis.m_activeColumnResizeDrag = state;
				}
			};

		gripperVisual.DragDelta +=
			(ResizeGripper _, ResizeGripperDragDeltaEventArgs vargs) =>
			{
				if (!weakColumn.TryGetTarget(out var col))
				{
					return;
				}

				state.didDelta = true;
				weakThis.TryGetTarget(out var strongThis);
				ApplyColumnResizeDrag(strongThis, col, state, vargs.TotalDelta);
			};

		// Canceled means the gesture was torn down rather than released (Escape, palm rejection, the
		// header rebuilt mid-drag): revert. A clean release announces the new width instead.
		gripperVisual.DragCompleted +=
			(ResizeGripper _, ResizeGripperDragCompletedEventArgs cargs) =>
			{
				weakThis.TryGetTarget(out var strongThis);

				// Cleared first: reverting the width below can rebuild the headers, and a stale pointer
				// here would let Escape end a gesture that no longer exists.
				if (strongThis is not null && strongThis.m_activeColumnResizeDrag == state)
				{
					strongThis.m_activeColumnResizeDrag = null;
				}
				state.gripper = null;

				weakColumn.TryGetTarget(out var col);

				if (cargs.Canceled)
				{
					// Before the widths below: restoring them runs app code that may end the holds again.
					if (strongThis is not null && state.lockedHoldsBefore is { } lockedHoldsBefore)
					{
						strongThis.m_lockedColumnHolds = lockedHoldsBefore.first;
						strongThis.m_lockedColumnHoldsSignature = lockedHoldsBefore.second;
						strongThis.InvalidateMeasure();
					}

					// Only when a write actually happened, so a press that never moved cannot pin an
					// Auto/Star column.
					// Non-empty only when a freeze actually ran, so this needs no didWrite gate: a write
					// that threw after freezing would otherwise strand the predecessors as pixels.
					foreach (var entry in state.frozen)
					{
						if (entry.column is not null && entry.column.TryGetTarget(out var frozenCol))
						{
							TableView.RestoreColumnWidth(frozenCol, entry.width);
						}
					}

					if (state.didWrite && col is not null)
					{
						TableView.RestoreColumnWidth(col, state.startWidth);
					}

					// Mid-drag refreshes were suppressed, so a sibling width change during the gesture
					// still needs its availability applied.
					if (strongThis is not null)
					{
						strongThis.RefreshResizeGripperAvailability();
					}
					return;
				}

				// Attributed to the header cell: it is the focusable element that represents the column,
				// and the one assistive technology is already on during a keyboard resize.
				if (strongThis is not null && col is not null && state.didDelta)
				{
					strongThis.AnnounceColumnWidth(weakHeaderCell.TryGetTarget(out var announcer) ? announcer : null, col);
				}

				if (strongThis is not null)
				{
					strongThis.RefreshResizeGripperAvailability();
				}
			};

		// PointerPressed with handledEventsToo: the gripper handles its presses, so DoubleTapped never arrives.
		gripperVisual.AddHandler(
			UIElement.PointerPressedEvent,
			new PointerEventHandler(
				(object sender, PointerRoutedEventArgs args) =>
				{
					var gripper = sender as UIElement;
					// Relative to the XamlRoot content, the space the slop is in: local DIPs grow under an ancestor scale.
					var point = args.GetCurrentPoint(null);
					if (point is null ||
						args.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse ||
						!point.Properties.IsLeftButtonPressed)
					{
						state.lastPressTimestamp = 0;
						return;
					}

					var timestamp = point.Timestamp;
					var position = point.Position;
					var slop = TableView.DoubleClickSlop(gripper);
					var isDoubleClick =
						state.lastPressTimestamp != 0 &&
						timestamp >= state.lastPressTimestamp &&
						(timestamp - state.lastPressTimestamp) <= TableView.DoubleClickIntervalMicroseconds() &&
						Math.Abs(position.X - state.lastPressPosition.X) <= slop &&
						Math.Abs(position.Y - state.lastPressPosition.Y) <= slop;

					state.lastPressTimestamp = isDoubleClick ? 0 : timestamp;
					state.lastPressPosition = position;

					if (isDoubleClick)
					{
						if (weakThis.TryGetTarget(out var strongThis) && weakColumn.TryGetTarget(out var col))
						{
							strongThis.AutoFitColumn(col);
						}
					}
				}),
			true /* handledEventsToo */);

		return gripperVisual;
	}

	internal static double DoubleClickSlop(UIElement? element)
	{
		// SM_CXDOUBLECLK is in physical pixels; positions are DIPs. Not cached: both can change at run time.
		// TODO Uno: 4 is the Windows default for GetSystemMetrics(SM_CXDOUBLECLK / SM_CYDOUBLECLK); no per-host source.
		// const auto cx = static_cast<double>(::GetSystemMetrics(SM_CXDOUBLECLK));
		// const auto cy = static_cast<double>(::GetSystemMetrics(SM_CYDOUBLECLK));
		const double cx = 4.0;
		const double cy = 4.0;
		var physical = Math.Max(Math.Max(cx, cy), 4.0) / 2.0;

		var scale = 1.0;
		if (element is not null)
		{
			if (element.XamlRoot is { } root)
			{
				var rasterization = root.RasterizationScale;
				if (rasterization > 0.0)
				{
					scale = rasterization;
				}
			}
		}

		return physical / scale;
	}

	internal static ulong DoubleClickIntervalMicroseconds()
	{
		// PointerPoint.Timestamp is in microseconds, GetDoubleClickTime in milliseconds.
		// return static_cast<uint64_t>(::GetDoubleClickTime()) * 1000ull;
		return (ulong)global::Windows.UI.ViewManagement.UISettings.GetDoubleClickTime() * 1000UL;
	}

	// Re-measures at the previous constraint so the probe leaves layout as it found it.
	private static double MeasureUnconstrainedWidth(UIElement cell)
	{
		var fe = cell as FrameworkElement;
		if (fe is null || cell.Visibility != Visibility.Visible)
		{
			return 0.0;
		}

		var previous = LayoutInformation.GetAvailableSize(fe);
		cell.Measure(new Size(double.PositiveInfinity, previous.Height));
		var width = cell.DesiredSize.Width;
		cell.Measure(previous);
		return double.IsFinite(width) ? StdMath.Max(0.0, width) : 0.0;
	}

	// Measured fresh: cached Auto widths are grow-only and absent for Pixel and Star columns.
	internal bool AutoFitColumn(TableViewColumn column)
	{
		if (GetAvailableResizeGripper(column) is null)
		{
			return false;
		}

		var fitted = 0.0;
		if (ShouldShowColumnHeaders())
		{
			if (m_headerHost is { } host)
			{
				if (TableViewCellsPanel.CellForColumn(host, column) is { } headerCell)
				{
					fitted = MeasureUnconstrainedWidth(headerCell);
				}
			}
		}

		if (m_rowsRepeater is { } repeater)
		{
			ForEachRealizedRow(row =>
			{
				if (repeater.GetElementIndex(row) < 0)
				{
					return;
				}

				var rowImpl = row;
				UIElement? cell = TableViewCellsPanel.CellForColumn(rowImpl.GetCellsHostPanelInternal(), column);
				if (cell is null)
				{
					return;
				}

				// An open editor is not the data's width; hold the column's current width for that row.
				if (cell == rowImpl.GetEditingCellWrapper())
				{
					fitted = StdMath.Max(fitted, column.ActualWidth);
					return;
				}

				fitted = StdMath.Max(fitted, MeasureUnconstrainedWidth(cell));
			});
		}

		if (!(fitted > 0.0))
		{
			return false;
		}

		var lo = TableViewColumn.NonNegativeFiniteOrZero(column.MinWidth);
		var hi = TableViewColumn.MaxWidthForColumn(column, lo);
		return TryResizeColumnFromAutomation(column, StdMath.Clamp(fitted, lo, hi), true /* commitPixelWidth */);
	}

	internal void CancelColumnResizeDrag()
	{
		var state = m_activeColumnResizeDrag;
		if (state is null)
		{
			return;
		}

		if (state.gripper is not null && state.gripper.TryGetTarget(out var gripper) && gripper.IsDragging)
		{
			// The DragCompleted handler clears m_activeColumnResizeDrag.
			try
			{
				gripper.EndDrag(true /* canceled */);
			}
			catch (Exception)
			{
				/* best-effort: EndDrag consumer handlers must not strand state. */
			}
		}
		else
		{
			m_activeColumnResizeDrag = null;
		}
	}

	// The same gates that decide whether the gripper exists and is shown, so UIA never offers a
	// resize the pointer cannot perform.
	internal bool IsColumnResizeAllowed(TableViewColumn? column)
	{
		return column is not null &&
			CanUserResizeColumns &&
			column.CanResize &&
			column.Visibility == Visibility.Visible &&
			column.GetOwningTableView() == this;
	}

	internal ResizeGripper? GetAvailableResizeGripper(TableViewColumn? column)
	{
		if (!IsColumnResizeAllowed(column))
		{
			return null;
		}

		var host = m_headerHost;
		if (host is null)
		{
			return null;
		}

		var headerCell = TableViewCellsPanel.CellForColumn(host, column);
		if (headerCell is null || IsColumnResizePinned(column))
		{
			return null;
		}

		return FindResizeGripperInCell(headerCell);
	}

	internal bool CanResizeColumnFromAutomation(TableViewColumn? column)
	{
		return GetAvailableResizeGripper(column) is not null;
	}

	internal bool TryResizeColumnFromAutomation(TableViewColumn column, double width, bool commitPixelWidth = false)
	{
		var gripper = GetAvailableResizeGripper(column);
		if (gripper is null || gripper.IsDragging)
		{
			return false;
		}

		// DragStarted anchors the gesture at ActualWidth, so the offset is taken from the same value.
		var totalDelta = width - column.ActualWidth;
		commitPixelWidth = commitPixelWidth && column.Width.GridUnitType != GridUnitType.Pixel;

		// Rounding noise from the pixel conversion; writing it would pin an Auto or Star column.
		if (Math.Abs(totalDelta) < 1e-3 && !commitPixelWidth)
		{
			return true;
		}

		gripper.BeginDrag();
		// DragStarted ends the gesture at once while another column's drag is in flight.
		if (!gripper.IsDragging)
		{
			return false;
		}

		static bool isDragOf(ColumnResizeDragState? dragState, ResizeGripper target)
		{
			return dragState is not null && dragState.gripper is not null &&
				dragState.gripper.TryGetTarget(out var dragGripper) && dragGripper == target;
		}

		if (commitPixelWidth && isDragOf(m_activeColumnResizeDrag, gripper))
		{
			m_activeColumnResizeDrag!.commitPixelWidth = true;
		}

		var applied = true;
		try
		{
			// TryDrag drops a step under its jitter deadband, but an absolute UIA width is no jitter:
			// apply it as DragDelta would, announcing only if it wrote.
			if (!gripper.TryDrag(totalDelta))
			{
				var state = m_activeColumnResizeDrag;
				applied = isDragOf(state, gripper);
				if (applied)
				{
					state!.didDelta = ApplyColumnResizeDrag(this, column, state, totalDelta);
				}
			}
		}
		catch (Exception)
		{
			gripper.EndDrag(true /* canceled */);
			throw;
		}

		gripper.EndDrag(!applied /* canceled */);
		return applied;
	}

	internal ResizeGripper? FindResizeGripperInCell(FrameworkElement? headerCell)
	{
		if (headerCell is TableViewColumnHeader header)
		{
			return header.GetResizeGripperInternal();
		}
		return null;
	}

	private void OnCanUserResizeColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		ExpireLockedColumnHolds();
		RebuildHeaders();
	}
}
