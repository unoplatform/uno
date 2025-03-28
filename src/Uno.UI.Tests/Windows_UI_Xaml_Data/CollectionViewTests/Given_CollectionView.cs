using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.CollectionViewTests
{
	[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
	public class Given_CollectionView
	{
		[TestMethod]
		public void When_CopyTo()
		{
			var originalList = new List<object> { 1, 2, 3 };
			var SUT = new CollectionView(originalList, false, null);

			var array = new object[3];
			(SUT as ICollection<object>).CopyTo(array, 0);

			Assert.IsTrue(array.SequenceEqual(originalList));
		}

		[TestMethod]
		public void When_CopyTo_With_Index()
		{
			var originalList = new List<object> { 1, 2, 3 };
			var SUT = new CollectionView(originalList, false, null);

			var array = new object[4];
			array[0] = 42;
			(SUT as ICollection<object>).CopyTo(array, 1);

			Assert.IsTrue(array.Skip(1).SequenceEqual(originalList));
			Assert.AreEqual(42, array[0]);
		}

		[TestMethod]
		public void When_Array_CopyTo()
		{
			var originalList = new object[] { 1, 2, 3 };
			var SUT = new CollectionView(originalList, false, null);

			var array = new object[3];
			(SUT as ICollection<object>).CopyTo(array, 0);

			Assert.IsTrue(array.SequenceEqual(originalList));
		}

		[TestMethod]
		public void When_Array_CopyTo_With_Index()
		{
			var originalList = new object[] { 1, 2, 3 };
			var SUT = new CollectionView(originalList, false, null);

			var array = new object[4];
			array[0] = 42;
			(SUT as ICollection<object>).CopyTo(array, 1);

			Assert.IsTrue(array.Skip(1).SequenceEqual(originalList));
			Assert.AreEqual(42, array[0]);
		}
	}
}
