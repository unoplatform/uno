// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Controls.SwipeControl;

[TestClass]
public class SwipeItemsTests
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void CompatibilityVectorMethodsAreHidden()
	{
#if HAS_UNO
		var publicMethods = typeof(SwipeItems).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

		foreach (var methodName in new[] { "First", "GetMany", "ReplaceAll" })
		{
			var method = publicMethods.Single(method => method.Name == methodName);
			Assert.IsNotNull(method.GetCustomAttribute<ObsoleteAttribute>());
			Assert.AreEqual(EditorBrowsableState.Never, method.GetCustomAttribute<EditorBrowsableAttribute>()?.State);
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void CompatibilityVectorMethodsWork()
	{
#if HAS_UNO
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

		items.ReplaceAll([second]);
#pragma warning restore CS0618

		Assert.AreEqual(1, items.Count);
		Assert.AreSame(second, items[0]);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void ReplaceAllPreservesExecuteItemsWhenRejected()
	{
#if HAS_UNO
		var items = new SwipeItems { Mode = SwipeMode.Execute };
		var original = new SwipeItem();
		items.Add(original);
		var changeCount = 0;
		items.VectorChanged += (_, _) => changeCount++;

#pragma warning disable CS0618 // Validate compatibility members retained for binary compatibility.
		Assert.ThrowsExactly<ArgumentException>(() => items.ReplaceAll([new SwipeItem(), new SwipeItem()]));
#pragma warning restore CS0618

		Assert.AreEqual(0, changeCount);
		Assert.AreEqual(1, items.Count);
		Assert.AreSame(original, items[0]);
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	public void RemoveRaisesVectorChanged()
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
