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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media;
using System.Diagnostics;

namespace Uno.UI.Tests.BinderTests.Propagation
{
	[TestClass]
	public partial class Given_DependencyProperty_Propagation
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
		public void When_Path_Invalid()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding("B"));

			Assert.AreEqual(0, sub.MyProperty);
			Assert.AreEqual(0, sub.MyPropertyCounter);

			SUT.SubObject = sub;

			Assert.AreEqual(0, sub.MyProperty);
			Assert.AreEqual(0, sub.MyPropertyCounter);

			SUT.DataContext = 42;

			Assert.AreEqual(0, sub.MyProperty);
			Assert.AreEqual(0, sub.MyPropertyCounter);

			SUT.DataContext = new PropagationContext();

			Assert.AreEqual(43, sub.MyProperty);
			Assert.AreEqual(1, sub.MyPropertyCounter);
		}

		[TestMethod]
		public void When_Path_Invalid_And_NotifyPropertyChanged()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyStringPropertyProperty, new Binding("C.D"));

			Assert.AreEqual("", sub.MyStringProperty);
			Assert.AreEqual(0, sub.MyStringPropertyCounter);

			SUT.SubObject = sub;

			Assert.AreEqual("", sub.MyStringProperty);
			Assert.AreEqual(0, sub.MyStringPropertyCounter);

			SUT.DataContext = new PropagationContext2();

			Assert.AreEqual("", sub.MyStringProperty);
			Assert.AreEqual(0, sub.MyStringPropertyCounter);

			SUT.DataContext = new PropagationContext();

			Assert.AreEqual("44", sub.MyStringProperty);

			// This must be one, even if the original binding was
			// set to an invalid property path for PropagationContext2.
			Assert.AreEqual(1, sub.MyStringPropertyCounter);
		}

		[TestMethod]
		public void When_Path_Invalid_And_NotifyPropertyChanged_With_FallbackValue()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyStringPropertyProperty, new Binding("C.D") { FallbackValue = "21" });

			Assert.AreEqual("21", sub.MyStringProperty);
			Assert.AreEqual(1, sub.MyStringPropertyCounter);

			SUT.SubObject = sub;

			Assert.AreEqual("21", sub.MyStringProperty);
			Assert.AreEqual(1, sub.MyStringPropertyCounter);

			SUT.DataContext = new PropagationContext2();

			Assert.AreEqual("21", sub.MyStringProperty);
			Assert.AreEqual(1, sub.MyStringPropertyCounter);

			SUT.DataContext = new PropagationContext();

			Assert.AreEqual("44", sub.MyStringProperty);

			// This must be two, even if the original binding was
			// set to an invalid property path for PropagationContext2.
			Assert.AreEqual(2, sub.MyStringPropertyCounter);
		}

		[TestMethod]
		public void When_Path_Invalid_And_Fallback_Value()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding("B") { FallbackValue = "21" });

			Assert.AreEqual(21, sub.MyProperty);
			Assert.AreEqual(1, sub.MyPropertyCounter);

			SUT.SubObject = sub;

			Assert.AreEqual(21, sub.MyProperty);
			Assert.AreEqual(1, sub.MyPropertyCounter);

			SUT.DataContext = new PropagationContext();

			Assert.AreEqual(43, sub.MyProperty);
			Assert.AreEqual(2, sub.MyPropertyCounter);
		}

		[TestMethod]
		public void When_Path_Invalid_And_Fallback_Value_And_Converter()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding("B") { FallbackValue = "21", Converter = new OppositeConverter() });

			Assert.AreEqual(21, sub.MyProperty);
			Assert.AreEqual(1, sub.MyPropertyCounter);

			SUT.SubObject = sub;

			// We are testing that the converter isn't called when using FallbackValue
			Assert.AreEqual(21, sub.MyProperty);
			Assert.AreEqual(1, sub.MyPropertyCounter);

			SUT.DataContext = new PropagationContext();

			// We are testing that the converter is called when not using FallbackValue
			Assert.AreEqual(-43, sub.MyProperty);
			Assert.AreEqual(2, sub.MyPropertyCounter);
		}

		[TestMethod]
		public void When_DataContext_Set_After()
		{
			var SUT = new MyObject();

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, sub.MyProperty);

			SUT.SubObject = sub;

			Assert.AreEqual(0, sub.MyProperty);

			SUT.DataContext = 42;

			Assert.AreEqual(42, sub.MyProperty);
		}

		[TestMethod]
		public void When_DataContext_Set_Before()
		{
			var SUT = new MyObject();

			SUT.DataContext = 42;

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding());

			Assert.AreEqual(0, sub.MyProperty);

			SUT.SubObject = sub;

			// The behavior of UWP is undefined. The datacontext is pass through unknown means tp
			// non-visual elements, to allow binding. But if defined through code, the datacontext
			// is not flowing. We're reproducing this behavior here.
			Assert.AreEqual(0, sub.MyProperty);
		}

		[TestMethod]
		public void When_DataContext_And_SelfReference()
		{
			var SUT = new MyObject();
			SUT.SelfTest = SUT;

			var sub = new SubObject();
			sub.SetBinding(SubObject.MyPropertyProperty, new Binding());
			SUT.SubObject = sub;

			// This should not fail because of a stack overflow.
			SUT.DataContext = 42;

			Assert.AreEqual(42, sub.MyProperty);
		}

		[TestMethod]
		public void When_ValidBinding_And_Then_InvalidBinding()
		{
			var sub = new DependencyObjectCollection();
			var other = new MyObjectWithExplicitDefaultValue();
			other.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("a") });

			sub.Add(other);

			sub.DataContext = new { a = 42 };

			Assert.AreEqual(42, other.MyProperty);

			sub.DataContext = 42;

			Assert.AreEqual(77, other.MyProperty);
		}


		[TestMethod]
		public void When_ValidBinding_And_Then_InvalidBinding_Inherited()
		{
			var o1 = new MyObjectWithExplicitDefaultValue();
			o1.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("a") });

			var o2 = new MyObjectWithExplicitDefaultValue();

			o1.SameTypeObject = o2;
			o2.SetParent(o1);

			o1.DataContext = new { a = 42 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(42, o2.MyProperty);

			o1.DataContext = 42;

			Assert.AreEqual(77, o1.MyProperty);
			Assert.AreEqual(77, o2.MyProperty);
		}



		[TestMethod]
		public void When_ValidBinding_And_Then_InvalidBinding_Inherited_Different_Binding()
		{
			var o1 = new MyObjectWithExplicitDefaultValue();
			o1.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("a") });

			var o2 = new MyObjectWithExplicitDefaultValue();
			o2.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("b") });

			o1.SameTypeObject = o2;
			o2.SetParent(o1);

			o1.DataContext = new { a = 42, b = 43 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(43, o2.MyProperty);

			o1.DataContext = 42;

			Assert.AreEqual(77, o1.MyProperty);
			Assert.AreEqual(77, o2.MyProperty);
		}


		[TestMethod]
		public void When_ValidBinding_And_Then_InvalidBinding_Inherited_Partial_Different_Binding()
		{
			var o1 = new MyObjectWithExplicitDefaultValue();
			o1.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("a") });

			var o2 = new MyObjectWithExplicitDefaultValue();
			o2.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("b") });

			o1.SameTypeObject = o2;
			o2.SetParent(o1);

			o1.DataContext = new { a = 42, b = 43 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(43, o2.MyProperty);

			o1.DataContext = new { a = 42 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(77, o2.MyProperty);

			o1.DataContext = 42;

			Assert.AreEqual(77, o1.MyProperty);
			Assert.AreEqual(77, o2.MyProperty);
		}

		[TestMethod]
		public void When_ValidBinding_And_Then_InvalidBinding_Inherited_Partial_Different_Binding_And_Fallback()
		{
			var o1 = new MyObjectWithExplicitDefaultValue();
			o1.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("a") });

			var o2 = new MyObjectWithExplicitDefaultValue();
			o2.SetBinding(MyObjectWithExplicitDefaultValue.MyPropertyProperty, new Binding() { Path = new PropertyPath("b"), FallbackValue = 78 });

			o1.SameTypeObject = o2;
			o2.SetParent(o1);

			o1.DataContext = new { a = 42, b = 43 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(43, o2.MyProperty);

			o1.DataContext = new { a = 42 };

			Assert.AreEqual(42, o1.MyProperty);
			Assert.AreEqual(78, o2.MyProperty);

			o1.DataContext = 42;

			Assert.AreEqual(77, o1.MyProperty);
			Assert.AreEqual(78, o2.MyProperty);
		}

		[TestMethod]
		public void When_DependencyObject_Bindable_Removed()
		{
			var SUT = new MyObject();
			var sub1 = new SubObject();
			SUT.SubObject = sub1;
			SUT.SubObject = null;

			SUT.DataContext = SUT;

			Assert.IsNull(sub1.DataContext);
		}

		[TestMethod]
		public void When_Updating_Collected()
		{
			var SUT = new MyObject();
			WeakReference sub1WR, sub1Store;

			void CreateSub()
			{
				var sub1 = new SubObject();
				SUT.SubObject = sub1;
				sub1.SetParent(SUT);

				sub1WR = new WeakReference(sub1);
				sub1Store = new WeakReference(((IDependencyObjectStoreProvider)sub1).Store);

				SUT.SubObject = null;
			}

			CreateSub();

			SUT.TemplatedParent = SUT;

			GC.Collect(2, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();

			SUT.TemplatedParent = null;

			GC.Collect(2, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();

			Assert.IsNull(sub1WR.Target);
			Assert.IsNull(sub1Store.Target);
		}

		[TestMethod]
		public void When_Style_Then_Dont_Inherit()
		{
			var SUT = new Grid();

			var style = new Style();
			var setter = new Setter(Grid.TagProperty, 1);
			style.Setters.Add(setter);

			SUT.Style = style;

			Assert.IsNull(setter.DataContext);

			SUT.DataContext = 42;

			Assert.IsNull(setter.DataContext);
			Assert.IsNull(style.DataContext);
		}

		[TestMethod]
		public void When_DataTemplate_Then_Dont_Inherit()
		{
			var SUT = new ContentControl();

			var template = new DataTemplate(() => new Grid());
			SUT.ContentTemplate = template;

			Assert.IsNull(template.DataContext);

			SUT.DataContext = 42;

			Assert.IsNull(template.DataContext);
		}

		[TestMethod]
		public void When_ControlTemplate_Then_Dont_Inherit()
		{
			var SUT = new ContentControl();

			var template = new ControlTemplate(() => new Grid());
			SUT.Template = template;

			Assert.IsNull(template.DataContext);

			SUT.DataContext = 42;

			Assert.IsNull(template.DataContext);
		}

		[TestMethod]
		public void When_ControlTemplate_And_Animation()
		{
			var SUT = new ContentControl() { Tag = 42 };
			DoubleAnimation anim = null;

			var template = new ControlTemplate(() =>
			{
				var g = new Grid();

				var vg = new VisualStateGroup();
				var t1 = new VisualTransition();
				var sb = new Storyboard();
				anim = new DoubleAnimation();
				anim.SetBinding(DoubleAnimation.ToProperty, new Binding() { Path = "Tag", RelativeSource = RelativeSource.TemplatedParent });
				sb.Children.Add(anim);
				t1.Storyboard = sb;
				vg.Transitions.Add(t1);

				VisualStateManager.SetVisualStateGroups(g, new List<VisualStateGroup> { vg });

				return g;
			});

			SUT.Template = template;
			SUT.ApplyTemplate();

			Assert.IsNotNull(anim);
		}

		[TestMethod]
		public async Task When_PrecedenceChanged_Then_Released()
		{
			var SUT = new ContentControl();

			WeakReference Build()
			{
				var dc = new object();
				SUT.DataContext = dc;
				SUT.SetValue(ContentControl.ForegroundProperty, new SolidColorBrush(Windows.UI.Colors.Red));

				SUT.DataContext = null;

				return new(dc);
			}

			await AssertCollectedReference(Build());
		}


		[TestMethod]
		public async Task When_PrecedenceChanged_And_Back_Then_Restored()
		{
			var SUT = new ContentControl();

			WeakReference Build()
			{
				var dc = new object();
				SUT.DataContext = dc;

				var originalBrush = SUT.Foreground as Brush;
				var newBrush = new SolidColorBrush(Windows.UI.Colors.Red);

				SUT.SetValue(ContentControl.ForegroundProperty, newBrush);

				Assert.IsNull(originalBrush.DataContext);
				Assert.IsNotNull(newBrush.DataContext);

				SUT.ClearValue(ContentControl.ForegroundProperty);

				Assert.AreEqual(dc, originalBrush.DataContext);
				Assert.IsNull(newBrush.DataContext);

				SUT.DataContext = null;

				return new(dc);
			}

			await AssertCollectedReference(Build());
		}

		[TestMethod]
		public async Task When_PrecedenceChanged_To_Null_And_Back_Then_Restored()
		{
			var SUT = new ContentControl();

			WeakReference Build()
			{
				var dc = new object();
				SUT.DataContext = dc;

				var originalBrush = SUT.Foreground as Brush;

				SUT.SetValue(ContentControl.ForegroundProperty, null);

				Assert.IsNull(originalBrush.DataContext);

				SUT.ClearValue(ContentControl.ForegroundProperty);

				Assert.AreEqual(dc, originalBrush.DataContext);

				SUT.DataContext = null;

				return new(dc);
			}

			await AssertCollectedReference(Build());
		}

		private async Task AssertCollectedReference(WeakReference reference)
		{
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(2))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();

				if (!reference.IsAlive)
				{
					return;
				}

				await Task.Delay(100);
			}

			Assert.IsFalse(reference.IsAlive);
		}
	}

	public partial class MyObject : DependencyObject
	{
		public MyObject()
		{

		}

		public DependencyObject SelfTest
		{
			get { return (DependencyObject)GetValue(SelfTestProperty); }
			set { SetValue(SelfTestProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfTest.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelfTestProperty =
			DependencyProperty.Register("SelfTest", typeof(DependencyObject), typeof(MyObject), new FrameworkPropertyMetadata(null));



		#region SubObject DependencyProperty

		public SubObject SubObject
		{
			get { return (SubObject)GetValue(SubObjectProperty); }
			set { SetValue(SubObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SubObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SubObjectProperty =
			DependencyProperty.Register("SubObject", typeof(SubObject), typeof(MyObject), new FrameworkPropertyMetadata(null, (s, e) => ((MyObject)s)?.OnSubObjectChanged(e)));


		private void OnSubObjectChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion
	}

	public partial class SubObject : DependencyObject
	{
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
	}

	public partial class MyObjectWithExplicitDefaultValue : DependencyObject
	{

		#region SameTypeObject DependencyProperty

		public MyObjectWithExplicitDefaultValue SameTypeObject
		{
			get { return (MyObjectWithExplicitDefaultValue)GetValue(SameTypeObjectProperty); }
			set { SetValue(SameTypeObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SameTypeObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SameTypeObjectProperty =
			DependencyProperty.Register(
				name: "SameTypeObject",
				propertyType: typeof(MyObjectWithExplicitDefaultValue),
				ownerType: typeof(MyObjectWithExplicitDefaultValue),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((MyObjectWithExplicitDefaultValue)s)?.OnSameTypeObjectChanged(e)
				)
		);


		private void OnSameTypeObjectChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion



		#region MyProperty DependencyProperty

		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register(
				name: "MyProperty",
				propertyType: typeof(int),
				ownerType: typeof(MyObjectWithExplicitDefaultValue),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 77,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((MyObjectWithExplicitDefaultValue)s)?.OnMyPropertyChanged(e)
				)
			);


		private void OnMyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

	}


	public class PropagationContext
	{
		public int B => 43;

		public PropagationContext3 C { get; } = new PropagationContext3();
	}

	public class PropagationContext3
	{
		public int D => 44;
	}

	public class PropagationContext2 : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
	}

	public class OppositeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var number = (int)value;

			return -number;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
