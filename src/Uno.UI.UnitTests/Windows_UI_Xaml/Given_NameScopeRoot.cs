using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Core.NameScoping;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_NameScopeRoot
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		private static (NameScopeRoot Root, Border Owner) CreateScope()
		{
			NameScopeRoot root = new();
			var owner = new Border();
			root.EnsureNameScope(owner, NameScopeType.StandardNameScope);
			return (root, owner);
		}

		[TestMethod]
		public void When_Name_Registered_It_Resolves()
		{
			var (root, owner) = CreateScope();
			var element = new Border();

			root.GetTable(owner, NameScopeType.StandardNameScope).RegisterName("a", element);

			Assert.AreEqual(element, root.GetNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope));
		}

		/// <summary>
		/// The case that defeated both previous attempts at Enter/Leave name registration: two
		/// elements share a name, the loser leaves, and a naive Remove(name) deletes the winner's
		/// entry. Verified red against an unguarded remove before the guard shipped.
		/// </summary>
		[TestMethod]
		public void When_Duplicate_Name_Loser_Leaves_Winner_Survives()
		{
			var (root, owner) = CreateScope();
			var table = root.GetTable(owner, NameScopeType.StandardNameScope);
			var first = new Border();
			var second = new Border();

			table.RegisterName("dupe", first);
			table.RegisterName("dupe", second);

			// `first` leaves the tree and tries to unregister the name it once held.
			var cleared = root.ClearNamedObjectIfExists("dupe", owner, NameScopeType.StandardNameScope, first);

			Assert.IsFalse(cleared, "the loser must not clear an entry it no longer owns");
			Assert.AreEqual(second, root.GetNamedObjectIfExists("dupe", owner, NameScopeType.StandardNameScope));
		}

		[TestMethod]
		public void When_Owner_Leaves_Its_Own_Name_Is_Cleared()
		{
			var (root, owner) = CreateScope();
			var element = new Border();
			root.GetTable(owner, NameScopeType.StandardNameScope).RegisterName("a", element);

			Assert.IsTrue(root.ClearNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope, element));
			Assert.IsNull(root.GetNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope));
		}

		[TestMethod]
		public void When_EnsureNameScope_Called_Twice_Entries_Survive()
		{
			var (root, owner) = CreateScope();
			var element = new Border();
			root.GetTable(owner, NameScopeType.StandardNameScope).RegisterName("a", element);

			root.EnsureNameScope(owner, NameScopeType.StandardNameScope);

			Assert.AreEqual(element, root.GetNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope));
		}

		[TestMethod]
		public void When_Owner_Has_No_Table()
		{
			NameScopeRoot root = new();
			var owner = new Border();

			Assert.IsFalse(root.HasStandardNameScopeTable(owner));
			Assert.IsNull(root.GetTable(owner, NameScopeType.StandardNameScope));
			Assert.IsNull(root.GetNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope));
			Assert.IsFalse(root.ClearNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope, new Border()));
		}

		[TestMethod]
		public void When_Owner_Is_Null()
		{
			NameScopeRoot root = new();

			Assert.IsNull(root.GetTable(null, NameScopeType.StandardNameScope));
			Assert.IsNull(root.GetNamedObjectIfExists("a", null, NameScopeType.StandardNameScope));
			Assert.IsFalse(root.ClearNamedObjectIfExists("a", null, NameScopeType.StandardNameScope, new Border()));
		}

		[TestMethod]
		public void When_Template_Scope_Is_Not_Implemented()
		{
			var (root, owner) = CreateScope();

			// Only the standard table exists so far; asking for the template one must not throw.
			Assert.IsNull(root.GetTable(owner, NameScopeType.TemplateNameScope));
		}

		[TestMethod]
		public void When_Scope_Removed_Names_Are_Gone()
		{
			var (root, owner) = CreateScope();
			root.GetTable(owner, NameScopeType.StandardNameScope).RegisterName("a", new Border());

			root.RemoveNameScopeIfExists(owner, NameScopeType.StandardNameScope);

			Assert.IsFalse(root.HasStandardNameScopeTable(owner));
			Assert.IsNull(root.GetNamedObjectIfExists("a", owner, NameScopeType.StandardNameScope));
		}
	}
}
