using CommonServiceLocator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;
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
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Threading;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_LocalCache
	{
		[TestMethod]
		public void When_NormalUpdate()
		{
			var SUT = new BinderLocalCache_Data();

			SUT.MyValue = 42;

			Assert.AreEqual(42, SUT.MyValue);
		}

		[TestMethod]
		public void When_DirectDP_Update()
		{
			var SUT = new BinderLocalCache_Data();

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 42);

			Assert.AreEqual(42, SUT.MyValue);
		}

		[TestMethod]
		public void When_Update_Lower_Precedence()
		{
			var SUT = new BinderLocalCache_Data();

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 42);
			Assert.AreEqual(42, SUT.MyValue);

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 43, DependencyPropertyValuePrecedences.DefaultStyle);
			Assert.AreEqual(42, SUT.MyValue);
		}

		[TestMethod]
		public void When_Update_Higher_Precedence()
		{
			var SUT = new BinderLocalCache_Data();

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 42);
			Assert.AreEqual(42, SUT.MyValue);

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 43, DependencyPropertyValuePrecedences.Animations);
			Assert.AreEqual(43, SUT.MyValue);

			SUT.ClearValue(BinderLocalCache_Data.MyValueProperty, DependencyPropertyValuePrecedences.Animations);
			Assert.AreEqual(42, SUT.MyValue);
		}

		[TestMethod]
		public void When_Changing_Read_Value()
		{
			var SUT = new BinderLocalCache_Data();

			SUT.SetValue(BinderLocalCache_Data.MyValueProperty, 42);
			Assert.AreEqual(42, SUT.MyValue);
			Assert.AreEqual(42, SUT.MyValuePropertyValueDuringChange);
		}

		[TestMethod]
		public void When_Coerce_Default()
		{
			var SUT = new BinderLocalCache_Data_IsEnabled();
			Assert.IsTrue(SUT.IsEnabled);

			SUT.SetValue(BinderLocalCache_Data_IsEnabled.IsEnabledProperty, false);
			Assert.IsFalse(SUT.IsEnabled);
		}

		[TestMethod]
		public void When_Coerce_And_Coerce_False()
		{
			var SUT = new BinderLocalCache_Data_IsEnabled();
			Assert.IsTrue(SUT.IsEnabled);

			SUT.SuppressIsEnabled(true);

			Assert.IsFalse(SUT.IsEnabled);

			SUT.SuppressIsEnabled(false);

			Assert.IsTrue(SUT.IsEnabled);
		}
	}

	public partial class BinderLocalCache_Data : DependencyObject
	{
		public int MyValuePropertyValueDuringChange { get; private set; }

		public int MyValue
		{
			get => GetMyValueValue();
			set => SetMyValueValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = 0, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty MyValueProperty { get; } = CreateMyValueProperty();

		private void OnMyValueChanged(int oldValue, int newValue)
		{
			MyValuePropertyValueDuringChange = MyValue;
		}
	}

	public partial class BinderLocalCache_Data_IsEnabled : DependencyObject
	{
		private bool _suppressIsEnabled;

		public bool IsEnabled
		{
			get => GetIsEnabledValue();
			set => SetIsEnabledValue(value);
		}
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty IsEnabledProperty { get; } = CreateIsEnabledProperty();
		private void OnIsEnabledChanged(bool oldValue, bool newValue) { }
		private object CoerceIsEnabled(object baseValue) => _suppressIsEnabled ? false : baseValue;

		public void SuppressIsEnabled(bool suppress)
		{
			_suppressIsEnabled = suppress;
			this.CoerceValue(IsEnabledProperty);
		}
	}
}
