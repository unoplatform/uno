using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ThemeResolution
{
	[TestInitialize]
	public void Init() => UnitTestsApp.App.EnsureApplication();

	// Sets the per-object theme on the store (the field moved off UIElement onto
	// DependencyObjectStore in Phase 1 / D1).
	private static void SetStoreTheme(DependencyObject o, Theme theme)
		=> ((IDependencyObjectStoreProvider)o).Store.SetTheme(theme);

	// Links the DP-inheritance parent (what ResolveOwnerTheme walks) without a visual-tree attach.
	private static void SetInheritanceParent(DependencyObject child, DependencyObject parent)
		=> ((IDependencyObjectStoreProvider)child).Store.Parent = parent;

	// The application/OS base-theme fallback (last link of the chain).
	private static Theme AppFallback()
		=> Theming.FromElementTheme(Application.Current?.ActualElementTheme ?? ElementTheme.Light);

	[TestMethod]
	public void When_Owner_Has_Own_Theme_Returns_Own()
	{
		var owner = new Border();
		SetStoreTheme(owner, Theme.Dark);

		Assert.AreEqual(Theme.Dark, ThemeResolution.ResolveOwnerTheme(owner));
	}

	[TestMethod]
	public void When_Owner_Theme_Set_Overrides_Themed_Ancestor()
	{
		var parent = new Border();
		var owner = new Border();
		SetInheritanceParent(owner, parent);
		SetStoreTheme(parent, Theme.Light);
		SetStoreTheme(owner, Theme.Dark); // own theme wins over the ancestor

		Assert.AreEqual(Theme.Dark, ThemeResolution.ResolveOwnerTheme(owner));
	}

	[TestMethod]
	public void When_Owner_Theme_None_Returns_Ambient_Not_Ancestor()
	{
		// WinUI's rule exactly: SetThemeResourceBinding consults m_theme alone (Theming.cpp:368) —
		// the per-object theme is established at tree Enter, so there is deliberately NO
		// resolution-time ancestor walk; an owner with no established theme resolves against the
		// ambient base theme even when an inheritance ancestor carries a different theme.
		var original = ResourceDictionary.GetActiveTheme();
		try
		{
			ResourceDictionary.SetActiveTheme("Light");

			var parent = new Border();
			var owner = new Border();
			SetInheritanceParent(owner, parent);

			SetStoreTheme(parent, Theme.Dark); // owner stays Theme.None

			Assert.AreEqual(Theme.Light, Theming.GetBaseValue(ThemeResolution.ResolveOwnerTheme(owner)));
		}
		finally
		{
			ResourceDictionary.SetActiveTheme(original);
		}
	}

	[TestMethod]
	public void When_NonFrameworkElement_Owner_Has_Store_Theme_Returns_Own()
	{
		// D1: non-UIElement DOs (e.g. brushes) carry a per-object theme via the store, established
		// at tree Enter; resolution consults that theme alone (no resolution-time parent walk).
		var original = ResourceDictionary.GetActiveTheme();
		try
		{
			ResourceDictionary.SetActiveTheme("Light");

			var parent = new Border();
			var brush = new SolidColorBrush(Colors.Red);
			SetInheritanceParent(brush, parent);
			SetStoreTheme(brush, Theme.Dark);

			Assert.AreEqual(Theme.Dark, Theming.GetBaseValue(ThemeResolution.ResolveOwnerTheme(brush)));

			// A parent's theme does not leak into a theme-less owner at resolution time.
			var themelessBrush = new SolidColorBrush(Colors.Red);
			SetInheritanceParent(themelessBrush, parent);
			SetStoreTheme(parent, Theme.Dark);

			Assert.AreEqual(Theme.Light, Theming.GetBaseValue(ThemeResolution.ResolveOwnerTheme(themelessBrush)));
		}
		finally
		{
			ResourceDictionary.SetActiveTheme(original);
		}
	}

	[TestMethod]
	public void When_No_Theme_Anywhere_Returns_App_Fallback()
	{
		var owner = new Border(); // no own theme, no parent

		Assert.AreEqual(AppFallback(), ThemeResolution.ResolveOwnerTheme(owner));
		Assert.AreNotEqual(Theme.None, ThemeResolution.ResolveOwnerTheme(owner));
	}

	[TestMethod]
	public void When_Owner_Null_Returns_App_Fallback()
		=> Assert.AreEqual(AppFallback(), ThemeResolution.ResolveOwnerTheme(null));

	// The owner-less fallback must track the ambient base theme — the requested-theme-for-subtree
	// slot when one is scoped, else the app base (FrameworkTheming.GetBaseTheme) — which is the SAME
	// base the lazy-materialization resolution leaf keys on (GetActiveTheme), so the two owner-less
	// {ThemeResource} resolution paths never disagree. (Themes.Active is only the native/no-core
	// mirror; on the enhanced-lifecycle flavor this test host runs, the slot/FrameworkTheming is
	// authoritative — EnsureActiveThemeDictionary, Resources.cpp:764-768.)
	[TestMethod]
	public void When_Owner_Null_Follows_Active_Theme()
	{
		using (Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(Theme.Dark))
		{
			Assert.AreEqual(Theme.Dark, Theming.GetBaseValue(ThemeResolution.ResolveOwnerTheme(null)));
		}

		using (Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(Theme.Light))
		{
			Assert.AreEqual(Theme.Light, Theming.GetBaseValue(ThemeResolution.ResolveOwnerTheme(null)));
		}
	}
}
