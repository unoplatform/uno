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
/// This is the single source of truth for an owner's resolution theme: the resolution choke points
/// (e.g. <see cref="DependencyObjectStore"/>.UpdateThemeReference) compute it once and scope it onto
/// the core requested-theme-for-subtree slot, which the resolution leaf reads
/// (EnsureActiveThemeDictionary, Resources.cpp:764-768), so {ThemeResource} resolution keys on the
/// owner's own theme.
/// </remarks>
internal static class ThemeResolution
{

	/// <summary>
	/// Returns the effective <see cref="Theme"/> for the given <paramref name="owner"/>: the owner's
	/// own established per-object theme, or the ambient base theme (the requested-theme-for-subtree
	/// slot when one is scoped, else the application's base theme) when none is established. Never
	/// returns <see cref="Theme.None"/>.
	/// </summary>
	/// <remarks>
	/// This is WinUI's rule exactly: CDependencyObject::SetThemeResourceBinding consults
	/// <c>m_theme</c> alone (Theming.cpp:368) and the dictionary leaf falls back to the ambient
	/// (EnsureActiveThemeDictionary, Resources.cpp:764-768). There is deliberately no resolution-time
	/// ancestor walk — the per-object theme is established at tree Enter (depends.cpp:1044-1069), so
	/// walking at resolution would only mask Enter-coverage gaps (a transitional diagnostic proved
	/// the walk contributed nothing across the theming suite before it was removed).
	/// </remarks>
	internal static Theme ResolveOwnerTheme(DependencyObject? owner)
	{
		// High contrast is an OS/app-global dimension OR-ed onto the base theme (effective theme =
		// base | highContrast; MUX FrameworkTheming::GetTheme, FrameworkTheming.cpp:123).
		var highContrast = GetApplicationHighContrastTheme();

		var theme = owner is not null ? GetStoreTheme(owner) : Theme.None;
		if (theme != Theme.None)
		{
			return Theming.GetBaseValue(theme) | highContrast;
		}

		// MUX: framework.cpp:3969-3994 — ActualTheme falls back to the ambient base theme
		// (the requested-theme-for-subtree slot when scoped, else FrameworkTheming::GetBaseTheme)
		// when no per-object theme is set. GetActiveBaseTheme is the SAME base theme the
		// lazy-materialization resolution leaf keys on (GetActiveTheme), so the two owner-less
		// resolution paths never disagree.
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

}
