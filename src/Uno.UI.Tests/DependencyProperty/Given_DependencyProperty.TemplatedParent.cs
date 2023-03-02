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

namespace Uno.UI.Tests.BinderTests_TemplatedParent
{
	[TestClass]
	public partial class Given_DependencyProperty_TemplatedParent
	{
		[TestMethod]
		public void When_TemplatedParent_Loop()
		{
			var SUT = new MyObject() { Name = "root" };
			var level1 = new MyObject() { Name = "level1" };
			var level2 = new MyObject() { Name = "level2" };
			var level3 = new MyObject() { Name = "level3" };

			level2.DataContext = null;

			SUT.InnerObject = level1;
			level1.InnerObject = level2;
			level2.InnerObject = level3;
			level2.TemplatedParent = SUT;

			level2.DataContext = 43;

			Assert.IsNull(SUT.DataContext);
		}

		[TestMethod]
		public void When_ValueInheritsDataContext_Then_TemplatedParent_is_Propagated()
		{
			var SUT = new MyObject() { Name = "root" };
			var inner = new MyObject() { Name = "inner" };

			SUT.InnerWithValueInheritsDataContext = inner;

			Assert.IsNull(inner.TemplatedParent);

			SUT.TemplatedParent = SUT;

			Assert.IsNotNull(inner.TemplatedParent);

			SUT.TemplatedParent = null;

			Assert.IsNull(inner.TemplatedParent);
		}

		[TestMethod]
		public void When_DataContextTemplateBinding()
		{
			var SUT = new MyObject() { Name = "root" };
			var inner = new MyObject() { Name = "inner" };

			var tp = new MyObject() { Name = "tp", Tag = 42 };

			SUT.InnerObject = inner;

			Assert.IsNull(inner.DataContext);

			inner.SetBinding(
				MyObject.DataContextProperty,
				new Binding
				{
					Path = new PropertyPath("Tag"),
					RelativeSource = RelativeSource.TemplatedParent
				}
			);

			SUT.TemplatedParent = tp;

			Assert.AreEqual(42, inner.DataContext);

			SUT.DataContext = 44;

			tp.Tag = 43;
			Assert.AreEqual(43, inner.DataContext);

			var tp2 = new MyObject() { Name = "tp2", Tag = 44 };
			SUT.TemplatedParent = tp2;

			Assert.AreEqual(44, inner.DataContext);
		}

	}

	public partial class MyObject : FrameworkElement
	{
		public MyObject InnerWithValueInheritsDataContext
		{
			get { return (MyObject)GetValue(InnerWithDataContextValueProperty); }
			set { SetValue(InnerWithDataContextValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for InnerWithDataContextValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerWithDataContextValueProperty =
			DependencyProperty.Register(
				name: "InnerWithValueInheritsDataContext",
				propertyType: typeof(MyObject),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext
				)
			);

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
}
