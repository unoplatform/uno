#nullable enable

using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

[UITestClass]
public class Given_DependencyObject_LazyPropertyStorage
{
	[TestMethod]
	public void When_DataOnly_Objects_Are_Created_Then_No_Property_Storage_Is_Allocated()
	{
		Assert.IsFalse(HasPropertyStorage(new DependencyObject()));
		Assert.IsFalse(HasPropertyStorage(new Setter()));
		Assert.IsFalse(HasPropertyStorage(new Binding { Path = new PropertyPath("Foo") }));
	}

	[TestMethod]
	public void When_Reading_Unset_Property_Then_Default_Is_Returned_Without_Storage()
	{
		LazyStorageObject SUT = new();

		Assert.AreEqual("default", SUT.GetValue(LazyStorageObject.ValueProperty));
		Assert.AreEqual(DependencyProperty.UnsetValue, SUT.ReadLocalValue(LazyStorageObject.ValueProperty));
		Assert.IsFalse(HasPropertyStorage(SUT));
	}

	[TestMethod]
	public void When_Property_Is_Set_Then_Storage_Is_Allocated()
	{
		LazyStorageObject SUT = new();

		SUT.SetValue(LazyStorageObject.ValueProperty, "set");

		Assert.IsTrue(HasPropertyStorage(SUT));
		Assert.AreEqual("set", SUT.GetValue(LazyStorageObject.ValueProperty));
	}

	[TestMethod]
	public void When_Inherited_DataContext_Arrives_Before_Storage_Then_Later_Binding_Uses_It()
	{
		LazyStorageObject SUT = new();
		Border host = new() { DataContext = "inherited" };

		host.Tag = SUT;
		SUT.SetBindingInternal(LazyStorageObject.ValueProperty, new Binding());

		Assert.AreEqual("inherited", SUT.GetValue(LazyStorageObject.ValueProperty));
	}

	[TestMethod]
	public void When_Bindings_Suspended_Before_Storage_Then_Resume_Applies_DataContext()
	{
		LazyStorageObject SUT = new();
		Border host = new() { DataContext = "inherited" };

		SUT.SuspendBindings();
		host.Tag = SUT;
		SUT.SetBindingInternal(LazyStorageObject.ValueProperty, new Binding());
		SUT.ResumeBindings();

		Assert.AreEqual("inherited", SUT.GetValue(LazyStorageObject.ValueProperty));
	}

	private static bool HasPropertyStorage(DependencyObject o)
		=> typeof(DependencyObject)
			.GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(o) is not null;
}

public partial class LazyStorageObject : DependencyObject
{
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(nameof(Value), typeof(string), typeof(LazyStorageObject), new PropertyMetadata("default"));

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
}
