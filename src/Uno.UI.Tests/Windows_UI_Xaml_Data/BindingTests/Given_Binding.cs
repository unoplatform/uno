using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests
{
	[TestClass]
	public class Given_Binding
	{
		[TestMethod]
		public void When_ElementName_In_Template()
		{
			var SUT = new Binding_ElementName_In_Template();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource()
		{
			var SUT = new Binding_ElementName_In_Template_Resource();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource_In_Dictionary()
		{
			var SUT = new Binding_ElementName_In_Template_Resource_In_Dictionary();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(SUT.PrimaryActionsList.Tag, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl_NonUINested()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl_NonUINested();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(SUT.PrimaryActionsList.Tag, button.Tag);
			 
			var nestedDO = Binding_ElementName_In_Template_ItemsControl_NonUINested_Attached.GetNonUIObject(button);

			Assert.IsNotNull(nestedDO);
			Assert.AreEqual(nestedDO.InnerProperty, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_NonUINested_GlobalResources()
		{
			var SUT = new Binding_ElementName_NonUINested_GlobalResources();

			var primaryActionsList = SUT.FindName("PrimaryActionsList") as Windows.UI.Xaml.Controls.ItemsControl;

			primaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(primaryActionsList.Tag, button.Tag);

			var nestedDO = Binding_ElementName_NonUINested_GlobalResources_Attached.GetNonUIObject(button);

			Assert.IsNotNull(nestedDO);
			Assert.AreEqual(nestedDO.InnerProperty, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_In_Template_In_Template()
		{
			var SUT = new Binding_ElementName_In_Template_In_Template();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}
	}
}
