using CommonServiceLocator;
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
