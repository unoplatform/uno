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

namespace Uno.UI.Tests.BinderTests_StandardProperty
{
	[TestClass]
	public partial class Given_DependencyProperty_StandardProperty
	{
		[TestMethod]
		public void When_SimpleInheritance()
		{
			var SUT = new MyObject();

			SUT.SetBinding("Name", new Binding());

			SUT.DataContext = "Test";

			Assert.AreEqual("Test", SUT.Name);
		}
	}

	public partial class MyObject : DependencyObject
	{
		public string Name
		{
			get => _name;
			set => _name = value;
		}

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
		private string _name;

		private void OnInnerObjectChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

	}
}
