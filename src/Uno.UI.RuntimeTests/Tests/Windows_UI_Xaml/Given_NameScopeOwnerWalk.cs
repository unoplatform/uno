using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.NameScoping;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// The namescope owner as seen by a live tree: who owns the scope a name lands in, and which
/// elements count as members of it. The Enter walk is what threads the owner down, so these
/// pin the walk's end state rather than its internals.
/// </summary>
/// <remarks>
/// Owners are compared relatively on purpose: the test host embeds its content deep inside its own
/// templates, and a template root is a namescope owner too, so the nearest owner above a loaded
/// element is not the public root visual.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_NameScopeOwnerWalk
{
	[TestMethod]
	public async Task When_Tree_Is_Loaded_The_Public_Root_Owns_Its_Own_Scope()
	{
		var root = new Border();

		await UITestHelper.Load(root, x => x.IsLoaded);
		var publicRoot = VisualTree.GetForElement(root)!.PublicRootVisual!;

		Assert.IsTrue(publicRoot.IsStandardNameScopeOwner, "the public root visual must be a namescope owner");
		Assert.IsFalse(publicRoot.IsStandardNameScopeMember, "the public root visual is the one owner that is not a member");
		Assert.AreEqual(publicRoot, publicRoot.GetStandardNameScopeOwner());
	}

	[TestMethod]
	public async Task When_Descendant_Is_Loaded_It_Shares_Its_Root_Scope()
	{
		var leaf = new Border();
		var root = new Border { Child = new Border { Child = leaf } };

		await UITestHelper.Load(root, x => x.IsLoaded);

		var owner = root.GetStandardNameScopeOwner();
		Assert.IsNotNull(owner, "a loaded element must reach a namescope owner");
		Assert.AreEqual(owner, leaf.GetStandardNameScopeOwner(), "a descendant belongs to the same scope as its root");
	}

	/// <summary>
	/// The element joins an already-live tree, so it enters through UIElement.AddChild -> ChildEnter
	/// rather than the initial walk.
	/// </summary>
	[TestMethod]
	public async Task When_Element_Added_At_Runtime_It_Joins_The_Host_Scope()
	{
		var host = new Grid();
		await UITestHelper.Load(host, x => x.IsLoaded);

		var added = new Border { Child = new Border() };
		host.Children.Add(added);
		await TestServices.WindowHelper.WaitForIdle();

		var owner = host.GetStandardNameScopeOwner();
		Assert.IsNotNull(owner);
		Assert.AreEqual(owner, added.GetStandardNameScopeOwner());
		Assert.AreEqual(owner, added.Child.GetStandardNameScopeOwner());
	}

	[TestMethod]
	public async Task When_Nested_Owner_Is_Loaded_Its_Descendants_Stop_At_It()
	{
		var leaf = new Border();
		var inner = new Border { Child = leaf };
		_ = new NameScope { Owner = inner };
		var root = new Border { Child = inner };

		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(inner, leaf.GetStandardNameScopeOwner(), "the nearest owner wins");
		Assert.AreEqual(inner, inner.GetStandardNameScopeOwner());
		Assert.AreNotEqual(inner, root.GetStandardNameScopeOwner(), "the tree outside the nested scope is unaffected");
	}

	/// <summary>
	/// WinUI's Enter walk hands each element the owner's member bit (depends.cpp:947-950), which is
	/// what keeps a descendant of a nested scope resolving to that scope instead of the root visual.
	/// </summary>
	[TestMethod]
	public async Task When_Descendant_Of_Nested_Owner_Enters_It_Is_A_Member()
	{
		var leaf = new Border();
		var inner = new Border { Child = leaf };
		_ = new NameScope { Owner = inner };
		var root = new Border { Child = inner };

		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.IsTrue(inner.IsStandardNameScopeMember, "a nested owner registers its own name somewhere");
		Assert.IsTrue(leaf.IsStandardNameScopeMember, "the member bit is inherited from the owner on Enter");
	}

	[TestMethod]
	public async Task When_Name_Registered_In_Nested_Scope_It_Is_Found_There_Only()
	{
		var leaf = new Border();
		var inner = new Border { Child = leaf };
		var scope = new NameScope { Owner = inner };
		scope.RegisterName("leaf", leaf);
		var root = new Border { Child = inner };

		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(leaf, scope.FindName("leaf"));
		Assert.AreEqual(leaf, inner.GetContext().GetNamedObject("leaf", inner, NameScopeType.StandardNameScope));
		Assert.IsNull(
			root.GetContext().GetNamedObject("leaf", root.GetStandardNameScopeOwner(), NameScopeType.StandardNameScope),
			"the name must not leak into the surrounding scope");
	}
}
