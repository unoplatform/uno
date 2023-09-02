using CommonServiceLocator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
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
using Windows.UI.Xaml;

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
		public void When_Add_After_DataContext()
		{
			var SUT = new DependencyObjectCollection();
			SUT.DataContext = 42;

			var o1 = new MyDependencyObject();
			o1.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, o1.MyProperty);

			SUT.Add(o1);

			Assert.AreEqual(42, o1.MyProperty);
		}

		[TestMethod]
		public void When_Add_Before_DataContext()
		{
			var SUT = new DependencyObjectCollection();

			var o1 = new MyDependencyObject();
			o1.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, o1.MyProperty);

			SUT.Add(o1);

			Assert.AreEqual(0, o1.MyProperty);

			SUT.DataContext = 42;

			Assert.AreEqual(42, o1.MyProperty);
		}

		[TestMethod]
		public void When_DataContext_Changed_Multiple()
		{
			var SUT = new DependencyObjectCollection();
			SUT.DataContext = 41;

			var o1 = new MyDependencyObject();
			o1.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			var o2 = new MyDependencyObject();
			o2.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, o1.MyProperty);
			Assert.AreEqual(0, o1.MyProperty);

			SUT.Add(o1);
			SUT.Add(o2);

			Assert.AreEqual(41, o1.MyProperty);
			Assert.AreEqual(41, o2.MyProperty);

			SUT.DataContext = 42;

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(42, o2.MyProperty);

			SUT.Clear();

			Assert.AreEqual(0, o1.MyProperty);
			Assert.AreEqual(0, o2.MyProperty);
		}

		[TestMethod]
		public void When_Changed_By_Indexer()
		{
			var SUT = new DependencyObjectCollection();
			SUT.DataContext = 42;

			var o1 = new MyDependencyObject();
			o1.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			var o2 = new MyDependencyObject();
			o2.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, o1.MyProperty);
			Assert.AreEqual(0, o1.MyProperty);

			SUT.Add(o1);
			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(0, o2.MyProperty);

			SUT[0] = o2;

			Assert.AreEqual(0, o1.MyProperty);
			Assert.AreEqual(42, o2.MyProperty);
		}

		[TestMethod]
		public void When_Removed()
		{
			var SUT = new DependencyObjectCollection();
			SUT.DataContext = 42;

			var o1 = new MyDependencyObject();
			o1.SetBinding(MyDependencyObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, o1.MyProperty);

			SUT.Add(o1);

			Assert.AreEqual(42, o1.MyProperty);

			SUT.Remove(o1);

			Assert.AreEqual(0, o1.MyProperty);
		}

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
