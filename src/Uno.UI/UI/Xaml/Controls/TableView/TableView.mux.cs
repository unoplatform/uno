// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
	private const string s_HeaderGridLineName = "TableViewHeaderGridLine";
	private const string s_ResizeGripperWidthKey = "TableViewResizeGripperWidth";
	// Matches TableViewResizeGripperWidth in the theme dictionaries; used when that key is missing or
	// unusable.
	private const double c_resizeGripperWidthFallback = 8.0;
	private const string s_SortIndicatorName = "TableViewSortIndicator";
	// ScrollViewer template names are documented; ancestors are resolved by walking from child parts.

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

	private static ScrollViewer? FindScrollViewerAncestor(DependencyObject? start)
	{
		var node = start;
		while (node is not null)
		{
			if (node is ScrollViewer sv)
			{
				return sv;
			}
			node = VisualTreeHelper.GetParent(node);
		}
		return null;
	}

	// TODO Uno: TryLookup maps to TryGetValue with shouldCheckSystem: false; Uno's public overload also
	// falls back to framework resources, which WinUI's IMap::Lookup does not.
	private static object? LookupInThemeDictionaries(
		ResourceDictionary? dict, string themeKey, object boxedKey)
	{
		if (dict is null)
		{
			return null;
		}
		if (dict.ThemeDictionaries is { } themeDicts)
		{
			var boxedThemeKey = themeKey;
			if (themeDicts.ContainsKey(boxedThemeKey))
			{
				if (themeDicts[boxedThemeKey] is ResourceDictionary themed)
				{
					if (themed.TryGetValue(boxedKey, out var v, shouldCheckSystem: false) && v is not null)
					{
						return v;
					}
				}
			}
		}
		if (dict.MergedDictionaries is { } merged)
		{
			for (var i = merged.Count; i-- > 0;)
			{
				if (LookupInThemeDictionaries(merged[i], themeKey, boxedKey) is { } v)
				{
					return v;
				}
			}
		}
		return null;
	}

	private static object? LookupElementResource(FrameworkElement? start, string key, bool highContrast = false)
	{
		object boxedKey = key;
		// Theme-scoped resources must resolve against the element's ActualTheme.
		var theme = start is not null ? start.ActualTheme : ElementTheme.Default;
		// High Contrast is orthogonal to ActualTheme; callers pass cached HC state for hot-path brush lookups.
		var themeKey =
			highContrast ? "HighContrast" : (theme == ElementTheme.Light ? "Light" : "Default");

		var walker = start;
		while (walker is not null)
		{
			if (walker.Resources is { } resources)
			{
				if (LookupInThemeDictionaries(resources, themeKey, boxedKey) is { } found)
				{
					return found;
				}
				if (resources.TryGetValue(boxedKey, out var foundValue, shouldCheckSystem: false) && foundValue is not null)
				{
					return foundValue;
				}
			}
			walker = walker.Parent as FrameworkElement;
		}

		if (Application.Current is { } app)
		{
			if (app.Resources is { } resources)
			{
				if (LookupInThemeDictionaries(resources, themeKey, boxedKey) is { } found)
				{
					return found;
				}
				return resources.TryGetValue(boxedKey, out var foundValue, shouldCheckSystem: false) ? foundValue : null;
			}
		}
		return null;
	}

	private static readonly Thickness s_zeroThickness = new(0, 0, 0, 0);

	private static bool WantsHorizontalLines(TableViewGridLinesVisibility visibility) =>
		visibility == TableViewGridLinesVisibility.Horizontal ||
		visibility == TableViewGridLinesVisibility.All;

	private static bool WantsVerticalLines(TableViewGridLinesVisibility visibility) =>
		visibility == TableViewGridLinesVisibility.Vertical ||
		visibility == TableViewGridLinesVisibility.All;

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
		cache.font.hasHeaderFontSize = false;
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

	private static Brush CreateGridLineFallbackBrush(FrameworkElement start, bool highContrast)
	{
		var color = highContrast
			? Colors.White
			: (start.ActualTheme == ElementTheme.Light
				? ColorHelper.FromArgb(0x29, 0x00, 0x00, 0x00)
				: ColorHelper.FromArgb(0x29, 0xff, 0xff, 0xff));

		if (highContrast)
		{
			color = LookupElementResource(start, "SystemColorWindowTextColor", true) is Color resolvedColor
				? resolvedColor
				: color;
		}

		return new SolidColorBrush(color);
	}

	internal Brush GetGridLineBrush()
	{
		var cache = GetTableViewResourceCache(this);
		var highContrast = IsHighContrast();
		var theme = ActualTheme;
		if (cache.gridLine.hasBrush &&
			cache.gridLine.theme == theme &&
			cache.gridLine.highContrast == highContrast)
		{
			return cache.gridLine.brush!;
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
	//     // Must run while this control still holds the selector: once anything has been recycled the
	//     // pools hang off the cached templates and close a cycle the reference tracker cannot walk.
	//     if (auto const selector = m_rowTemplateSelector.get())
	//     {
	//         winrt::get_self<::TableViewRowTemplateSelector>(selector)->Detach();
	//     }
	// }
#endif

	public TableView()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_TableView);

		this.SetTabularDefaultStyleKey();

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
					strongThis.OnKeyDownForNavigation(sender, args);
				}
			});
		// Ancestor PART_BodyScroller marks nav keys Handled before they bubble here; handledEventsToo:true lets us still act on them.
		// AddHandler takes the handler as IInspectable, so the delegate must be boxed (see RoutedEventHelpers.h).
		AddHandler(UIElement.KeyDownEvent, m_keyDownHandler, true /* handledEventsToo */);

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

		// The header cell's subtree does not observe the ambient FlowDirection auto-flip, so
		// RebuildHeaders stamps the trailing-edge alignment from the control's FlowDirection. That
		// stamp is not self-updating: rebuild the headers when the direction actually flips.
		RegisterPropertyChangedCallback(
			FrameworkElement.FlowDirectionProperty,
			(DependencyObject _, DependencyProperty _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.QueueRebuildHeaders();
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
		}

		InvalidateTableViewResourceCache(this);
		if (IsLoaded)
		{
			RebuildHeaders();
			RefreshGridLinesOnRealizedRows();
		}
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		InvalidateTableViewResourceCache(this);

		// Detach old wiring
		if (m_pendingFocusLayoutToken.Disposable is not null)
		{
			m_pendingFocusLayoutToken.Disposable = null;
		}
		if (m_pendingGroupFocusLayoutToken.Disposable is not null)
		{
			m_pendingGroupFocusLayoutToken.Disposable = null;
		}
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
			// A live popup would still host content parented into the abandoned band.
			ReleaseHeaderToolTips(oldHeaderHost);

			// Mirror the rowsRepeater Loaded cleanup for the header host.
			if (m_headerHostLoadedToken.Disposable is not null)
			{
				m_headerHostLoadedToken.Disposable = null;
			}
		}
		if (m_bodyScroller is not null)
		{
			m_bodyScrollerViewChangedToken.Disposable = null;
			m_bodyScrollerSizeChangedRevoker.Disposable = null;
		}

		// Reset resolved-on-Loaded refs so re-templating re-resolves them against the new tree.
		m_headerRow = null;
		m_headerScroller = null;
		m_bodyScroller = null;

		m_rowsRepeater = GetTemplateChild(s_RowsRepeaterPartName) as ItemsRepeater;
		m_headerRow = GetTemplateChild(s_HeaderRowPartName) as FrameworkElement;
		m_headerHost = GetTemplateChild(s_HeaderHostPartName) as Panel;
		m_emptyStatePresenter = GetTemplateChild(s_EmptyStatePresenterPartName) as ContentControl;
		WeakReference<TableView> weakThis = new(this);

		// Drive the repeater from the active source once the template is alive. The source itself is
		// unchanged, so this only pushes its view into the freshly built repeater.
		RefreshRowsPipeline();

		// Defer ScrollViewer ancestor lookup until Loaded because template parts are not fully connected here.
		if (m_headerHost is { } headerHost)
		{
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
		}

		if (m_rowsRepeater is { } repeater)
		{
			// Assigned here rather than in the template: the selector needs an owning TableView to map
			// an item to its row kind, and XAML has no way to hand it one.
			var selector = new TableViewRowTemplateSelector();
			selector.SetOwningTableViewInternal(this);
			m_rowTemplateSelector = selector;
			repeater.ItemTemplate = selector;

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
					}
					catch (Exception)
					{
						// Theme-switch refresh is best-effort.
					}
				};
			ActualThemeChanged += actualThemeChangedHandler;
			m_actualThemeChangedToken.Disposable = Disposable.Create(() => ActualThemeChanged -= actualThemeChangedHandler);
		}
	}

	private void OnHeaderHostLoaded(object sender, RoutedEventArgs args)
	{
		if (m_headerScroller is not null)
		{
			return; // already resolved
		}
		if (m_headerHost is { } headerHost)
		{
			ScrollViewer? scroller = null;
			try
			{
				scroller = FindScrollViewerAncestor(headerHost);
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
				scroller = FindScrollViewerAncestor(repeater);
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
					(object _, SizeChangedEventArgs _) =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.InvalidateMeasure();
							strongThis.RefreshFrozenColumns();
						}
					};
				bodyScroller.SizeChanged += sizeChangedHandler;
				m_bodyScrollerSizeChangedRevoker.Disposable = Disposable.Create(() => bodyScroller.SizeChanged -= sizeChangedHandler);

				// Resolve during the next table measure now that the viewport is known (initial layout).
				InvalidateMeasure();
				RefreshFrozenColumns();
			}
		}
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

		var bodyHOffset = bodyScroller.HorizontalOffset;

		// Re-pin leading-frozen columns only when horizontal scroll moves.
		if (ShouldRefreshFrozenColumnsForScroll(this, bodyHOffset))
		{
			RefreshFrozenColumns();
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

		// The edited item is about to leave the control. Forced, because the data set is already gone
		// by the time a handler could react, so the close cannot be vetoed. No-focus teardown: this is
		// a dependency-property change callback, and moving focus from one re-enters the framework.
		if (IsEditing)
		{
			TerminateEditWithoutVisualRestore();
		}

		// The current cell belongs to the old data set. Left alone, CurrentItem keeps returning an item
		// that is not in the new source, BeginEdit fails with no diagnostic, and the discarded item
		// stays rooted by the tracker. WPF DataGrid likewise resets CurrentItem/CurrentCell here.
		//
		// Per-row begin-edit press state needs no reset: it is compared by item identity, so an entry
		// left over from the previous data set can never match an item from the new one.
		SetCurrentCell(null, null);

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
		AdoptItemsSource();
		RefreshRowsPipeline();
	}

	private void OnHeadersVisibilityPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		UpdateHeaderVisibility();
	}

	private void OnGridLinesVisibilityPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		ApplyGridLinesToHeader();
		RefreshGridLinesOnRealizedRows();
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

	private void ApplyGridLinesToHeader()
	{
		var visibility = GridLinesVisibility;

		if (m_headerRow is { } headerFE)
		{
			if (headerFE is Border headerBorder)
			{
				if (WantsHorizontalLines(visibility))
				{
					headerBorder.ClearValue(Border.BorderThicknessProperty);
				}
				else
				{
					headerBorder.BorderThickness = s_zeroThickness;
				}
			}
		}

		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		var wantVertical = WantsVerticalLines(visibility);
		var headerGridLineName = s_HeaderGridLineName;
		var headerCells = host.Children;
		var headerCellCount = headerCells.Count;
		for (var i = 0; i < headerCellCount; ++i)
		{
			if (headerCells[i] is Panel headerCell)
			{
				var children = headerCell.Children;
				var childCount = children.Count;
				for (var childIndex = 0; childIndex < childCount; ++childIndex)
				{
					if (children[childIndex] is Border border)
					{
						if (border.Name == headerGridLineName)
						{
							border.Visibility = wantVertical ? Visibility.Visible : Visibility.Collapsed;
						}
					}
				}
			}
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
		ForEachRealizedRow(row =>
		{
			row.RefreshGridLines();
		});
	}

	private void RefreshRowBackgroundsOnRealizedRows()
	{
		ForEachRealizedRow(row =>
		{
			row.RefreshRowBackground();
		});
	}

	private void AdoptItemsSource()
	{
		var itemsSource = ItemsSource;
		var tableViewSource = itemsSource as TableViewSource;

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
		}

		// Detach the previous source before adopting the new one. Swapping ItemsSource between two
		// TableViewSources leaves the old one alive and still subscribed to the app's collection, so
		// without this it keeps a back-pointer to this control and a later rebuild of that discarded
		// source would drive a TableView it no longer belongs to.
		if (m_activeSource is { } previouslyOwned && previouslyOwned != tableViewSource)
		{
			previouslyOwned.SetOwningTableView(null);
		}

		m_activeSource = tableViewSource;

		if (tableViewSource is not null)
		{
			var sourceImpl = tableViewSource;
			sourceImpl.SetOwningTableView(this);

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
			// ItemsRepeater has no identity short-circuit: re-assigning the same source tears down
			// every container and resets scroll. Guard so a theme-change or Loaded repump does not
			// blow away realized rows.
			if (!IsSameObject(repeater.ItemsSource, rowsSource))
			{
				repeater.ItemsSource = rowsSource;
			}

			UpdateEmptyStateCollectionChangedSubscription();
			UpdateEmptyState();

			// Re-point selection at the new source. SelectionModel::Source clears unconditionally, so a
			// swap always drops the selection; then drain anything requested before a source existed.
			ResolveSelectionAfterSourceChange();
		}
		// else: OnApplyTemplate hasn't run yet; the repeater will be sourced from there.
	}

	private void OnTableViewSourceProjectionChanged()
	{
		// A shaping verb swapped the projected shape after we bound, so the cached view and row
		// metadata describe the previous projection. Re-read them and re-drive the rows.
		if (IsEditing)
		{
			// The edited item may not exist in the new projection. Forced, for the same reason as an
			// ItemsSource swap: the shape is already gone by the time a handler could veto it.
			TerminateEditWithoutVisualRestore();
		}

		RefreshRowsPipeline();
	}

	private void OnTableViewSourceShapingChanged(bool reorderOnly)
	{
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
			peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
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

		UpdateEmptyStateCollectionChangedSubscription();
		UpdateEmptyState();
	}

	private void UpdateEmptyStateCollectionChangedSubscription()
	{
		// Rewire count-change tracking; auto_revoke drops the prior source subscription.
		m_emptyStateCollectionChangedRevoker.Disposable = null;
		if (EmptyTemplate != null)
		{
			// Count changes matter only when an empty template can be displayed.
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
							strongThis.OnEmptyStateItemsSourceCollectionChanged(s, a);
						}
					};
					view.CollectionChanged += handler;
					m_emptyStateCollectionChangedRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= handler);
				}
			}
		}
	}

	private void OnEmptyStateItemsSourceCollectionChanged(object? sender, object? args)
	{
		UpdateEmptyState();
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

	internal double GetHeaderFontSize()
	{
		var cache = GetTableViewResourceCache(this);
		if (cache.font.hasHeaderFontSize)
		{
			return cache.font.headerFontSize;
		}

		const double fallback = 14.0;
		if (LookupElementResource(this, "TableViewHeaderFontSize") is { } raw)
		{
			cache.font.headerFontSize = raw is double rawValue ? rawValue : fallback;
			cache.font.hasHeaderFontSize = true;
			return cache.font.headerFontSize;
		}
		cache.font.headerFontSize = fallback;
		cache.font.hasHeaderFontSize = true;
		return cache.font.headerFontSize;
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
		if (column is null || !IsEditing)
		{
			return;
		}

		if (m_currentEditColumn == column)
		{
			TerminateEditWithoutVisualRestore();
		}
	}

	private void OnIsReadOnlyPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
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
		if (args.Element is TableViewRow row)
		{
			var rowImpl = row;
			rowImpl.SetOwningTableViewInternal(this);
			rowImpl.RefreshGridLines();
			rowImpl.RefreshRowBackground();
			RefreshRowSelectionState(row);
			InvalidateMeasure();
		}
		else if (args.Element is TableViewGroupHeader header)
		{
			PrepareGroupHeaderElement(header, args.Index);
			InvalidateMeasure();
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
					// Beginning window, where app code (a BeginningEdit handler, ITableViewEditableItem
					// .BeginEdit) mutated the collection and recycled the row underneath us. The row
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
			// Release app-supplied tooltip content rather than pinning it in the recycle pool.
			rowImpl.ReleaseCellToolTips();
			rowImpl.SetOwningTableViewInternal(null);
			InvalidateMeasure();
		}
		else if (args.Element is TableViewGroupHeader header)
		{
			ClearGroupHeaderElement(header);
			InvalidateMeasure();
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
			}
			return;
		}

		// Realized rows keep their element but get a new index, so banding parity must refresh.
		if (RowBackground != null || AlternatingRowBackground != null)
		{
			row.RefreshRowBackground();
		}

		// ...and so must selected chrome. The element keeps its item here (only its index moved), so
		// this normally re-derives the same answer - it is the cheap guarantee that a row whose index
		// shifted under an insert cannot end up disagreeing with the model.
		RefreshRowSelectionState(row);
	}

	// One header-text extraction per column, shared by the header cell's automation name and the
	// gripper's OwnerName.
	private static string GetColumnHeaderText(TableViewColumn column)
	{
		if (column.Header is { } header)
		{
			if (SharedHelpers.IsStringable(header))
			{
				return SharedHelpers.StringableToString(header);
			}
			// TODO Uno: IPropertyValue projection - a boxed PropertyType::String projects as System.String.
			// Original C++: header.try_as<IPropertyValue>(); propValue && propValue.Type() == PropertyType::String
			if (header is string propValue)
			{
				return propValue;
			}
		}

		return "";
	}

	private void ReleaseHeaderToolTips(Panel? host)
	{
		if (host is null)
		{
			return;
		}

		// Snapshot: Closed runs app code that can re-enter RebuildHeaders and clear this collection.
		var children = host.Children;
		List<FrameworkElement> cells = new(children.Count);
		for (var i = 0; i < children.Count; ++i)
		{
			if (children[i] is FrameworkElement headerCell)
			{
				cells.Add(headerCell);
			}
		}

		foreach (var headerCell in cells)
		{
			TableViewDetails.ClearOwnedToolTip(headerCell);
		}
	}

	internal void RebuildHeaders()
	{
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		// Clearing the host under a live popup tears its child down reentrantly.
		ReleaseHeaderToolTips(host);

		host.Children.Clear();

		// Cache theme-resource padding once per header rebuild; values are stable for the pass.
		var cachedHeaderCellPadding = GetDensityHeaderCellPadding();
		// Always cache the header grid-line brush at rebuild time; visibility toggles do not reassign it later.
		var wantVerticalHeaderLines = WantsVerticalLines(GridLinesVisibility);
		var cachedHeaderGridLineBrush = GetGridLineBrush();
		// Header cells share the density row min-height so the header band matches the body rows.
		var cachedRowMinHeight = GetDensityRowMinHeight();
		var cachedHeaderFontSize = GetHeaderFontSize();
		Brush cachedHeaderCellFill = new SolidColorBrush(Colors.Transparent);
		// unbox_value_or, not unbox_value: the key is app-overridable and a non-double would throw out
		// of RebuildHeaders; a non-positive or non-finite override would flow straight into Width().
		var cachedResizeGripperWidth =
			LookupElementResource(this, s_ResizeGripperWidthKey) is double resizeGripperWidth ? resizeGripperWidth : c_resizeGripperWidthFallback;
		if (!double.IsFinite(cachedResizeGripperWidth) || cachedResizeGripperWidth <= 0.0)
		{
			cachedResizeGripperWidth = c_resizeGripperWidthFallback;
		}
		var canUserSortColumns = CanUserSortColumns;

		// Logical-end (trailing) edge alignment must mirror under RTL. The header cell's subtree does
		// not observe the ambient FlowDirection auto-flip, so read the control's FlowDirection and swap
		// explicitly. This is a build-time stamp, kept current because a runtime FlowDirection flip
		// rebuilds the headers.
		var isRightToLeft = FlowDirection == FlowDirection.RightToLeft;
		var logicalEndAlignment = isRightToLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right;

		if (Columns is { } columns)
		{
			foreach (var column in columns)
			{
				// Skip entries this TableView rejected so a half-owned column cannot render here while
				// its callbacks still route to another owner.
				if (column is null || column.GetOwningTableView() != this)
				{
					continue;
				}

				// Header cell root.
				Grid headerCell = new();
				headerCell.Visibility = column.Visibility;
				// The header cell, not the gripper, is the keyboard target: column commands live here,
				// and a bare focusable Grid is unnamed and Raw to a screen reader. Only a tab stop when
				// focusing it can actually do something -- otherwise every column costs a Tab press for
				// nothing. Same condition that decides whether a gripper is created at all.
				var headerIsResizable = CanUserResizeColumns && column.CanResize;
				headerCell.IsTabStop = headerIsResizable;
				headerCell.UseSystemFocusVisuals = headerIsResizable;
				var headerText = GetColumnHeaderText(column);
				if (!string.IsNullOrEmpty(headerText))
				{
					AutomationProperties.SetName(headerCell, headerText);
				}
				AutomationProperties.SetAccessibilityView(headerCell, AccessibilityView.Content);
				// Match the body row min-height so the header band and rows render at the same height.
				headerCell.MinHeight = cachedRowMinHeight;
				// Without a fill the padding takes no pointer input, killing the tooltip and
				// click-to-sort there.
				headerCell.Background = cachedHeaderCellFill;

				// No Width binding: TableViewCellsPanel arranges header cells at the column's ActualWidth;
				// an explicit Width would defeat the panel's unconstrained Auto measured-width measurement.

				ContentPresenter content = new();
				content.Content = column.Header;
				if (column.HeaderTemplateSelector is { } headerTemplateSelector)
				{
					content.ContentTemplateSelector = headerTemplateSelector;
				}
				else if (column.HeaderTemplate is { } headerTemplate)
				{
					content.ContentTemplate = headerTemplate;
				}
				// Consume TableViewHeaderCellPadding from theme resources (cached once per rebuild).
				content.Padding = cachedHeaderCellPadding;
				content.HorizontalAlignment = HorizontalAlignment.Stretch;
				content.VerticalAlignment = VerticalAlignment.Center;
				// Column-header text: theme font size, SemiBold to stand out from cells (templates override).
				content.FontSize = cachedHeaderFontSize;
				content.FontWeight = FontWeights.SemiBold;
				headerCell.Children.Add(content);

				// Resolve from TableView so header grid lines track theme.
				{
					Border headerGridLine = new();
					headerGridLine.Name = s_HeaderGridLineName;
					headerGridLine.Width = 1;
					headerGridLine.HorizontalAlignment = logicalEndAlignment;
					headerGridLine.IsHitTestVisible = false;
					headerGridLine.Visibility = wantVerticalHeaderLines ? Visibility.Visible : Visibility.Collapsed;
					headerGridLine.Background = cachedHeaderGridLineBrush;
					headerCell.Children.Add(headerGridLine);
				}

				// Tag header cells so frozen-column refresh can map them back to columns.
				headerCell.Tag = column;

				// No HelpText: the header's peer is virtual and composes the text itself.
				TableViewDetails.ApplyHeaderToolTip(headerCell, column.HeaderToolTip);

				// Sort affordance. Gated on both the control-wide and the per-column opt-in, so an
				// opted-out column carries no chevron and no click handler at all.
				if (canUserSortColumns && column.CanSort)
				{
					// The header cell is a Grid, and a Grid with a null Background is not hit-test
					// visible in its empty regions. The header content presenter and the chevron host
					// below are both effectively non-hit-testable in their padding, so without an
					// explicit brush a tap that misses the header glyphs never reaches the Tapped
					// handler and the column never sorts. A transparent fill makes the WHOLE cell the
					// click target, matching the group-header band and WPF DataGrid column headers.
					headerCell.Background = new SolidColorBrush(Colors.Transparent);

					// Hosted in its own panel so the chevron sits on the logical trailing edge without
					// competing with the header content's Stretch alignment.
					StackPanel indicatorHost = new();
					indicatorHost.Orientation = Orientation.Horizontal;
					indicatorHost.HorizontalAlignment = logicalEndAlignment;
					indicatorHost.VerticalAlignment = VerticalAlignment.Center;
					// The chevron is decoration on top of a clickable header: letting it take the hit
					// would create a dead spot in the middle of the click target.
					indicatorHost.IsHitTestVisible = false;
					AppendSortIndicatorVisual(indicatorHost, column);
					headerCell.Children.Add(indicatorHost);

					// Weak: the handler is owned by a visual the control also owns, so a strong
					// capture would keep the TableView alive through its own header.
					WeakReference<TableView> weakThis = new(this);
					var sortColumn = column;
					headerCell.Tapped += (object _, TappedRoutedEventArgs args) =>
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

				// Last, so it wins the hit test on the edge it shares with the grid line and the sort
				// affordance. The gripper marks the press handled, which also keeps a resize drag from
				// reaching the header's Tapped handler and sorting the column.
				if (headerIsResizable)
				{
					AppendResizeGripperVisual(headerCell, column, cachedResizeGripperWidth, headerText, logicalEndAlignment);
				}

				host.Children.Add(headerCell);
			}
		}

		// Pin (or refresh) leading-frozen header cells at the current scroll offset.
		RefreshFrozenColumns();
		ApplyGridLinesToHeader();
	}

	// Builds the chevron for one header. The indicator is a display-only primitive: it owns no sort
	// policy, so the control sets Direction and nothing else.
	private void AppendSortIndicatorVisual(Panel? host, TableViewColumn? column)
	{
		if (host is null || column is null)
		{
			return;
		}

		SortIndicator indicator = new();
		indicator.Name = s_SortIndicatorName;
		indicator.VerticalAlignment = VerticalAlignment.Center;
		indicator.Direction = ToSortIndicatorDirection(column.SortDirection);
		// The header cell already reports the sort state through its automation peer's help text;
		// surfacing the chevron separately would make AT announce the same thing twice.
		AutomationProperties.SetAccessibilityView(indicator, AccessibilityView.Raw);
		host.Children.Add(indicator);
	}

	private static SortIndicatorDirection ToSortIndicatorDirection(SortDirection direction)
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

	private static SortIndicator? FindSortIndicator(Panel? root)
	{
		if (root is null)
		{
			return null;
		}

		// The chevron lives one level down inside its own host StackPanel today, but recurse so a
		// future template tweak that nests it deeper does not silently break the refresh.
		foreach (var child in root.Children)
		{
			if (child is SortIndicator indicator)
			{
				return indicator;
			}
			if (child is Panel childPanel)
			{
				if (FindSortIndicator(childPanel) is { } found)
				{
					return found;
				}
			}
		}

		return null;
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
			var headerCell = child as Panel;
			if (headerCell is null)
			{
				continue;
			}

			var column = headerCell.Tag as TableViewColumn;
			if (column is null)
			{
				continue;
			}

			// A child walk by type rather than FindName: the indicator is code-created into a nested
			// host panel, so its Name was never registered in a namescope and FindName returns null --
			// which left a programmatic sort (no header rebuild) with a stale chevron.
			if (FindSortIndicator(headerCell) is { } indicator)
			{
				indicator.Direction = ToSortIndicatorDirection(column.SortDirection);
			}
		}
	}

	private void OnCanUserSortColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// The gate turning off must also drop any sort it was responsible for; leaving the rows in a
		// sorted order with no affordance to change it would strand the user.
		if (!CanUserSortColumns)
		{
			ClearSort();
		}

		// The chevron and the click handler are stamped at header-build time.
		QueueRebuildHeaders();
	}

	internal void OnColumnCanSortChanged(TableViewColumn? column)
	{
		// A column that just opted out must not keep an active sort applied to it.
		if (column is not null && !column.CanSort && column.SortDirection != SortDirection.None)
		{
			SortByColumn(column, SortDirection.None);
		}

		// The chevron and the click handler are stamped at header-build time.
		QueueRebuildHeaders();
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

	private void OnTableViewUnloaded()
	{
		if (m_pendingFocusLayoutToken.Disposable is not null)
		{
			m_pendingFocusLayoutToken.Disposable = null;
		}

		if (m_pendingGroupFocusLayoutToken.Disposable is not null)
		{
			m_pendingGroupFocusLayoutToken.Disposable = null;
		}
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;

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
		//
		// Order matters. The auto_revoke revoker holds only a weak_ref to ThemeSettings and its revoke()
		// is noexcept: if remove_Changed throws (which it does at app shutdown, where this Unloaded runs
		// from DispatcherQueueController::ShutdownQueue and ThemeSettings' underlying window feature is
		// already detaching, surfacing RPC_E_WRONG_THREAD), the exception escapes the noexcept boundary
		// and terminates the process -- a try/catch here can never intercept it. So we release our strong
		// reference FIRST. If it was the last one the object dies, the revoker's weak_ref goes stale and
		// revoke() becomes a no-op (no ABI call, no throw); if the framework still holds the object it is
		// alive and remove_Changed succeeds normally. Either way remove_Changed is never called against a
		// half-torn-down feature.
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

	internal void AppendResizeGripperVisual(
		Grid headerCell,
		TableViewColumn column,
		double gripperWidth,
		string headerText,
		HorizontalAlignment logicalEndAlignment)
	{
		// A real gripper in the tree, so the pointer has something to hit before any drag starts.
		WeakReference<TableView> weakThis = new(this);
		ResizeGripper gripperVisual = new();
		// Direction of travel, opposite of WPF's GridSplitter, so state it rather than lean on the default.
		gripperVisual.DragOrientation = Orientation.Horizontal;
		// Pointer affordance only here: the header cell owns keyboard focus, and one tab stop per
		// column would sit between the user and the data.
		gripperVisual.IsTabStop = false;
		// Same explicit logical-end alignment the grid line and the sort affordance use: the header
		// cell's subtree does not observe the ambient FlowDirection auto-flip, so the gripper has to be
		// told which edge is trailing or it lands opposite the grid line under RTL.
		gripperVisual.HorizontalAlignment = logicalEndAlignment;
		gripperVisual.Width = gripperWidth;
		// The peer names itself from OwnerName, so N grippers in one header band are distinguishable.
		if (!string.IsNullOrEmpty(headerText))
		{
			gripperVisual.OwnerName = headerText;
		}

		// Deltas must be measured against the TableView: its frame does not move while a column
		// resizes, and unlike the XamlRoot it is below any scale an app applies above the control.
		gripperVisual.ManipulationContainer = this;

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
					state.startWidth = col.Width;
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

				// std::max mirrors TableViewColumn::UpdateActualWidth, so a column whose MaxWidth is below
				// its MinWidth cannot make Width and ActualWidth disagree.
				var lo = (double.IsFinite(col.MinWidth) && col.MinWidth >= 0.0) ? col.MinWidth : 0.0;
				var hi = (double.IsFinite(col.MaxWidth) && col.MaxWidth >= 0.0)
					? StdMath.Max(lo, col.MaxWidth)
					: double.PositiveInfinity;

				var next = StdMath.Clamp(state.startValue + vargs.TotalDelta, lo, hi);

				// Pinned at a bound the pointer keeps moving but the width does not: writing anyway would
				// re-run measure and every cell panel on each move.
				var current = col.Width;
				if (current.GridUnitType == GridUnitType.Pixel && Math.Abs(current.Value - next) < 0.0001)
				{
					return;
				}

				col.Width = GridLengthHelper.FromPixels(next);
				state.didWrite = true;
			};

		// Canceled means the gesture was torn down rather than released (Escape, palm rejection, the
		// header rebuilt mid-drag): revert. A clean release announces the new width instead.
		WeakReference<Grid> weakHeaderCell = new(headerCell);
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
					// Only when a write actually happened, so a press that never moved cannot pin an
					// Auto/Star column.
					if (state.didWrite && col is not null)
					{
						col.Width = state.startWidth;
					}
					return;
				}

				// Attributed to the header cell: it is the focusable element that represents the column,
				// and the one assistive technology is already on during a keyboard resize.
				if (strongThis is not null && col is not null && state.didDelta)
				{
					strongThis.AnnounceColumnWidth(weakHeaderCell.TryGetTarget(out var announcer) ? announcer : null, col);
				}
			};
		headerCell.Children.Add(gripperVisual);
	}

	// Escape is the only host-driven cancel left: the gripper ends its own gesture on release, on a
	// canceled contact and on unload. Idempotent - EndDrag no-ops when no drag is in flight.
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

	// The header cell is tagged with its column; the gripper is one of its children.
	// The gripper is one of the header cell's children; the caller already has the cell.
	internal ResizeGripper? FindResizeGripperInCell(FrameworkElement? headerCell)
	{
		var cell = headerCell as Panel;
		if (cell is null)
		{
			return null;
		}

		var cellChildren = cell.Children;
		var cellCount = cellChildren.Count;
		for (var j = 0; j < cellCount; ++j)
		{
			if (cellChildren[j] is ResizeGripper gripper)
			{
				return gripper;
			}
		}

		return null;
	}

	private void OnCanUserResizeColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.OldValue == args.NewValue)
		{
			return;
		}

		RebuildHeaders();
	}
}
