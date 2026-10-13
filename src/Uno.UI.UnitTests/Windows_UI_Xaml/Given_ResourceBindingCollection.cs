using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ResourceBindingCollection
{
	private static ResourceBinding Binding(string key, DependencyPropertyValuePrecedences precedence = DependencyPropertyValuePrecedences.Local)
		=> new(new SpecializedResourceDictionary.ResourceKey(key), ResourceUpdateReason.ThemeResource, parseContext: null, precedence, setterBindingPath: null);

	[TestMethod]
	public void When_Same_Property_And_Precedence_Then_Replaced_In_Place()
	{
		var sut = new ResourceBindingCollection();
		var first = Binding("A");
		var other = Binding("B");
		var replacement = Binding("C");

		sut.Add(Border.BackgroundProperty, first);
		sut.Add(Border.BorderBrushProperty, other);
		sut.Add(Border.BackgroundProperty, replacement);

		CollectionAssert.AreEqual(new[] { replacement, other }, sut.GetAllBindings().Select(e => e.Binding).ToArray());
	}

	[TestMethod]
	public void When_Different_Precedences_Then_Both_Kept()
	{
		var sut = new ResourceBindingCollection();
		var local = Binding("A");
		var style = Binding("B", DependencyPropertyValuePrecedences.Style);

		sut.Add(Border.BackgroundProperty, local);
		sut.Add(Border.BackgroundProperty, style);

		CollectionAssert.AreEquivalent(new[] { local, style }, sut.GetBindingsForProperty(Border.BackgroundProperty)!.ToArray());
	}

	[TestMethod]
	public void When_Cleared_Then_Only_That_Precedence_Removed()
	{
		var sut = new ResourceBindingCollection();
		var local = Binding("A");
		var style = Binding("B", DependencyPropertyValuePrecedences.Style);
		sut.Add(Border.BackgroundProperty, local);
		sut.Add(Border.BackgroundProperty, style);

		sut.ClearBinding(Border.BackgroundProperty, DependencyPropertyValuePrecedences.Local);

		CollectionAssert.AreEqual(new[] { style }, sut.GetAllBindings().Select(e => e.Binding).ToArray());

		sut.ClearBinding(Border.BackgroundProperty, DependencyPropertyValuePrecedences.Style);

		Assert.IsFalse(sut.HasBindings);
		Assert.IsNull(sut.GetBindingsForProperty(Border.BackgroundProperty));
	}

	[TestMethod]
	public void When_Changed_Then_Snapshot_Refreshed()
	{
		var sut = new ResourceBindingCollection();
		sut.Add(Border.BackgroundProperty, Binding("A"));

		var snapshot = sut.GetAllBindings();
		Assert.AreSame(snapshot, sut.GetAllBindings(), "The snapshot is cached while nothing changes");

		sut.Add(Border.BorderBrushProperty, Binding("B"));

		Assert.AreEqual(1, snapshot.Length, "A snapshot taken earlier is not mutated");
		Assert.AreEqual(2, sut.GetAllBindings().Length);
	}
}
