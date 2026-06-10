// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference xcpcore.cpp (CCoreServices theming: NotifyThemeChange, theme-changed listeners,
// requested-theme-for-subtree slot), commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
#if UNO_HAS_ENHANCED_LIFECYCLE
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using Uno.UI.Xaml.Media;
#endif

namespace Uno.UI.Xaml.Core;

internal partial class CoreServices
{
	// MUX Reference: CCoreServices::m_requestedThemeForSubTree — the single core-level ambient
	// the resolution leaf reads to select a theme sub-dictionary. It is a single value with
	// scoped save/set/restore (recursion forms the implicit stack) written from a DO's m_theme
	// at exactly three places: CDependencyObject::NotifyThemeChanged (Theming.cpp:137-149),
	// CDependencyObject::SetThemeResourceBinding (Theming.cpp:364-379), and
	// CFrameworkElement::NotifyThemeChangedForInheritedProperties (framework.cpp:3441-3487) —
	// plus the keyed-lookup helper CCoreServices::LookupThemeResource (xcpcore.cpp:2371-2394).
	// Element-level theming only runs on enhanced-lifecycle targets; the slot itself compiles
	// everywhere and simply stays None elsewhere.
	private Theme _requestedThemeForSubTree;

	internal Theme GetRequestedThemeForSubTree() => _requestedThemeForSubTree;

	// MUX Reference: CCoreServices::SetRequestedThemeForSubTree — xcpcore.cpp:8128-8133
	internal void SetRequestedThemeForSubTree(Theme requestedTheme)
	{
		_requestedThemeForSubTree = Microsoft.UI.Xaml.Theming.GetBaseValue(requestedTheme);

		// TODO Uno: WinUI forwards the sub-tree theme to the walk cache here
		// (m_themeWalkResourceCache->SetSubTreeTheme, xcpcore.cpp:8132). Uno's
		// ThemeWalkResourceCache takes the theme as a lookup parameter instead of an
		// ambient; couple it here once the cache reads the slot (Phase 5).
	}

	// MUX Reference: CCoreServices::IsThemeRequestedForSubTree — xcpcore.cpp:8136-8138
	internal bool IsThemeRequestedForSubTree() => Microsoft.UI.Xaml.Theming.GetBaseValue(_requestedThemeForSubTree) != Theme.None;

	/// <summary>
	/// Whether a theme switch is currently being processed. Analog of
	/// CCoreServices::m_fIsSwitchingTheme (set by CFrameworkElement::OnRequestedThemeChanged,
	/// framework.cpp:3540-3545, and by CCoreServices::NotifyThemeChange, xcpcore.cpp:8041-8042).
	/// </summary>
	internal bool IsSwitchingTheme { get; set; }

	/// <summary>
	/// Whether a base/HC theme change has ever been applied. Analog of
	/// CCoreServices::m_hasThemeEverChanged (set by CCoreServices::NotifyThemeChange,
	/// xcpcore.cpp:8043; consumed by the popup open path, popup.cpp).
	/// </summary>
	internal bool HasThemeEverChanged { get; private set; }

#if UNO_HAS_ENHANCED_LIFECYCLE
	// MUX Reference: CCoreServices::m_elementsWithThemeChangedListener — corep.h:2320,
	//   containers::vector_map<CFrameworkElement*, unsigned int>
	// Elements with at least one ActualThemeChanged subscription, ref-counted per subscription, so
	// default (system/app) theme changes can be raised on elements outside the live tree (which the
	// root walks cannot reach). Uno: a ConditionalWeakTable instead of raw pointers — WinUI removes
	// entries from the CDependencyObject destructor, which has no deterministic C# equivalent, so the
	// table must not root the elements.
	private readonly ConditionalWeakTable<FrameworkElement, ThemeChangedListenerCount> _elementsWithThemeChangedListener = new();

	private sealed class ThemeChangedListenerCount
	{
		public uint Count;
	}

	// MUX Reference: CCoreServices::AddThemeChangedListener — xcpcore.cpp:11365-11377
	internal void AddThemeChangedListener(FrameworkElement fe)
	{
		if (_elementsWithThemeChangedListener.TryGetValue(fe, out var refCount))
		{
			refCount.Count++;
		}
		else
		{
			_elementsWithThemeChangedListener.Add(fe, new ThemeChangedListenerCount { Count = 1 });
		}
	}

	// MUX Reference: CCoreServices::RemoveThemeChangedListener — xcpcore.cpp:11379-11400
	internal void RemoveThemeChangedListener(FrameworkElement fe)
	{
		if (!_elementsWithThemeChangedListener.TryGetValue(fe, out var refCount))
		{
			// C++ ASSERT(FALSE)s here; in C# unsubscribing a handler that was never subscribed is
			// legal on any event, so tolerate the unbalanced remove.
			return;
		}

		var count = --refCount.Count;
		if (count == 0)
		{
			_elementsWithThemeChangedListener.Remove(fe);
		}
	}
#endif

#if UNO_HAS_ENHANCED_LIFECYCLE
	//------------------------------------------------------------------------
	//
	//  Method:   NotifyThemeChange
	//
	//  Synopsis:
	//      Notifies the entire visual tree that the theme has changed.
	//
	//------------------------------------------------------------------------
	// MUX Reference: CCoreServices::NotifyThemeChange — xcpcore.cpp:8006-8118. Invoked by
	// FrameworkTheming's notify callback (see CoreServices.Theming property).
	internal void NotifyThemeChange()
	{
		using var endOnExit = ThemeWalkResourceCache.BeginCachingThemeResources();

		// TODO Uno: UpdateColorAndBrushResources (xcpcore.cpp:8017-8026) creates/updates the
		// SystemColor*/SystemAccentColor color and brush resources in the global theme resources from
		// FrameworkTheming's ColorAndBrushResourceInfo list. Uno's system palette is provided by the
		// generated Uno.Themes/Fluent resources instead, so there is no first-create short-circuit.
		//   bool wasFirstCreateForResources = false;
		//   IFC_RETURN(UpdateColorAndBrushResources(&wasFirstCreateForResources));
		//   // We skip the rest of the theme notification if we just created
		//   // our color and brush resources
		//   if (wasFirstCreateForResources)
		//   {
		//       return S_OK;
		//   }

		// MUX: ResetThemedBrushes() (xcpcore.cpp:8028) — "Ensure that the default text and selection
		// brushes get invalidated." Uno's analog of the default-text-brush cache is DefaultBrushes.
		DefaultBrushes.ResetDefaultThemeBrushes();

		// TODO Uno: m_pTextCore->ClearDefaultTextFormatting() (xcpcore.cpp:8030-8033) — Uno has no
		// process-wide default TextFormatting cache; the default text foreground re-resolves through
		// DefaultBrushes above and the inherited-foreground freeze walk below.

		// TODO Uno: IPALResourceManager::NotifyThemeChanged (xcpcore.cpp:8035-8037) — theme-qualified
		// (ms-resource / .theme-dark) asset resolution has no Uno analog.

		// If already switching theme, then we don't need to re-notify of theme change
		if (IsSwitchingTheme)
		{
			return;
		}

		// Uno-specific app-level coherence (kept from Application.InternalRequestedTheme): mirror the
		// resolved base theme onto ResourceDictionary's Themes.Active and CoreApplication before any
		// resolution below. Themes.Active is the owner-less {ThemeResource} fallback until it is fully
		// retired in favor of FrameworkTheming.GetBaseTheme().
		if (Application.Current is not null)
		{
			Application.UpdateRequestedThemesForResources();
		}

		// MUX (xcpcore.cpp:8045-8048):
		//   if (m_pMainVisualTree)
		//   {
		//       IFC_RETURN(m_pMainVisualTree->GetRootVisual()->SetBackgroundColor(m_spTheming->GetRootVisualBackground()));
		//   }
		// Uno: every content root's IRootElement (kept from Application.UpdateRootElementBackground,
		// covering secondary windows/islands the way WinUI's per-CCoreServices root visual does).
		foreach (var contentRoot in ContentRootCoordinator.ContentRoots)
		{
			if (contentRoot.VisualTree?.RootElement is IRootElement rootElement)
			{
				rootElement.SetBackgroundColor(ThemingHelper.GetRootVisualBackground());
			}
		}

		IsSwitchingTheme = true;
		try
		{
			HasThemeEverChanged = true;

			var theme = Theming.GetTheme();

			// Uno-specific (kept from Application.OnResourcesChanged): when the theme walk re-resolves
			// {ThemeResource} values it sets them with Local precedence; if an Animation value was in
			// effect, the new Local value must not defeat it.
			ModifiedValue.SuppressLocalCanDefeatAnimations();
			try
			{
				// Notify global theme resources since they are not part of the tree.
				// MUX (xcpcore.cpp:8051-8056): pGlobalThemeResourcesNoRef->NotifyThemeChanged(theme, fForceRefresh=true).
				// Uno: the master system dictionary's walk lives in ResourceDictionary.UpdateThemeBindings
				// (the CResourceDictionary::NotifyThemeChangedCore analog).
				ResourceResolver.UpdateSystemThemeBindings(ResourceUpdateReason.ThemeResource);

				// Notify app resources since they are not part of the tree.
				// MUX (xcpcore.cpp:8058-8063): pAppResourcesNoRef->NotifyThemeChanged(theme, fForceRefresh=true).
				Application.Current?.Resources?.UpdateThemeBindings(ResourceUpdateReason.ThemeResource);

				// Notify open popups of theme change.
				// MUX (xcpcore.cpp:8065-8070): GetMainPopupRoot()->NotifyThemeChanged(theme, fForceRefresh=true).
				MainPopupRoot?.NotifyThemeChanged(theme, forceRefresh: true);

				// MUX (xcpcore.cpp:8072-8089): auto root = getVisualRoot(); — the public root visual.
				var root = VisualRoot;
				if (root is not null)
				{
					// Although this is the root, it can inherit default property values
					// (specifically, Text foreground). Freeze this inheritance, to pick up
					// values corresponding to the new theme, instead of the default values.
					if (root is FrameworkElement rootAsFE)
					{
						rootAsFE.NotifyThemeChangedForInheritedProperties(
							theme,
							freeze: true /* freezeInheritedPropertiesAtSubTreeRoot */);
					}

					// Notify visual tree root of theme change.
					root.NotifyThemeChanged(theme, forceRefresh: true);
				}

				// MUX (xcpcore.cpp:8091-8098): the XamlIslandRootCollection walk. Uno hosts XAML islands
				// as additional content roots rather than under the main tree's island collection, so
				// walk each non-main content root's root element the same way.
				foreach (var contentRoot in ContentRootCoordinator.ContentRoots)
				{
					if (contentRoot.VisualTree is null || contentRoot.VisualTree == _mainVisualTree)
					{
						continue;
					}

					if (contentRoot.VisualTree.RootElement is { } islandRoot)
					{
						(islandRoot as FrameworkElement)?.NotifyThemeChangedForInheritedProperties(
							theme,
							freeze: true /* freezeInheritedPropertiesAtSubTreeRoot */);
						islandRoot.NotifyThemeChanged(theme, forceRefresh: true);
					}
				}

				// TODO Uno: theme-variant image reload (xcpcore.cpp:8100-8108) — per content root,
				// CImageReloadManager::ReloadImages(ResourceInvalidationReason::ThemeChanged) re-resolves
				// theme-qualified (.theme-dark/.theme-light) image assets; Uno has no image reload manager.

				// Notify registered theme change listeners.
				// MUX (xcpcore.cpp:8110-8114).
				foreach (var (element, _) in _elementsWithThemeChangedListener)
				{
					element.NotifyThemeChangedListeners(theme);
				}
			}
			finally
			{
				ModifiedValue.ContinueLocalCanDefeatAnimations();
			}
		}
		finally
		{
			// C++ uses a wil::scope_exit guard (resetSwitchingThemeAfterChange, xcpcore.cpp:8042).
			IsSwitchingTheme = false;
		}
	}
#else
	// Native (non-enhanced-lifecycle) targets keep their Application-driven theme flow
	// (Application.OnRequestedThemeChanged → OnResourcesChanged); the FrameworkTheming
	// notify callback is inert there.
	private void NotifyThemeChange()
	{
	}
#endif

	/// <summary>
	/// Scopes the requested-theme-for-subtree slot to <paramref name="theme"/> for the duration of
	/// a lookup — the save/set/restore pattern of CCoreServices::LookupThemeResource
	/// (xcpcore.cpp:2371-2394), used wherever WinUI resolves a keyed resource under a specific
	/// owner's theme outside a theme walk.
	/// </summary>
	internal RequestedThemeForSubTreeScope ScopeRequestedThemeForSubTree(Theme theme) => new(this, theme);

	internal readonly struct RequestedThemeForSubTreeScope : global::System.IDisposable
	{
		private readonly CoreServices _core;
		private readonly Theme _previous;
		private readonly bool _set;

		public RequestedThemeForSubTreeScope(CoreServices core, Theme theme)
		{
			_core = core;
			_previous = core.GetRequestedThemeForSubTree();
			_set = Microsoft.UI.Xaml.Theming.GetBaseValue(theme) != _previous;
			if (_set)
			{
				core.SetRequestedThemeForSubTree(theme);
			}
		}

		public void Dispose()
		{
			if (_set)
			{
				_core.SetRequestedThemeForSubTree(_previous);
			}
		}
	}
}
