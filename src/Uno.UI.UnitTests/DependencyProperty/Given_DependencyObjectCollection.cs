using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Uno.Disposables;
using System.ComponentModel;
using Uno.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_DependencyObjectCollection
	{
		//[TestCleanup]
		//public void Cleanup()
		//{
		//	DependencyProperty.ClearRegistry();
		//}

		//[TestInitialize]
		//public void Initialize()
		//{
		//	DependencyProperty.ClearRegistry();
		//}

		[TestMethod]
		public void When_Add_CollectionChanged()
		{
			var SUT = new MyDependencyObjectCollection
			{
				new MyDependencyObject(),
				new MyDependencyObject(),
				new MyDependencyObject()
			};

			Assert.AreEqual(3, SUT.CollectionChangedCount);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		[DataRow(1, 1)]
		[DataRow(2, 1)]
		[DataRow(1, 2)]
		[DataRow(0, 1)]
		[DataRow(1, 0)]
		public void When_ReplaceRange_Mutates_Then_Indexed_Walk_Is_Invalidated(int count, int replacementCount)
		{
			var collection = new DependencyObjectCollection<MyDependencyObject>
			{
				new MyDependencyObject(),
				new MyDependencyObject(),
			};
			var replacement = Enumerable.Range(0, replacementCount).Select(_ => new MyDependencyObject()).ToArray();
			var version = collection.ItemsVersion;
			var notified = false;
			collection.VectorChanged += (_, _) =>
			{
				Assert.IsGreaterThan(version, collection.ItemsVersion);
				notified = true;
			};

			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				var items = new DependencyObjectItems(collection);
				Assert.IsTrue(items.MoveNext());
				collection.ReplaceRange(0, count, replacement);
				items.MoveNext();
			});
			Assert.IsTrue(notified);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		public void When_ReplaceRange_Does_Not_Mutate_Then_Indexed_Walk_Remains_Valid()
		{
			var item = new MyDependencyObject();
			var collection = new DependencyObjectCollection<MyDependencyObject>
			{
				item,
				new MyDependencyObject(),
			};
			var version = collection.ItemsVersion;
			var items = new DependencyObjectItems(collection);
			Assert.IsTrue(items.MoveNext());

			collection.ReplaceRange(0, 0, Array.Empty<MyDependencyObject>());
			collection.ReplaceRange(0, 1, new[] { item });

			Assert.AreEqual(version, collection.ItemsVersion);
			Assert.IsTrue(items.MoveNext());
			Assert.IsFalse(items.MoveNext());
		}

		public partial class MyDependencyObject : DependencyObject
		{
			public int MyProperty
			{
				get { return (int)GetValue(MyPropertyProperty); }
				set { SetValue(MyPropertyProperty, value); }
			}

			// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty MyPropertyProperty =
				DependencyProperty.Register("MyProperty", typeof(int), typeof(MyDependencyObject), new FrameworkPropertyMetadata(0));
		}

		private class MyDependencyObjectCollection : DependencyObjectCollection
		{
			public int CollectionChangedCount;

			private protected override void OnCollectionChanged()
			{
				base.OnCollectionChanged();

				CollectionChangedCount++;
			}
		}
	}
}
