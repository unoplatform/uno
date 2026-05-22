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
	public void When_Owner_Theme_None_Returns_Nearest_Themed_Ancestor()
	{
		var grandparent = new Border();
		var parent = new Border();
		var owner = new Border();
		SetInheritanceParent(parent, grandparent);
		SetInheritanceParent(owner, parent);

		SetStoreTheme(grandparent, Theme.Light);
		SetStoreTheme(parent, Theme.Dark); // nearest themed ancestor wins over grandparent

		Assert.AreEqual(Theme.Dark, ThemeResolution.ResolveOwnerTheme(owner));
	}

	[TestMethod]
	public void When_Only_Grandparent_Themed_Walks_Past_Untouched_Parent()
	{
		var grandparent = new Border();
		var parent = new Border();
		var owner = new Border();
		SetInheritanceParent(parent, grandparent);
		SetInheritanceParent(owner, parent);

		SetStoreTheme(grandparent, Theme.Light); // parent and owner stay Theme.None

		Assert.AreEqual(Theme.Light, ThemeResolution.ResolveOwnerTheme(owner));
	}

	[TestMethod]
	public void When_NonFrameworkElement_Owner_Inherits_From_Parent()
	{
		// D1: non-UIElement DOs (e.g. brushes) now carry a theme via the store and
		// participate in the inheritance-parent walk.
		var parent = new Border();
		var brush = new SolidColorBrush(Colors.Red);
		SetInheritanceParent(brush, parent);
		SetStoreTheme(parent, Theme.Dark);

		Assert.AreEqual(Theme.Dark, ThemeResolution.ResolveOwnerTheme(brush));
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
}
