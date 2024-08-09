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
using Uno.UI.Converters;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests.ManualPropagation
{
	[TestClass]
	public partial class Given_DependencyProperty_ManualPropagation
	{
		[TestInitialize]
		public void Init()
		{

			global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SubObject).TypeHandle);
			global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(MyObject).TypeHandle);
		}

		public void TestInitialize()
		{

		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Disabled()
		{
			var SUT = new MyObject();
			var inner = new SubObject();

			SUT.SubObject = inner;

			Assert.IsNull(inner.LastDataContextChangedValue);

			SUT.DataContext = 42;

			Assert.IsNull(inner.LastDataContextChangedValue);
			Assert.AreEqual("", inner.MyStringProperty);

			inner.SetBinding(
				SubObject.MyStringPropertyProperty,
				new Binding()
				{
				}
			);

			Assert.AreEqual(42, inner.LastDataContextChangedValue);
			Assert.AreEqual("42", inner.MyStringProperty);
		}

		[TestMethod]
		public void When_PropertyReset_Then_Parent_removed()
		{
			var SUT = new MyObject();
			var inner = new SubObject();

			SUT.SubObject = inner;

			Assert.AreEqual(SUT, inner.GetParent());

			SUT.SubObject = null;
			Assert.IsNull(inner.GetParent());
		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Disabled_TemplatedParent()
		{
			var SUT = new MyObject();
			var inner = new SubObject();

			SUT.SubObject = inner;

			Assert.IsNull(inner.LastDataContextChangedValue);

			var parent = new Grid();
			SUT.TemplatedParent = parent;

			Assert.IsNull(inner.LastTemplatedParentChangedValue);

			Assert.AreEqual(parent, inner.TemplatedParent);
			Assert.AreEqual(parent, inner.LastTemplatedParentChangedValue);
		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Disabled_TemplatedParent_Binding()
		{
			var SUT = new MyObject();
			var inner = new SubObject();

			SUT.SubObject = inner;

			Assert.IsNull(inner.LastDataContextChangedValue);

			var parent = new Grid() { Tag = 42 };
			SUT.TemplatedParent = parent;

			Assert.IsNull(inner.LastTemplatedParentChangedValue);

			inner.SetBinding(
				SubObject.DataContextProperty,
				new Binding()
				{
					Path = "Tag",
					RelativeSource = RelativeSource.TemplatedParent
				}
			);

			Assert.AreEqual(parent, inner.LastTemplatedParentChangedValue);
			Assert.AreEqual(42, inner.DataContext);
		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Multi_Disabled()
		{
			var SUT = new MyObject();
			var inner = new SubObject();
			var inner2 = new SubObject();

			SUT.SubObject = inner;
			inner.Inner = inner2;

			Assert.IsNull(inner.LastDataContextChangedValue);

			SUT.DataContext = 42;

			Assert.IsNull(inner.LastDataContextChangedValue);
			Assert.AreEqual("", inner.MyStringProperty);
			Assert.AreEqual("", inner2.MyStringProperty);

			inner2.SetBinding(
				SubObject.MyStringPropertyProperty,
				new Binding()
				{
				}
			);

			Assert.AreEqual(42, inner.LastDataContextChangedValue);
			Assert.AreEqual(42, inner2.LastDataContextChangedValue);
			Assert.AreEqual("", inner.MyStringProperty);
			Assert.AreEqual("42", inner2.MyStringProperty);
		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Multi_DataContext_Read_Top()
		{
			var SUT = new MyObject();
			var inner = new SubObject();
			var inner2 = new SubObject();

			SUT.SubObject = inner;
			inner.Inner = inner2;

			Assert.IsNull(inner.LastDataContextChangedValue);

			SUT.DataContext = 42;

			Assert.IsNull(inner.LastDataContextChangedValue);
			Assert.IsNull(inner2.LastDataContextChangedValue);

			// The order is important
			Assert.AreEqual(42, inner2.DataContext);
			Assert.AreEqual(42, inner.DataContext);

			Assert.AreEqual(42, inner.LastDataContextChangedValue);
			Assert.AreEqual(42, inner2.LastDataContextChangedValue);
		}

		[TestMethod]
		public void When_IsAutoPropertyInheritance_Multi_DataContext_Read_Bottom()
		{
			var SUT = new MyObject();
			var inner = new SubObject();
			var inner2 = new SubObject();

			SUT.SubObject = inner;
			inner.Inner = inner2;

			Assert.IsNull(inner.LastDataContextChangedValue);

			SUT.DataContext = 42;

			Assert.IsNull(inner.LastDataContextChangedValue);
			Assert.IsNull(inner2.LastDataContextChangedValue);

			// The order is important
			Assert.AreEqual(42, inner.DataContext);
			Assert.AreEqual(42, inner.LastDataContextChangedValue);
			Assert.IsNull(inner2.LastDataContextChangedValue);

			Assert.AreEqual(42, inner2.DataContext);
			Assert.AreEqual(42, inner2.LastDataContextChangedValue);
		}

		[TestMethod]
		public void When_StateTrigger_DataBound()
		{
			var SUT = new UserControl();
			var mgr = VisualStateManager.GetVisualStateManager(SUT);


			var group = new VisualStateGroup();

			var state = new VisualState();

			var trigger = new StateTrigger();
			trigger.SetBinding(StateTrigger.IsActiveProperty, new Binding() { Path = "a" });
			state.StateTriggers.Add(trigger);

			group.States.Add(state);

			var groups = new List<VisualStateGroup>();
			groups.Add(group);
			VisualStateManager.SetVisualStateGroups(SUT, groups);

			Assert.IsNull(trigger.DataContext);
			Assert.IsFalse(trigger.IsActive);

			SUT.DataContext = new { a = true };

			Assert.IsNotNull(group.DataContext);
			Assert.IsNotNull(state.DataContext);
			Assert.IsNotNull(trigger.DataContext);
			Assert.IsTrue(trigger.IsActive);
		}

		[TestMethod]
		public void When_StateTrigger_DataBound_Late()
		{
			var SUT = new UserControl();
			var mgr = VisualStateManager.GetVisualStateManager(SUT);

			var group = new VisualStateGroup();

			var state = new VisualState();
			group.States.Add(state);

			var trigger = new StateTrigger();

			trigger.SetBinding(StateTrigger.IsActiveProperty, new Binding() { Path = "a" });

			var groups = new List<VisualStateGroup>();

			groups.Add(group);
			state.StateTriggers.Add(trigger);
			VisualStateManager.SetVisualStateGroups(SUT, groups);

			Assert.IsNull(trigger.DataContext);
			Assert.IsFalse(trigger.IsActive);

			SUT.DataContext = new { a = true };

			Assert.IsNotNull(group.DataContext);
			Assert.IsNotNull(state.DataContext);
			Assert.IsNotNull(trigger.DataContext);
			Assert.IsTrue(trigger.IsActive);
		}

		[TestMethod]
		public void When_StateTrigger_DataBound_Early()
		{
			var group = new VisualStateGroup();
			group.DataContext = new { a = true };

			var state = new VisualState();
			object stateDataContextValue = null;
			state.DataContextChanged += (_, e) =>
			{
				stateDataContextValue = e.NewValue;
			};

			var trigger = new StateTrigger();
			state.StateTriggers.Add(trigger);

			group.States.Add(state);

			Assert.IsNull(stateDataContextValue);
			Assert.IsFalse(trigger.IsActive);

			trigger.SetBinding(StateTrigger.IsActiveProperty, new Binding() { Path = "a" });

			Assert.IsNotNull(stateDataContextValue);
			Assert.IsTrue(trigger.IsActive);
		}

		[TestMethod]
		public void When_Grid_DataBound_Setters()
		{
			var grid = new Grid();
			grid.DataContext = new { a = "42", b = "43" };

			var columnDefinition = new ColumnDefinition();

			object columnDefinitionDataContextValue = null;
			columnDefinition.DataContextChanged += (_, e) =>
			{
				columnDefinitionDataContextValue = e.NewValue;
			};

			grid.ColumnDefinitions.Add(columnDefinition);

			var rowDefinition = new RowDefinition();

			object rowDefinitionDataContextValue = null;
			rowDefinition.DataContextChanged += (_, e) =>
			{
				rowDefinitionDataContextValue = e.NewValue;
			};

			grid.RowDefinitions.Add(rowDefinition);

			Assert.IsNull(rowDefinitionDataContextValue);
			Assert.AreEqual(GridUnitType.Star, columnDefinition.Width.GridUnitType);

			Assert.IsNull(columnDefinitionDataContextValue);
			Assert.AreEqual(GridUnitType.Star, rowDefinition.Height.GridUnitType);

			columnDefinition.SetBinding(ColumnDefinition.WidthProperty, new Binding() { Path = "a" });

			Assert.IsNotNull(columnDefinitionDataContextValue);
			Assert.AreEqual(GridUnitType.Pixel, columnDefinition.Width.GridUnitType);
			Assert.AreEqual(42, columnDefinition.Width.Value);

			Assert.IsNull(rowDefinitionDataContextValue);
			Assert.AreEqual(GridUnitType.Star, rowDefinition.Height.GridUnitType);

			rowDefinition.SetBinding(RowDefinition.HeightProperty, new Binding() { Path = "b" });

			Assert.IsNotNull(rowDefinitionDataContextValue);
			Assert.AreEqual(GridUnitType.Pixel, rowDefinition.Height.GridUnitType);
			Assert.AreEqual(43, rowDefinition.Height.Value);
		}

		[TestMethod]
		public void When_SolidColorBrush_DataBound_Setters()
		{
			var grid = new Grid();

			var brush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);

			object brushDataContextValue = null;
			brush.DataContextChanged += (_, e) =>
			{
				brushDataContextValue = e.NewValue;
			};

			grid.Background = brush;

			grid.DataContext = new { a = "#FF00ff00" };

			brush.SetBinding(Windows.UI.Xaml.Media.SolidColorBrush.ColorProperty, new Binding { Path = "a" });

			Assert.IsNotNull(brushDataContextValue);
			Assert.AreEqual(Windows.UI.Colors.Lime, brush.Color);
		}
	}

	public partial class CompositeTrigger : StateTriggerBase
	{
		#region TriggerCollection DependencyProperty

		public DependencyObjectCollection TriggerCollection
		{
			get => (DependencyObjectCollection)GetValue(TriggerCollectionProperty);
			set => SetValue(TriggerCollectionProperty, value);
		}

		// Using a DependencyProperty as the backing store for TriggerCollection.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TriggerCollectionProperty =
			DependencyProperty.Register(
				"TriggerCollection",
				typeof(DependencyObjectCollection),
				typeof(CompositeTrigger),
				new PropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((CompositeTrigger)s)?.OnTriggerCollectionChanged(e)
				)
			);


		private void OnTriggerCollectionChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion
	}



	public partial class MyObject : DependencyObject
	{
		public MyObject()
		{

		}

		#region SubObject DependencyProperty

		public SubObject SubObject
		{
			get { return (SubObject)GetValue(SubObjectProperty); }
			set { SetValue(SubObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SubObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SubObjectProperty =
			DependencyProperty.Register(
				"SubObject",
				typeof(SubObject),
				typeof(MyObject),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild,
					propertyChangedCallback: (s, e) => ((MyObject)s)?.OnSubObjectChanged(e)
				)
			);


		private void OnSubObjectChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion
	}

	public partial class SubObject : DependencyObject
	{
		public SubObject()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		public object LastDataContextChangedValue { get; private set; }

		public object LastTemplatedParentChangedValue { get; private set; }

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			LastDataContextChangedValue = e.NewValue;
		}

		partial void OnTemplatedParentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			LastTemplatedParentChangedValue = e.NewValue;
		}

		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		public int MyPropertyCounter { get; private set; }
		public int MyStringPropertyCounter { get; private set; }

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(SubObject), new FrameworkPropertyMetadata(0, OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is SubObject so)
			{
				so.MyPropertyCounter++;
			}
		}

		public string MyStringProperty
		{
			get { return (string)GetValue(MyStringPropertyProperty); }
			set { SetValue(MyStringPropertyProperty, value); }
		}



		// Using a DependencyProperty as the backing store for MyStringProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyStringPropertyProperty =
			DependencyProperty.Register("MyStringProperty", typeof(string), typeof(SubObject), new FrameworkPropertyMetadata("", OnStringPropertyChanged));

		private static void OnStringPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is SubObject so)
			{
				Console.WriteLine($"Setting StringProperty to {args.NewValue}");
				so.MyStringPropertyCounter++;
			}
		}


		#region Inner DependencyProperty

		public SubObject Inner
		{
			get { return (SubObject)GetValue(InnerProperty); }
			set { SetValue(InnerProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Inner.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerProperty =
			DependencyProperty.Register(
				name: "Inner",
				propertyType: typeof(SubObject),
				ownerType: typeof(SubObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.LogicalChild,
					propertyChangedCallback: (s, e) => ((SubObject)s)?.OnInnerChanged(e)
				)
			);

		private void OnInnerChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

	}
}
