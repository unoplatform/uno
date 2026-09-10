#nullable enable

using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests;

[TestClass]
public partial class Given_BindingPropertyHelper_InterfaceProperties
{
	[TestMethod]
	public void When_GetPropertyType_InterfaceProperty_Array()
	{
		// An array exposes Length, and gets Count only through IReadOnlyCollection<T>.
		var propertyType = BindingPropertyHelper.GetPropertyType(typeof(string[]), "Count", false);

		Assert.AreEqual(typeof(int), propertyType);
	}

	[TestMethod]
	public void When_GetValueGetter_InterfaceProperty_Array()
	{
		var getter = BindingPropertyHelper.GetValueGetter(typeof(string[]), "Count");

		Assert.AreEqual(3, getter(new[] { "Item1", "Item2", "Item3" }));
	}

	[TestMethod]
	public void When_GetValueGetter_InterfaceIndexer_Array()
	{
		var getter = BindingPropertyHelper.GetValueGetter(typeof(string[]), "[0]");

		Assert.AreEqual("Item1", getter(new[] { "Item1", "Item2", "Item3" }));
	}

	[TestMethod]
	public void When_GetPropertyType_InterfaceProperty_List()
	{
		var propertyType = BindingPropertyHelper.GetPropertyType(typeof(List<string>), "Count", false);

		Assert.AreEqual(typeof(int), propertyType);
	}

	[TestMethod]
	public void When_GetPropertyType_NonExistentProperty()
	{
		var propertyType = BindingPropertyHelper.GetPropertyType(typeof(string[]), "NonExistent", false);

		Assert.IsNull(propertyType);
	}

	[TestMethod]
	public void When_GetPropertyType_ExplicitInterfaceProperty()
	{
		var propertyType = BindingPropertyHelper.GetPropertyType(typeof(ExplicitReadOnlyList), "Count", false);

		Assert.AreEqual(typeof(int), propertyType);
	}

	[TestMethod]
	public void When_GetValueGetter_ExplicitInterfaceProperty()
	{
		var getter = BindingPropertyHelper.GetValueGetter(typeof(ExplicitReadOnlyList), "Count");

		Assert.AreEqual(2, getter(new ExplicitReadOnlyList()));
	}

	[TestMethod]
	public void When_GetValueGetter_ExplicitInterfaceIndexer()
	{
		var getter = BindingPropertyHelper.GetValueGetter(typeof(ExplicitReadOnlyList), "[1]");

		Assert.AreEqual("B", getter(new ExplicitReadOnlyList()));
	}

	[TestMethod]
	public void When_GetPropertyType_ConcreteProperty_Wins_Over_Interface()
	{
		// Both the concrete type and ICounted declare Count, with different types.
		var propertyType = BindingPropertyHelper.GetPropertyType(typeof(ConcreteCounted), "Count", false);
		var getter = BindingPropertyHelper.GetValueGetter(typeof(ConcreteCounted), "Count");

		Assert.AreEqual(typeof(int), propertyType);
		Assert.AreEqual(42, getter(new ConcreteCounted()));
	}

	private interface ICounted
	{
		string Count { get; }
	}

	private class ConcreteCounted : ICounted
	{
		public int Count => 42;

		string ICounted.Count => "from-interface";
	}

	private class ExplicitReadOnlyList : IReadOnlyList<string>
	{
		private readonly string[] _items = new[] { "A", "B" };

		string IReadOnlyList<string>.this[int index] => _items[index];

		int IReadOnlyCollection<string>.Count => _items.Length;

		IEnumerator<string> IEnumerable<string>.GetEnumerator() => ((IEnumerable<string>)_items).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	}
}
