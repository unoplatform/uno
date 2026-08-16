#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.DependencyObjectStoreTests;

/// <summary>
/// A <see cref="DependencyObjectStore"/> is allocated for every <see cref="DependencyObject"/>, so its
/// per-instance state is size-sensitive. Two of its dictionaries are only needed by a minority of
/// objects and are therefore allocated on first use; these tests pin both the "not allocated until
/// needed" contract and the behaviour that depends on them (token callbacks, inherited-attached
/// property forwarding to late-added children).
/// </summary>
[TestClass]
public class Given_DependencyObjectStore_LazyState
{
	private static readonly FieldInfo _propertyChangedTokensField =
		typeof(DependencyObjectStore).GetField("_propertyChangedTokens", BindingFlags.NonPublic | BindingFlags.Instance)!;

	private static readonly FieldInfo _inheritedForwardedPropertiesField =
		typeof(DependencyObjectStore).GetField("_inheritedForwardedProperties", BindingFlags.NonPublic | BindingFlags.Instance)!;

	private static object? GetTokens(DependencyObject o)
		=> _propertyChangedTokensField.GetValue(((IDependencyObjectStoreProvider)o).Store);

	private static IReadOnlyDictionary<DependencyProperty, ManagedWeakReference>? GetForwarded(DependencyObject o)
		=> (IReadOnlyDictionary<DependencyProperty, ManagedWeakReference>?)_inheritedForwardedPropertiesField
			.GetValue(((IDependencyObjectStoreProvider)o).Store);

	[TestMethod]
	public void When_No_Token_Callback_Registered_Then_Dictionary_Not_Allocated()
	{
		var sut = new Border();

		sut.SetValue(FrameworkElement.TagProperty, "value");
		Assert.AreEqual("value", sut.GetValue(FrameworkElement.TagProperty));

		Assert.IsNull(
			GetTokens(sut),
			"RegisterPropertyChangedCallback(DependencyProperty, DependencyPropertyChangedCallback) is rarely used, so its token map must not be allocated for every DependencyObject.");
	}

	[TestMethod]
	public void When_Token_Callback_Registered_Then_Dictionary_Allocated_And_Callback_Invoked()
	{
		var sut = new Border();
		var invocations = 0;

		var token = sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (s, p) =>
		{
			Assert.AreSame(sut, s);
			Assert.AreSame(FrameworkElement.TagProperty, p);
			invocations++;
		});

		Assert.IsNotNull(GetTokens(sut), "The token map must be created on first registration.");

		sut.Tag = "first";
		Assert.AreEqual(1, invocations);

		sut.UnregisterPropertyChangedCallback(FrameworkElement.TagProperty, token);

		sut.Tag = "second";
		Assert.AreEqual(1, invocations, "Unregistering the token must stop the callback.");
	}

	[TestMethod]
	public void When_Multiple_Token_Callbacks_Then_Only_The_Unregistered_One_Stops()
	{
		var sut = new Border();
		var first = 0;
		var second = 0;

		var firstToken = sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (_, _) => first++);
		sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (_, _) => second++);

		sut.Tag = "a";
		Assert.AreEqual(1, first);
		Assert.AreEqual(1, second);

		sut.UnregisterPropertyChangedCallback(FrameworkElement.TagProperty, firstToken);

		sut.Tag = "b";
		Assert.AreEqual(1, first);
		Assert.AreEqual(2, second);
	}

	[TestMethod]
	public void When_Unregister_Unknown_Token_Then_No_Throw()
	{
		var sut = new Border();

		// Nothing was ever registered, so the token map is still unallocated: unregistering must
		// remain a no-op rather than dereferencing it.
		sut.UnregisterPropertyChangedCallback(FrameworkElement.TagProperty, 42);

		sut.RegisterPropertyChangedCallback(FrameworkElement.TagProperty, (_, _) => { });
		sut.UnregisterPropertyChangedCallback(FrameworkElement.TagProperty, 4242);
	}

	[TestMethod]
	public void When_No_Inherited_Property_Forwarded_Then_Dictionary_Not_Allocated()
	{
		var root = new Grid();
		var child = new Border();

		root.Children.Add(child);
		root.DataContext = new object();

		Assert.AreSame(root.DataContext, child.DataContext, "Pre-condition: DataContext must still be inherited.");

		// DataContext is a local property on both, so nothing needs forwarding on behalf of a descendant.
		Assert.IsNull(GetForwarded(root), "No inherited property was forwarded through the root.");
		Assert.IsNull(GetForwarded(child), "No inherited property was forwarded through the child.");
	}

	[TestMethod]
	public void When_Inherited_Attached_Property_Set_Then_Forwarded_To_Late_Added_Child()
	{
		var root = new Grid();

		LazyStateAttachedProperties.SetInheritedValue(root, 42);

		var forwarded = GetForwarded(root);
		Assert.IsNotNull(forwarded, "Setting an inheritable attached property must create the forwarding map on the owner.");
		Assert.IsTrue(
			forwarded!.ContainsKey(LazyStateAttachedProperties.InheritedValueProperty),
			"The inheritable attached property must be registered for forwarding to late-added children.");

		var lateChild = new Border();
		root.Children.Add(lateChild);

		Assert.AreEqual(42, LazyStateAttachedProperties.GetInheritedValue(lateChild), "A late-added child must receive the forwarded value.");

		LazyStateAttachedProperties.SetInheritedValue(root, 43);
		Assert.AreEqual(43, LazyStateAttachedProperties.GetInheritedValue(lateChild), "Later updates must keep propagating.");
	}

	[TestMethod]
	public void When_Removed_From_Parent_Then_Inherited_Value_Reset()
	{
		var root = new Grid();
		var intermediate = new Border();
		var leaf = new Border();

		root.Children.Add(intermediate);
		LazyStateAttachedProperties.SetInheritedValue(root, 7);

		intermediate.Child = leaf;
		Assert.AreEqual(7, LazyStateAttachedProperties.GetInheritedValue(leaf), "Pre-condition: the value must reach the leaf.");

		var forwarded = GetForwarded(intermediate);
		Assert.IsNotNull(forwarded, "Pre-condition: the intermediate must have recorded the inheritable attached property.");
		Assert.IsTrue(forwarded!.ContainsKey(LazyStateAttachedProperties.InheritedValueProperty));

		root.Children.Remove(intermediate);

		Assert.AreEqual(0, LazyStateAttachedProperties.GetInheritedValue(intermediate), "Detaching must drop the inherited value.");
		Assert.AreEqual(0, LazyStateAttachedProperties.GetInheritedValue(leaf), "The reset must keep propagating to the subtree.");
	}

	[TestMethod]
	public void When_Removed_From_Parent_Without_Forwarding_Then_No_Throw()
	{
		var root = new Grid();
		var child = new Border();

		root.Children.Add(child);
		Assert.IsNull(GetForwarded(child), "Pre-condition: nothing was forwarded, so the map must still be unallocated.");

		// Unregistering inherited properties clears the forwarding map; it must tolerate it never
		// having been allocated.
		root.Children.Remove(child);

		Assert.IsNull(GetForwarded(child));
	}
}

public static class LazyStateAttachedProperties
{
	public static int GetInheritedValue(DependencyObject obj) => (int)obj.GetValue(InheritedValueProperty);

	public static void SetInheritedValue(DependencyObject obj, int value) => obj.SetValue(InheritedValueProperty, value);

	public static readonly DependencyProperty InheritedValueProperty =
		DependencyProperty.RegisterAttached(
			name: "InheritedValue",
			propertyType: typeof(int),
			ownerType: typeof(LazyStateAttachedProperties),
			defaultMetadata: new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));
}
