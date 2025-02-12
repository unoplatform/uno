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
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls;
using System.Threading;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_GeneratedAttached
	{
		[TestMethod]
		public void ReadDefault()
		{
			var SUT = new Binder_GeneratedAttached_Data();

			Assert.AreEqual(21, Binder_GeneratedAttached_Attached.GetMyValue(SUT));
		}

		[TestMethod]
		public void ReadSetValue()
		{
			var SUT = new Binder_GeneratedAttached_Data();

			Binder_GeneratedAttached_Attached.SetMyValue(SUT, 42);
			Assert.AreEqual(42, Binder_GeneratedAttached_Attached.GetMyValue(SUT));
		}

		[TestMethod]
		public void ReadSetValue_Precedence()
		{
			var SUT = new Binder_GeneratedAttached_Data();

			Binder_GeneratedAttached_Attached.SetMyValue(SUT, 42);
			Assert.AreEqual(42, Binder_GeneratedAttached_Attached.GetMyValue(SUT));

			SUT.SetValue(Binder_GeneratedAttached_Attached.MyValueProperty, 43, DependencyPropertyValuePrecedences.ImplicitStyle);
			Assert.AreEqual(42, Binder_GeneratedAttached_Attached.GetMyValue(SUT));
		}

		[TestMethod]
		public void ReadSetValue2_Precedence()
		{
			var SUT = new Binder_GeneratedAttached_Data();
			Assert.AreEqual(0, SUT.Value2ChangedCallback);

			Binder_GeneratedAttached_Attached.SetMyValue2(SUT, 42);
			Assert.AreEqual(42, Binder_GeneratedAttached_Attached.GetMyValue2(SUT));
			Assert.AreEqual(1, SUT.Value2ChangedCallback);

			SUT.SetValue(Binder_GeneratedAttached_Attached.MyValue2Property, 43, DependencyPropertyValuePrecedences.ImplicitStyle);
			Assert.AreEqual(42, Binder_GeneratedAttached_Attached.GetMyValue2(SUT));
			Assert.AreEqual(1, SUT.Value2ChangedCallback);
		}
	}

	public partial class Binder_GeneratedAttached_Data : DependencyObject
	{
		public int Value2ChangedCallback { get; internal set; }
	}

	public static partial class Binder_GeneratedAttached_Attached
	{
		[GeneratedDependencyProperty(DefaultValue = 21, AttachedBackingFieldOwner = typeof(Binder_GeneratedAttached_Data), Attached = true)]
		public static DependencyProperty MyValueProperty { get; } = CreateMyValueProperty();

		public static int GetMyValue(DependencyObject instance) => GetMyValueValue(instance);

		public static void SetMyValue(DependencyObject instance, int value) => SetMyValueValue(instance, value);

		[GeneratedDependencyProperty(DefaultValue = 21, AttachedBackingFieldOwner = typeof(Binder_GeneratedAttached_Data), ChangedCallback = true, Attached = true)]
		public static DependencyProperty MyValue2Property { get; } = CreateMyValue2Property();

		public static int GetMyValue2(DependencyObject instance) => GetMyValue2Value(instance);

		public static void SetMyValue2(DependencyObject instance, int value) => SetMyValue2Value(instance, value);

		public static void OnMyValue2Changed(DependencyObject instance, DependencyPropertyChangedEventArgs args)
		{
			if (instance is Binder_GeneratedAttached_Data data)
			{
				data.Value2ChangedCallback++;
			}
		}
	}
}
