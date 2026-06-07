#nullable enable

using Uno.Helpers.Theming;

// Port of WinUI's per-object effective-theme resolution.
//
// MUX References:
//   CDependencyObject::GetTheme            — CDependencyObject.h:1648 (field m_theme:5 at :1761)
//   CDependencyObject::EnterImpl (theme)   — depends.cpp:1023-1048 (inherits from the (logical)
//                                            inheritance parent at tree Enter)
//   GetInheritanceParentInternal(logical)  — framework.cpp:3097-3130
//   CFrameworkElement::ActualTheme         — framework.cpp:3953-3978 (falls back to the app/OS
//                                            base theme when m_theme == None)

namespace Microsoft.UI.Xaml;

/// <summary>
/// Resolves the effective <see cref="Theme"/> of a <see cref="DependencyObject"/> "owner", mirroring
/// how WinUI derives the theme used to resolve a {ThemeResource}: the owner's own per-object theme
/// (CDependencyObject::m_theme), inherited from its (logical) inheritance parent at tree Enter,
/// falling back to the application/OS base theme.
/// </summary>
/// <remarks>
/// This is the single source of truth for an owner's resolution theme: the resolution choke point
/// (<see cref="DependencyObjectStore"/>.UpdateThemeReference) computes it once and threads it into the
/// resolution leaf, so {ThemeResource} resolution keys on the owner's own theme rather than a
/// process-global ambient.
/// </remarks>
internal static class ThemeResolution
{

	/// <summary>
	/// Returns the effective <see cref="Theme"/> for the given <paramref name="owner"/>: the nearest
	/// established per-object theme found by walking up the inheritance-parent chain (starting at the
	/// owner itself), or the application's base theme when none is established. Never returns
	/// <see cref="Theme.None"/>.
	/// </summary>
	internal static Theme ResolveOwnerTheme(DependencyObject? owner)
	{
		// High contrast is an OS/app-global dimension OR-ed onto the base theme (effective theme =
		// base | highContrast; MUX FrameworkTheming::GetTheme, FrameworkTheming.cpp:123).
		var highContrast = GetApplicationHighContrastTheme();

		// MUX: depends.cpp:1023-1048 — a DO's theme is established at Enter from its (logical)
		// inheritance parent. We mirror the *result* of that establishment by walking the
		// inheritance-parent chain to the nearest DO that already carries a theme.
		for (var current = owner; current is not null; current = GetInheritanceParent(current))
		{
			var theme = GetStoreTheme(current);
			if (theme != Theme.None)
			{
				return Theming.GetBaseValue(theme) | highContrast;
			}
		}

		// MUX: framework.cpp:3953-3978 — ActualTheme falls back to the app/OS base theme
		// (FrameworkTheming::GetBaseTheme) when no per-object theme is set. Use Themes.Active
		// (ResourceDictionary.GetActiveBaseTheme), which is the SAME base theme the lazy-materialization
		// resolution leaf keys on (GetActiveTheme), so the two owner-less resolution paths never disagree.
		// In production Themes.Active and ActualElementTheme are both derived from Application.RequestedTheme
		// (Application.cs:229-252) and are therefore equal; they diverge only when the active theme is set
		// directly via ResourceDictionary.SetActiveTheme without round-tripping Application.RequestedTheme.
		return ResourceDictionary.GetActiveBaseTheme() | highContrast;
	}

	/// <summary>
	/// The application's current high-contrast theme bits (<see cref="Theme.HighContrast"/> when the OS
	/// high-contrast feature is active, otherwise <see cref="Theme.HighContrastNone"/>). High contrast is a
	/// global OS/app dimension in WinUI (read from FrameworkTheming, not from the per-object theme — MUX
	/// FrameworkTheming.cpp:123), so it is OR-ed onto every resolved owner theme. The high-contrast
	/// sub-dictionary itself is selected at the resolution leaf
	/// (<see cref="ResourceDictionary"/>.GetActiveThemeDictionary), which reads the same global state.
	/// </summary>
	private static Theme GetApplicationHighContrastTheme()
		=> SystemThemeHelper.IsHighContrast ? Theme.HighContrast : Theme.HighContrastNone;

	/// <summary>
	/// Gets the per-object theme stored on the object's <see cref="DependencyObjectStore"/>, or
	/// <see cref="Theme.None"/> if the object does not expose a store.
	/// </summary>
	private static Theme GetStoreTheme(DependencyObject owner)
		=> owner is IDependencyObjectStoreProvider provider ? provider.Store.GetTheme() : Theme.None;

	/// <summary>
	/// Gets the inheritance (DP-inheritance) parent of the object — Uno's analog of WinUI's
	/// GetInheritanceParentInternal (framework.cpp:3097-3130). Returns null at the root (the parent
	/// can be a non-DependencyObject, e.g. the visual-tree root).
	/// </summary>
	private static DependencyObject? GetInheritanceParent(DependencyObject owner)
		=> owner is IDependencyObjectStoreProvider provider ? provider.Store.Parent as DependencyObject : null;

}
