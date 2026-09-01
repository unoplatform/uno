using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.NameScoping;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_NameScopeOwner
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		private static NameScopeRoot Root => CoreServices.Instance.NameScopeRoot;

		[TestMethod]
		public void When_Owner_Set_Owner_Is_Minted()
		{
			NameScope scope = new();
			var owner = new Border();

			scope.Owner = owner;

			Assert.AreEqual(owner, scope.Owner);
			Assert.IsTrue(owner.IsStandardNameScopeOwner);
			Assert.IsTrue(owner.IsStandardNameScopeMember);
			Assert.IsTrue(Root.HasStandardNameScopeTable(owner));
		}

		[TestMethod]
		public void When_Owner_Set_Twice_First_Owner_Wins()
		{
			NameScope scope = new();
			var first = new Border();
			var second = new Border();

			scope.Owner = first;
			scope.Owner = second;

			Assert.AreEqual(first, scope.Owner);
			Assert.IsFalse(second.IsStandardNameScopeOwner);
			Assert.IsFalse(Root.HasStandardNameScopeTable(second));
		}

		[TestMethod]
		public void When_Owner_Set_Names_Land_In_Owner_Table()
		{
			NameScope scope = new();
			var owner = new Border();
			var child = new Border();
			scope.Owner = owner;

			scope.RegisterName("child", child);

			Assert.AreEqual(child, Root.GetNamedObjectIfExists("child", owner, NameScopeType.StandardNameScope));
			Assert.AreEqual(child, scope.FindName("child"));
		}

		/// <summary>
		/// Template bodies are built bottom-up, so the root (and thus the owner) is only known after
		/// every child name has been registered.
		/// </summary>
		[TestMethod]
		public void When_Names_Registered_Before_Owner_They_Flush_Into_Owner_Table()
		{
			NameScope scope = new();
			var owner = new Border();
			var child = new Border();

			scope.RegisterName("child", child);
			Assert.AreEqual(child, scope.FindName("child"), "pending names must resolve before the owner is known");

			scope.Owner = owner;

			Assert.AreEqual(child, Root.GetNamedObjectIfExists("child", owner, NameScopeType.StandardNameScope));
			Assert.AreEqual(child, scope.FindName("child"));
		}

		[TestMethod]
		public void When_Name_Unregistered_It_No_Longer_Resolves()
		{
			NameScope scope = new();
			var owner = new Border();
			var child = new Border();
			scope.Owner = owner;
			scope.RegisterName("child", child);

			scope.UnregisterName("child");

			Assert.IsNull(scope.FindName("child"));
			Assert.IsNull(Root.GetNamedObjectIfExists("child", owner, NameScopeType.StandardNameScope));
		}

		[TestMethod]
		public void When_Name_Unregistered_Before_Owner_It_Does_Not_Flush()
		{
			NameScope scope = new();
			var owner = new Border();
			scope.RegisterName("child", new Border());

			scope.UnregisterName("child");
			scope.Owner = owner;

			Assert.IsNull(Root.GetNamedObjectIfExists("child", owner, NameScopeType.StandardNameScope));
		}

		[TestMethod]
		public void When_Same_Name_Registered_Twice_Last_Wins()
		{
			NameScope scope = new();
			var owner = new Border();
			var first = new Border();
			var second = new Border();
			scope.Owner = owner;

			scope.RegisterName("dupe", first);
			scope.RegisterName("dupe", second);

			Assert.AreEqual(second, scope.FindName("dupe"));
		}

		[TestMethod]
		public void When_Non_DependencyObject_Registered_It_Is_Ignored()
		{
			NameScope scope = new();
			scope.Owner = new Border();

			scope.RegisterName("plain", new object());

			Assert.IsNull(scope.FindName("plain"));
		}

		[TestMethod]
		public void When_Owner_Walk_From_Owner_Returns_Itself()
		{
			NameScope scope = new();
			var owner = new Border();
			scope.Owner = owner;

			Assert.AreEqual(owner, owner.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_Owner_Walk_From_Descendant_Climbs_To_Owner()
		{
			var leaf = new Border();
			var middle = new Border { Child = leaf };
			var owner = new Border { Child = middle };
			NameScope scope = new() { Owner = owner };

			Assert.AreEqual(owner, middle.GetStandardNameScopeOwner());
			Assert.AreEqual(owner, leaf.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_Owner_Walk_Crosses_Nested_Owner_Nearest_Wins()
		{
			var leaf = new Border();
			var inner = new Border { Child = leaf };
			var outer = new Border { Child = inner };
			NameScope outerScope = new() { Owner = outer };
			NameScope innerScope = new() { Owner = inner };

			Assert.AreEqual(inner, leaf.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_Owner_Walk_Finds_No_Owner_It_Returns_Null()
		{
			var leaf = new Border();
			var root = new Border { Child = leaf };

			Assert.IsNull(leaf.GetStandardNameScopeOwner());
			Assert.IsNull(root.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_Owner_Walk_Hits_A_Cycle_It_Terminates()
		{
			var a = new CyclicObject();
			var b = new CyclicObject();
			a.NameScopeParent = b;
			b.NameScopeParent = a;

			Assert.IsNull(a.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_IsParsing_Parser_Owns_Parent()
		{
			var element = new Border();
			Assert.IsFalse(element.ParserOwnsParent);

			element.IsParsing = true;
			Assert.IsTrue(element.ParserOwnsParent);

			element.CreationComplete();
			Assert.IsFalse(element.ParserOwnsParent);
		}

		[TestMethod]
		public void When_Definition_Name_Marked_Owner_Registers_In_Parent_Namescope()
		{
			NameScope scope = new();
			var owner = new Border();
			scope.Owner = owner;
			Assert.IsFalse(owner.ShouldRegisterInParentNamescope);

			scope.MarkOwnerAsPossiblyHavingDefinitionName();

			Assert.IsTrue(owner.ShouldRegisterInParentNamescope);
		}

		[TestMethod]
		public void When_XClass_Root_It_Owns_Its_Names()
		{
			var SUT = new When_NameScope();

			Assert.IsTrue(SUT.IsStandardNameScopeOwner);
			Assert.IsTrue(SUT.IsStandardNameScopeMember);
			Assert.IsTrue(SUT.ShouldRegisterInParentNamescope, "an x:Class root carries a definition name");
			Assert.IsFalse(SUT.ParserOwnsParent, "the parser lock must be released once InitializeComponent completes");
			Assert.AreEqual(SUT.OuterBorder, Root.GetNamedObjectIfExists(nameof(SUT.OuterBorder), SUT, NameScopeType.StandardNameScope));
			Assert.AreEqual(SUT, Root.GetNamedObjectIfExists(nameof(SUT.OuterElementTopLevel), SUT, NameScopeType.StandardNameScope));
			Assert.AreEqual(SUT, SUT.OuterBorder.GetStandardNameScopeOwner());
		}

		[TestMethod]
		public void When_Nested_XClass_Root_Its_Names_Stay_In_Its_Own_Table()
		{
			var SUT = new When_NameScope();
			var inner = SUT.OuterElementName;

			Assert.IsTrue(inner.IsStandardNameScopeOwner);
			Assert.AreEqual(inner.InnerBorder, Root.GetNamedObjectIfExists(nameof(inner.InnerBorder), inner, NameScopeType.StandardNameScope));
			Assert.IsNull(Root.GetNamedObjectIfExists(nameof(inner.InnerBorder), SUT, NameScopeType.StandardNameScope));
			Assert.AreEqual(inner, inner.InnerBorder.GetStandardNameScopeOwner());
		}

		private sealed class CyclicObject : DependencyObject
		{
			public DependencyObject NameScopeParent { get; set; }

			internal override DependencyObject GetStandardNameScopeParent() => NameScopeParent;
		}
	}
}
