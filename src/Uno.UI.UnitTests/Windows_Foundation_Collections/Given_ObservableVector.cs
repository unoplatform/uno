using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.Windows_Foundation_Collections
{
	[UITestClass]
	public class Given_ObservableVector
	{
		[TestMethod]
		public void When_Item_Added_VectorChanged()
		{
			var vector = new ObservableVector<string>() { "one", "two", "three" };
			var count = 0;
			vector.VectorChanged += (o, e) =>
			  {
				  count++;
				  switch (count)
				  {
					  case 1:
						  Assert.AreEqual(3u, e.Index);
						  Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
						  break;
					  case 2:
						  Assert.AreEqual(0u, e.Index);
						  Assert.AreEqual(CollectionChange.ItemInserted, e.CollectionChange);
						  break;
					  default:
						  Assert.Fail("Raised too many times");
						  break;
				  }
			  };

			vector.Add("four");
			vector.Insert(0, "zero");
		}

		[TestMethod]
		public void When_Item_Removed_VectorChanged()
		{
			var vector = new ObservableVector<string>() { "one", "two", "three" };
			var count = 0;
			vector.VectorChanged += (o, e) =>
			{
				count++;
				switch (count)
				{
					case 1:
						Assert.AreEqual(2u, e.Index);
						Assert.AreEqual(CollectionChange.ItemRemoved, e.CollectionChange);
						break;
					case 2:
						Assert.AreEqual(0u, e.Index);
						Assert.AreEqual(CollectionChange.ItemRemoved, e.CollectionChange);
						break;
					default:
						Assert.Fail("Raised too many times");
						break;
				}
			};

			vector.RemoveAt(2);
			vector.Remove("one");
		}

		[TestMethod]
		public void When_Item_Set_VectorChanged()
		{
			var vector = new ObservableVector<string>() { "one", "two", "three" };
			var count = 0;
			vector.VectorChanged += (o, e) =>
			{
				count++;
				Assert.AreEqual(1u, e.Index);
				Assert.AreEqual(CollectionChange.ItemChanged, e.CollectionChange);
			};

			vector[1] = "TWO";

			Assert.AreEqual(1, count);
			Assert.AreEqual("TWO", vector[1]);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25238")]
		public void When_Same_Item_Set_VectorChanged()
		{
			var two = "two";
			var vector = new ObservableVector<string>() { "one", two, "three" };
			var typedCount = 0;
			var untypedCount = 0;
			vector.VectorChanged += (o, e) =>
			{
				typedCount++;
				Assert.AreEqual(1u, e.Index);
				Assert.AreEqual(CollectionChange.ItemChanged, e.CollectionChange);
			};
			vector.UntypedVectorChanged += (o, e) =>
			{
				untypedCount++;
				Assert.AreEqual(1u, e.Index);
				Assert.AreEqual(CollectionChange.ItemChanged, e.CollectionChange);
			};

			vector[1] = two;
			((IList)vector)[1] = two;

			Assert.AreEqual(2, typedCount);
			Assert.AreEqual(2, untypedCount);
		}

		[TestMethod]
		public void When_IListOverride_IndexOf()
		{
			var vector = new MyObservableVector();
			vector.Add(21);
			vector.Add(42);

			var vectorAsIList = (IList)vector;

			Assert.AreEqual(1, vectorAsIList.IndexOf(42));

			Assert.AreEqual(2, vector.IndexGetCount);
		}

		private class MyObservableVector : ObservableVector<int>
		{
			public int IndexGetCount;
			public int IndexSetCount;

			public MyObservableVector()
			{

			}

			public override int this[int index]
			{
				get
				{
					IndexGetCount++;
					return base[index];
				}

				set
				{
					IndexSetCount++;
					base[index] = value;
				}
			}
		}
	}
}
