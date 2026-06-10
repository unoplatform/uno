// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference xcpcore.cpp (CCoreServices requested-theme-for-subtree slot), commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;

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
