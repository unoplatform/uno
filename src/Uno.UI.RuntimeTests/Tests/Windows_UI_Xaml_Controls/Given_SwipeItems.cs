#if HAS_UNO
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// First/GetMany/ReplaceAll are absent from the WinUI C# projection, so the whole fixture is Uno-only.
[TestClass]
[RunsOnUIThread]
public class Given_SwipeItems
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_CompatibilityMembers_Then_Hidden_From_IntelliSense()
	{
		var publicMethods = typeof(SwipeItems).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

		foreach (var methodName in new[] { "First", "GetMany", "ReplaceAll" })
		{
			var method = publicMethods.Single(candidate => candidate.Name == methodName);
			Assert.IsNotNull(method.GetCustomAttribute<ObsoleteAttribute>(), $"{methodName} should be obsolete.");
			Assert.AreEqual(EditorBrowsableState.Never, method.GetCustomAttribute<EditorBrowsableAttribute>()?.State, $"{methodName} should be browsable-never.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_First_And_Empty_Then_Throws()
	{
		var items = new SwipeItems();

#pragma warning disable CS0618 // Compatibility members are deliberately exercised here.
		Assert.ThrowsExactly<IndexOutOfRangeException>(() => items.First());
#pragma warning restore CS0618
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_First_Then_Returns_Item_At_Index_Zero()
	{
		var first = new SwipeItem();
		var items = new SwipeItems { first, new SwipeItem() };

#pragma warning disable CS0618
		Assert.AreSame(first, items.First());
#pragma warning restore CS0618
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_GetMany_Then_Copies_From_StartIndex()
	{
		var first = new SwipeItem();
		var second = new SwipeItem();
		var third = new SwipeItem();
		var items = new SwipeItems { first, second, third };
		var destination = new SwipeItem[2];

#pragma warning disable CS0618
		Assert.AreEqual(2u, items.GetMany(0, destination));
		CollectionAssert.AreEqual(new[] { first, second }, destination);

		Array.Clear(destination, 0, destination.Length);
		Assert.AreEqual(1u, items.GetMany(2, destination));
#pragma warning restore CS0618

		Assert.AreSame(third, destination[0]);
		Assert.IsNull(destination[1]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_GetMany_And_StartIndex_Is_Out_Of_Range_Then_Returns_Zero()
	{
		var items = new SwipeItems { new SwipeItem() };
		var destination = new SwipeItem[2];

		// VectorInnerImpl::GetMany returns 0 instead of throwing once the start index reaches the end.
#pragma warning disable CS0618
		Assert.AreEqual(0u, items.GetMany(1, destination));
		Assert.AreEqual(0u, items.GetMany(7, destination));
		Assert.AreEqual(0u, new SwipeItems().GetMany(0, destination));
#pragma warning restore CS0618

		Assert.IsNull(destination[0]);
		Assert.IsNull(destination[1]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_GetMany_And_Destination_Is_Null_Then_Throws()
	{
		var items = new SwipeItems();

#pragma warning disable CS0618
		Assert.ThrowsExactly<ArgumentNullException>(() => items.GetMany(0, null));
#pragma warning restore CS0618
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_ReplaceAll_Then_Items_Replaced_And_Single_Notification_Raised()
	{
		var replacement = new SwipeItem();
		var items = new SwipeItems { new SwipeItem(), new SwipeItem() };

		var changeCount = 0;
		IObservableVector<SwipeItem> changedSender = null;
		var receivedNullArgs = false;
		items.VectorChanged += (sender, args) =>
		{
			changeCount++;
			changedSender = sender;
			receivedNullArgs = args is null;
		};

#pragma warning disable CS0618
		items.ReplaceAll([replacement]);
#pragma warning restore CS0618

		Assert.AreEqual(1, changeCount);
		Assert.AreSame(items, changedSender);
		// SwipeItems.cpp raises every mutation notification with null args.
		Assert.IsTrue(receivedNullArgs);
		CollectionAssert.AreEqual(new[] { replacement }, items.ToArray());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_ReplaceAll_Then_Outstanding_Enumerator_Is_Invalidated()
	{
		var first = new SwipeItem();
		var replacement = new SwipeItem();
		var items = new SwipeItems { first, new SwipeItem() };
		var enumerator = items.GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreSame(first, enumerator.Current);

#pragma warning disable CS0618
		items.ReplaceAll([replacement]);
#pragma warning restore CS0618

		// Proves the backing vector was mutated in place rather than swapped for a new instance.
		Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
		CollectionAssert.AreEqual(new[] { replacement }, items.ToArray());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_ReplaceAll_With_Empty_Array_Then_Collection_Is_Cleared()
	{
		var items = new SwipeItems { new SwipeItem() };
		var enumerator = items.GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());

		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;

#pragma warning disable CS0618
		items.ReplaceAll([]);
#pragma warning restore CS0618

		Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(0, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_ReplaceAll_Is_Rejected_Then_Items_Are_Unchanged()
	{
		var original = new SwipeItem();
		var items = new SwipeItems { Mode = SwipeMode.Execute };
		items.Add(original);

		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;
		var enumerator = items.GetEnumerator();

#pragma warning disable CS0618
		Assert.ThrowsExactly<ArgumentNullException>(() => items.ReplaceAll(null));
		Assert.ThrowsExactly<ArgumentException>(() => items.ReplaceAll([new SwipeItem(), new SwipeItem()]));
#pragma warning restore CS0618

		Assert.AreEqual(0, changeCount);
		CollectionAssert.AreEqual(new[] { original }, items.ToArray());
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreSame(original, enumerator.Current);
		Assert.IsFalse(enumerator.MoveNext());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_ReplaceAll_In_Execute_Mode_With_Single_Item_Then_Succeeds()
	{
		var replacement = new SwipeItem();
		var items = new SwipeItems { Mode = SwipeMode.Execute };
		items.Add(new SwipeItem());

#pragma warning disable CS0618
		items.ReplaceAll([replacement]);
#pragma warning restore CS0618

		CollectionAssert.AreEqual(new[] { replacement }, items.ToArray());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_Remove_Then_Raises_VectorChanged()
	{
		var item = new SwipeItem();
		var items = new SwipeItems { item };

		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;

		Assert.IsTrue(items.Remove(item));
		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(0, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_Remove_Unknown_Item_Then_No_Notification()
	{
		var items = new SwipeItems { new SwipeItem() };

		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;

		Assert.IsFalse(items.Remove(new SwipeItem()));
		Assert.AreEqual(0, changeCount);
		Assert.AreEqual(1, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_RemoveAtEnd_And_Empty_Then_Notification_Is_Raised_Without_Throwing()
	{
		var items = new SwipeItems();

		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;

		// VectorInnerImpl::RemoveAtEnd is a no-op on an empty vector, and SwipeItems still notifies.
		items.RemoveAtEnd();

		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(0, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_RemoveAtEnd_Then_Last_Item_Is_Removed()
	{
		var first = new SwipeItem();
		var items = new SwipeItems { first, new SwipeItem() };

		items.RemoveAtEnd();

		CollectionAssert.AreEqual(new[] { first }, items.ToArray());
	}
}
#endif
