#nullable enable

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
/// Phase 1 (D1) note: this helper is added now so the per-object theme is queryable on every DO, but
/// it is intentionally <b>not</b> wired into {ThemeResource} resolution yet — that happens in Phase 3
/// (D3), where <see cref="DependencyObjectStore"/>.UpdateThemeReference computes the owner theme once
/// and threads it into the resolution leaf (architecture.md §6, Mechanism 1). Until then resolution
/// still uses the global active-theme stack, so behavior is unchanged.
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
		// MUX: depends.cpp:1023-1048 — a DO's theme is established at Enter from its (logical)
		// inheritance parent. We mirror the *result* of that establishment by walking the
		// inheritance-parent chain to the nearest DO that already carries a theme.
		for (var current = owner; current is not null; current = GetInheritanceParent(current))
		{
			var theme = GetStoreTheme(current);
			if (theme != Theme.None)
			{
				return theme;
			}
		}

		// MUX: framework.cpp:3953-3978 — ActualTheme falls back to the app/OS base theme
		// (FrameworkTheming::GetBaseTheme) when no per-object theme is set.
		// TODO (Phase 6 / D8): OR in the application's high-contrast bits once HC composition lands.
		return Theming.FromElementTheme(Application.Current?.ActualElementTheme ?? ElementTheme.Light);
	}

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
