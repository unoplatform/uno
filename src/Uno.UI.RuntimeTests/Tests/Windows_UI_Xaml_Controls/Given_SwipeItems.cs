#if HAS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// First/GetMany/ReplaceAll are absent from the WinUI C# projection, so the whole fixture is Uno-only.
[TestClass]
public class Given_SwipeItems
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_CompatibilityVectorMethodsAreHidden()
	{
		var publicMethods = typeof(SwipeItems).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

		foreach (var methodName in new[] { "First", "GetMany", "ReplaceAll" })
		{
			var method = publicMethods.Single(method => method.Name == methodName);
			Assert.IsNotNull(method.GetCustomAttribute<ObsoleteAttribute>());
			Assert.AreEqual(EditorBrowsableState.Never, method.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	[RunsOnUIThread]
	public void When_CompatibilityVectorMethodsWork()
	{
		var items = new SwipeItems();
		var first = new SwipeItem();
		var second = new SwipeItem();

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		Assert.ThrowsExactly<IndexOutOfRangeException>(() => items.First());
#pragma warning restore CS0618

		items.Add(first);
		items.Add(second);

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		Assert.AreSame(first, items.First());

		var destination = new SwipeItem[2];
		Assert.AreEqual(1u, items.GetMany(1, destination));
		Assert.AreSame(second, destination[0]);
		Assert.IsNull(destination[1]);
		Assert.AreEqual(0u, items.GetMany(2, destination));
		Assert.ThrowsExactly<IndexOutOfRangeException>(() => items.GetMany(3, destination));

		var changeCount = 0;
		IObservableVector<SwipeItem> changedSender = null;
		IVectorChangedEventArgs changedArgs = null;
		items.VectorChanged += (sender, args) =>
		{
			changeCount++;
			changedSender = sender;
			changedArgs = args;
		};
		IList<SwipeItem> list = items;
		items.ReplaceAll([second]);
#pragma warning restore CS0618

		Assert.AreEqual(1, changeCount);
		Assert.AreSame(items, changedSender);
		Assert.AreEqual(CollectionChange.Reset, changedArgs.CollectionChange);
		Assert.AreEqual(0u, changedArgs.Index);
		Assert.AreSame(items, list);
		Assert.AreEqual(1, items.Count);
		Assert.AreSame(second, list[0]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	[RunsOnUIThread]
	public void When_ReplaceAllValidationFails_Then_ItemsAreUnchanged()
	{
		var items = new SwipeItems { Mode = SwipeMode.Execute };
		var original = new SwipeItem();
		items.Add(original);
		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;
		var enumerator = items.GetEnumerator();

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		Assert.ThrowsExactly<ArgumentNullException>(() => items.ReplaceAll(null));
		Assert.ThrowsExactly<ArgumentException>(() => items.ReplaceAll([new SwipeItem(), new SwipeItem()]));
#pragma warning restore CS0618

		Assert.AreEqual(0, changeCount);
		Assert.AreEqual(1, items.Count);
		Assert.AreSame(original, items[0]);
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreSame(original, enumerator.Current);
		Assert.IsFalse(enumerator.MoveNext());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	[RunsOnUIThread]
	public void When_ReplaceAll_Then_OutstandingEnumeratorIsInvalidated()
	{
		var removedFirst = new SwipeItem();
		var removedSecond = new SwipeItem();
		var replacement = new SwipeItem();
		var items = new SwipeItems { removedFirst, removedSecond };
		var enumerator = items.GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreSame(removedFirst, enumerator.Current);

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		items.ReplaceAll([replacement]);
#pragma warning restore CS0618

		Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
		CollectionAssert.AreEqual(new[] { replacement }, items.ToArray());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	[RunsOnUIThread]
	public void When_ReplaceAllWithEmptyArray_Then_EnumeratorIsInvalidatedAndResetRaised()
	{
		var items = new SwipeItems { new SwipeItem() };
		var enumerator = items.GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());
		var changeCount = 0;
		IVectorChangedEventArgs changedArgs = null;
		items.VectorChanged += (_, args) =>
		{
			changeCount++;
			changedArgs = args;
		};

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		items.ReplaceAll([]);
#pragma warning restore CS0618

		Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(CollectionChange.Reset, changedArgs.CollectionChange);
		Assert.AreEqual(0, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	[RunsOnUIThread]
	public void When_RemoveRaisesVectorChanged()
	{
		var items = new SwipeItems();
		var item = new SwipeItem();
		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;
		items.Add(item);
		changeCount = 0;

		Assert.IsTrue(items.Remove(item));
		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(0, items.Count);
	}
}
#endif
