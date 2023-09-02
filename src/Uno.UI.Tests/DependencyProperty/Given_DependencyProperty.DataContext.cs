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
using System.Threading;
using Windows.UI.Xaml.Controls;
using System.Collections;

namespace Uno.UI.Tests.BinderTests_DataContext
{
	[TestClass]
	public partial class Given_DependencyProperty
	{
		[TestMethod]
		public void When_SimpleInheritance()
		{
			var SUT = new DependencyObjectCollection();
			var level1 = new DependencyObjectCollection();
			var level2 = new MyObject();

			SUT.Add(level1);
			level1.Add(level2);

			SUT.DataContext = 42;

			Assert.AreEqual(42, level2.DataContext);
		}

		[TestMethod]
		public void When_DataContext_Binding()
		{
			var SUT = new MyObject();
			var level1 = new MyObject();
			var level2 = new MyObject();

			SUT.InnerObject = level1;
			level1.InnerObject = level2;

			SUT.DataContext = new { a = 42 };
			Assert.AreEqual(SUT.DataContext, level1.DataContext);
			Assert.AreEqual(SUT.DataContext, level2.DataContext);

			level1.SetBinding(MyObject.DataContextProperty, new Binding() { Path = new PropertyPath("a") });

			Assert.AreEqual(42, level1.DataContext);
			Assert.AreEqual(42, level2.DataContext);

			SUT.DataContext = null;
			Assert.IsNull(level1.DataContext);
			Assert.IsNull(level2.DataContext);
		}

		[TestMethod]
		public void When_DataContext_Binding_Two_Levels()
		{
			var SUT = new MyObject();
			var level1 = new MyObject();
			var level2 = new MyObject();
			var level3 = new MyObject();

			SUT.InnerObject = level1;
			level1.InnerObject = level2;
			level2.InnerObject = level3;

			var dc = new { a = new { b = 42 } };
			var dca = dc.a;

			SUT.DataContext = dc;

			Assert.AreEqual(SUT.DataContext, level1.DataContext);
			Assert.AreEqual(SUT.DataContext, level2.DataContext);
			Assert.AreEqual(SUT.DataContext, level3.DataContext);

			level1.SetBinding(MyObject.DataContextProperty, new Binding() { Path = new PropertyPath("a") });

			Assert.AreEqual(dca, level1.DataContext);
			Assert.AreEqual(dca, level2.DataContext);
			Assert.AreEqual(dca, level3.DataContext);

			level2.SetBinding(MyObject.DataContextProperty, new Binding() { Path = new PropertyPath("b") });

			Assert.AreEqual(dca, level1.DataContext);
			Assert.AreEqual(42, level2.DataContext);
			Assert.AreEqual(42, level3.DataContext);

			SUT.DataContext = null;
			Assert.IsNull(level1.DataContext);
			Assert.IsNull(level2.DataContext);
			Assert.IsNull(level3.DataContext);
		}

		[TestMethod]
		public void When_Parent_Generic()
		{
			var SUT = new MyObject();
			SUT.DataContext = 42;

			var inner = new MyObject();
			var genericParent = new object();
			inner.SetParent(genericParent);

			Assert.AreEqual(genericParent, inner.GetParent());

			SUT.InnerObject = inner;
			Assert.AreEqual(42, inner.DataContext);
		}

		[TestMethod]
		public void When_Object_ChangesParent()
		{
			var SUT = new MyObject();
			var SUT2 = new MyObject();

			var item1 = new MyObject();
			SUT.InnerObject = item1;

			Assert.IsNull(item1.DataContext);

			SUT.DataContext = 42;

			Assert.AreEqual(42, item1.DataContext);

			SUT2.InnerObject = item1;
			Assert.IsNull(item1.DataContext);

			SUT2.DataContext = 43;

			Assert.AreEqual(43, item1.DataContext);
		}

		[TestMethod]
		public void When_Object_ChangesParent_With_Detach()
		{
			var SUT = new MyObject();
			var SUT2 = new MyObject();

			var item1 = new MyObject();
			SUT.InnerObject = item1;

			Assert.IsNull(item1.DataContext);

			SUT.DataContext = 42;

			Assert.AreEqual(42, item1.DataContext);

			SUT.InnerObject = null;
			item1.SetParent(null);
			Assert.IsNull(item1.DataContext);

			SUT2.InnerObject = item1;
			Assert.IsNull(item1.DataContext);

			SUT2.DataContext = 43;

			Assert.AreEqual(43, item1.DataContext);
		}

		[TestMethod]
		public void When_ValueInheritDataContext_And_IList()
		{
			var SUT = new MyBasicListType();
			var sub1 = new MyObject();
			var templatedParent = new Grid();

			SUT.MyList = new List<MyObject>() { sub1 };

			SUT.DataContext = 42;
			SUT.TemplatedParent = templatedParent;

			Assert.AreEqual(42, sub1.DataContext);
			Assert.AreEqual(templatedParent, sub1.TemplatedParent);
		}

		[TestMethod]
		public void When_ValueInheritDataContext_And_IList_Non_Enumerable()
		{
			var SUT = new MyBasicListType();
			var sub1 = new MyObject();
			var templatedParent = new Grid();

			SUT.MyList = new NonEnumerableList<MyObject>() { sub1 };

			SUT.DataContext = 42;
			SUT.TemplatedParent = templatedParent;

			Assert.AreEqual(42, sub1.DataContext);
			Assert.AreEqual(templatedParent, sub1.TemplatedParent);
		}

		[TestMethod]
		public void When_DataContext_Inherited_And_Parent_Changed()
		{
			var SUT = new Border();
			var parent1 = new Border();
			parent1.DataContext = 10;
			var parent2 = new Border();
			parent2.DataContext = 42;
			SUT.SetParent(parent1);
			Assert.AreEqual(10, SUT.DataContext);
			SUT.SetParent(parent2);
			Assert.AreEqual(42, SUT.DataContext);

		}

		[TestMethod]
		public void When_DataContext_Inherited_And_Child_Removed()
		{
			var SUT = new MyControl();

			var independentChild = new MyControl();
			var parent = new Grid
			{
				Children = { independentChild, SUT },
				DataContext = 42
			};

			// Here we use the "FrameworkProperty" which is expected to inherit the DataContext by default
			SUT.InnerFramework = independentChild;

			int parentCtxChanged = 0, childCtxChanged = 0, SUTCtxChanged = 0;
			parent.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => parentCtxChanged++);
			independentChild.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => childCtxChanged++);
			SUT.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => SUTCtxChanged++);

			Assert.AreEqual(42, SUT.DataContext);

			// And here the issue: when we remove the SUT from the parent,
			// it will propagate to the independentChild the DataContext change ...
			// which is acceptable (but not really expected neither) as the DP is a FrameworkProperty on which we want to propagate the DataContext
			parent.Children.Remove(SUT);

			Assert.AreEqual(null, SUT.DataContext);
			Assert.AreEqual(0, parentCtxChanged);
			Assert.AreEqual(1, SUTCtxChanged);
			Assert.AreEqual(0, childCtxChanged);
		}

		[TestMethod]
		public void When_DataContext_NotInherited_And_Child_Removed()
		{
			var SUT = new MyControl();

			var independentChild = new MyControl();
			var parent = new Grid
			{
				Children = { independentChild, SUT },
				DataContext = 42
			};

			// Here we use the standard (a.k.a. Application) DP which is NOT expected to propagate the DataContext
			SUT.InnerStandard = independentChild;

			int parentCtxChanged = 0, childCtxChanged = 0, SUTCtxChanged = 0;
			parent.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => parentCtxChanged++);
			independentChild.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => childCtxChanged++);
			SUT.RegisterPropertyChangedCallback(UIElement.DataContextProperty, (snd, dp) => SUTCtxChanged++);

			Assert.AreEqual(42, SUT.DataContext);

			parent.Children.Remove(SUT);

			Assert.AreEqual(null, SUT.DataContext);
			Assert.AreEqual(0, parentCtxChanged);
			Assert.AreEqual(1, SUTCtxChanged);
			Assert.AreEqual(0, childCtxChanged);
		}
	}

	public partial class MyBasicListType : DependencyObject
	{

		#region MyList DependencyProperty

		public IList<MyObject> MyList
		{
			get { return (IList<MyObject>)GetValue(MyListProperty); }
			set { SetValue(MyListProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyList.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyListProperty =
			DependencyProperty.Register(
				name: "MyList",
				propertyType: typeof(IList<MyObject>),
				ownerType: typeof(MyBasicListType),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
					propertyChangedCallback: (s, e) => ((MyBasicListType)s)?.OnMyListChanged(e)
				)
			);


		private void OnMyListChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

	}

	public partial class MyObject : DependencyObject
	{

		#region InnerObject DependencyProperty

		public MyObject InnerObject
		{
			get { return (MyObject)GetValue(InnerObjectProperty); }
			set { SetValue(InnerObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for InnerObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerObjectProperty =
			DependencyProperty.Register(
				name: "InnerObject",
				propertyType: typeof(MyObject),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild,
					propertyChangedCallback: (s, e) => ((MyObject)s)?.OnInnerObjectChanged(e)
				)
			);


		private void OnInnerObjectChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion
	}

	public partial class MyControl : Control
	{
		// Just a standard DP defined by a project / third party component
		public static readonly DependencyProperty InnerStandardProperty = DependencyProperty.Register(
			"InnerStandard", typeof(MyControl), typeof(MyControl), new PropertyMetadata(default(MyControl)));

		public MyControl InnerStandard
		{
			get { return (MyControl)GetValue(InnerStandardProperty); }
			set { SetValue(InnerStandardProperty, value); }
		}

		public static readonly DependencyProperty InnerFrameworkProperty = DependencyProperty.Register(
			"InnerFramework", typeof(MyControl), typeof(MyControl), new FrameworkPropertyMetadata(default(MyControl)));

		public MyControl InnerFramework
		{
			get { return (MyControl)GetValue(InnerFrameworkProperty); }
			set { SetValue(InnerFrameworkProperty, value); }
		}
	}

	class NonEnumerableList<T> : IList<T>, IList
	{
		private List<T> _internal = new List<T>();

		public T this[int index] { get => _internal[index]; set => _internal[index] = value; }

		object IList.this[int index] { get => _internal[index]; set => _internal[index] = (T)value; }

		public int Count => _internal.Count;

		public bool IsReadOnly => false;

		public bool IsFixedSize => false;

		public object SyncRoot { get; } = new object();

		public bool IsSynchronized => false;

		public void Add(T item) => _internal.Add(item);

		public int Add(object value) => throw new NotImplementedException();
		public void Clear() => _internal.Clear();
		public bool Contains(T item) => _internal.Contains(item);
		public bool Contains(object value) => throw new NotImplementedException();
		public void CopyTo(T[] array, int arrayIndex) => _internal.CopyTo(array, arrayIndex);
		public void CopyTo(Array array, int index) => throw new NotImplementedException();
		public IEnumerator<T> GetEnumerator() => throw new NotSupportedException();
		public int IndexOf(T item) => _internal.IndexOf(item);
		public int IndexOf(object value) => throw new NotImplementedException();
		public void Insert(int index, T item) => _internal.Insert(index, item);
		public void Insert(int index, object value) => throw new NotImplementedException();
		public bool Remove(T item) => _internal.Remove(item);
		public void Remove(object value) => throw new NotImplementedException();
		public void RemoveAt(int index) => _internal.RemoveAt(index);
		IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
	}
}
