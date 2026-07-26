using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.SwipeItemsTests
{
	[TestClass]
	public class Given_SwipeItems
	{
		[TestMethod]
		public void When_GetView_Then_ReturnsViewWithSameItemsAndOrder()
		{
			var item1 = new SwipeItem { Text = "Item1" };
			var item2 = new SwipeItem { Text = "Item2" };

			var SUT = new SwipeItems { item1, item2 };

			var view = SUT.GetView();

			Assert.IsNotNull(view);
			Assert.AreEqual(2, view.Count);
			Assert.AreSame(item1, view[0]);
			Assert.AreSame(item2, view[1]);
		}

		[TestMethod]
		public void When_GetView_Then_ReturnsIReadOnlyList()
		{
			var SUT = new SwipeItems();

			// The WinUI API surface for SwipeItems.GetView is `IReadOnlyList<SwipeItem>` on .NET
			// (the .NET projection of the native `IVectorView<SwipeItem>`).
			IReadOnlyList<SwipeItem> view = SUT.GetView();

			Assert.IsNotNull(view);
		}

		[TestMethod]
		public void When_ItemsEmpty_Then_GetViewIsEmpty()
		{
			var SUT = new SwipeItems();

			var view = SUT.GetView();

			Assert.IsNotNull(view);
			Assert.AreEqual(0, view.Count);
		}

		[TestMethod]
		public void When_ItemAppendedAfterGetView_Then_ViewReflectsLiveState()
		{
			// WinUI's SwipeItems::GetView() forwards to the backing Vector<T>'s GetView(),
			// which is a live view over the same underlying storage (not a point-in-time copy).
			var item1 = new SwipeItem { Text = "Item1" };
			var item2 = new SwipeItem { Text = "Item2" };

			var SUT = new SwipeItems { item1 };

			var view = SUT.GetView();
			Assert.AreEqual(1, view.Count);

			SUT.Add(item2);

			Assert.AreEqual(2, view.Count);
			Assert.AreSame(item2, view[1]);
		}

		[TestMethod]
		public void When_GetView_Then_CannotBeDowncastToMutateTheCollection()
		{
			// The native WinRT IVectorView<T> is a distinct object that cannot be cast back to a
			// mutable IVector<T>. The .NET port must preserve this: a caller must not be able to
			// downcast the returned view to bypass SwipeItems' own validation (e.g. the
			// SwipeMode.Execute single-item constraint) or its VectorChanged notifications.
			var SUT = new SwipeItems { new SwipeItem { Text = "Item1" } };

			var view = SUT.GetView();

			Assert.IsNotInstanceOfType<ObservableCollection<SwipeItem>>(view);
			Assert.ThrowsExactly<NotSupportedException>(() => ((IList<SwipeItem>)view).Add(new SwipeItem()));
			Assert.AreEqual(1, SUT.Count);
		}
	}
}
