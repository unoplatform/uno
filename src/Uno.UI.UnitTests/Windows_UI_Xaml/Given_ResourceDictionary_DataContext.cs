#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ResourceDictionary_DataContext
	{
		// A shared resource (e.g. a brush in a GlobalStaticResources singleton) is rooted by a long-lived
		// ResourceDictionary, yet it is pulled into many short-lived subtrees via {StaticResource}/{ThemeResource}.
		// If such a resource inherited/cached the DataContext of the subtree it is applied into, it would keep that
		// DataContext — and everything it transitively references — alive forever. When the subtree lives in a
		// collectible AssemblyLoadContext (a previewed app), that retention pins the whole context and prevents it
		// from unloading. WinUI gives resources no DataContext; these tests guard that parity.

		[TestMethod]
		public void When_Element_Stored_In_ResourceDictionary_Then_Flagged_As_ResourceDictionaryItem()
		{
			var resource = new Border();

			var dictionary = new ResourceDictionary { ["res"] = resource };

			Assert.IsTrue(
				((IDependencyObjectStoreProvider)resource).Store.IsResourceDictionaryItem,
				"An element stored as a ResourceDictionary value must be flagged so it does not inherit/cache DataContext.");

			Assert.IsTrue(dictionary.ContainsKey("res"));
		}

		[TestMethod]
		public void When_Resource_Element_In_Subtree_With_DataContext_Then_Resource_Does_Not_Inherit_It()
		{
			var resource = new Border();
			var host = new Grid();
			host.Resources["res"] = resource;

			var dataContext = new object();
			host.DataContext = dataContext;

			Assert.IsNull(
				resource.DataContext,
				"Resources have no DataContext (WinUI parity); inheriting it would pin the subtree's DataContext — " +
				"and, for a previewed app, its collectible AssemblyLoadContext — alive via the shared dictionary.");
		}

		[TestMethod]
		public void When_NonResource_Child_In_Subtree_With_DataContext_Then_It_Still_Inherits()
		{
			// Guard against over-blocking: only ResourceDictionary items are exempted; ordinary children
			// must still inherit DataContext as usual.
			var child = new Border();
			var host = new Grid();
			host.Children.Add(child);

			var dataContext = new object();
			host.DataContext = dataContext;

			Assert.AreSame(
				dataContext,
				child.DataContext,
				"A normal logical child must continue to inherit DataContext; only ResourceDictionary items are exempt.");
		}

		[TestMethod]
		public void When_Element_Binding_Uses_Inherited_DataContext_Then_It_Still_Resolves()
		{
			// Guards the reviewer concern that blocking DataContext on resources could break binding in styles.
			// A Style's setter {Binding} resolves against the *styled element's* DataContext, and the block is
			// scoped to ResourceDictionary items — never the elements a resource/style is applied to. So a
			// {Binding} on a normal element must still resolve via inherited DataContext, even while resources
			// in the same tree carry none.
			var vm = new BindingProbe { Value = "bound!" };

			var host = new Grid();
			var child = new Border();
			host.Children.Add(child);
			child.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath(nameof(BindingProbe.Value)) });

			host.DataContext = vm; // propagates to the child, where the binding evaluates

			Assert.AreEqual(
				"bound!",
				child.Tag,
				"a {Binding} on a normal element must still resolve via inherited DataContext; the DataContext " +
				"block is scoped to ResourceDictionary items, not the elements styles/resources are applied to.");
		}

		private sealed class BindingProbe
		{
			public string? Value { get; set; }
		}
	}
}
