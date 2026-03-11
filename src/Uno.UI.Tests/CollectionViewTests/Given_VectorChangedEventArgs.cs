using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.CollectionViewTests
{
	[TestClass]
	public class Given_VectorChangedEventArgs
	{
		[TestMethod]
		public void When_Converting_Args()
		{
			var add = new VectorChangedEventArgs(CollectionChange.ItemInserted, 12);
			var addC = add.ToNotifyCollectionChangedEventArgs();
			Assert.AreEqual((int)add.Index, addC.NewStartingIndex);
			Assert.HasCount(1, addC.NewItems);

			var remove = new VectorChangedEventArgs(CollectionChange.ItemRemoved, 15);
			var removeC = remove.ToNotifyCollectionChangedEventArgs();
			Assert.AreEqual((int)remove.Index, removeC.OldStartingIndex);
			Assert.HasCount(1, removeC.OldItems);

			var replace = new VectorChangedEventArgs(CollectionChange.ItemChanged, 3);
			var replaceC = replace.ToNotifyCollectionChangedEventArgs();
			Assert.AreEqual((int)replace.Index, replaceC.NewStartingIndex);
			Assert.HasCount(1, replaceC.NewItems);
			Assert.HasCount(1, replaceC.OldItems);

			var reset = new VectorChangedEventArgs(CollectionChange.Reset, 0);
			var resetC = reset.ToNotifyCollectionChangedEventArgs();
		}
	}
}
