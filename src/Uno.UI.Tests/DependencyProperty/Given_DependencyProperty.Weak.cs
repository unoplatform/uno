using Microsoft.Practices.ServiceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Logging;
using Uno.Extensions;
using Uno.Presentation.Resources;
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

namespace Uno.UI.Tests.BinderTests_Weak
{
	[TestClass]
	public partial class Given_DependencyProperty_Weak
	{
		[TestMethod]
		public void When_SimpleInheritance()
		{
			var SUT = new MyObject();

			SUT.SetValue(MyObject.ValueProperty, 42, DependencyPropertyValuePrecedences.Inheritance);
			Assert.AreEqual(42, SUT.GetValue(MyObject.ValueProperty));

			SUT.SetValue(MyObject.ValueProperty, 43, DependencyPropertyValuePrecedences.Local);
			Assert.AreEqual(43, SUT.GetValue(MyObject.ValueProperty));

			SUT.ClearValue(MyObject.ValueProperty, DependencyPropertyValuePrecedences.Local);
			Assert.AreEqual(42, SUT.GetValue(MyObject.ValueProperty));

			SUT.ClearValue(MyObject.ValueProperty, DependencyPropertyValuePrecedences.Inheritance);
			Assert.AreEqual(null, SUT.GetValue(MyObject.ValueProperty));
		}
	}

	public partial class MyObject : DependencyObject
	{
		public object Value
		{
			get => (MyObject)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		// Using a DependencyProperty as the backing store for InnerObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				name: "Value",
				propertyType: typeof(object),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.WeakStorage,
					propertyChangedCallback: (s, e) => ((MyObject)s)?.OnValueChanged(e)
				)
			);


		private void OnValueChanged(DependencyPropertyChangedEventArgs e)
		{
		}
	}
}
