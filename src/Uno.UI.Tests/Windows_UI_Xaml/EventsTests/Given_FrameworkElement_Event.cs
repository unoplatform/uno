using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.UI.Tests.Windows_UI_Xaml.EventsTests.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml.EventsTests
{
	[TestClass]
	public class Given_FrameworkElement_Event
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_DataTemplateEvent()
		{
			var SUT = new FrameworkElement_DataTemplate_Event();
			SUT.ForceLoaded();
			Assert.AreEqual(0, SUT.CheckBox_CheckedCount);

			SUT.DataContext = true;

			Assert.AreEqual(1, SUT.CheckBox_CheckedCount);
		}

		[TestMethod]
		public void When_DataTemplateEvent_OtherControl()
		{
			var SUT = new FrameworkElement_DataTemplate_Event_OtherControl();
			SUT.ForceLoaded();

			Assert.AreEqual("Fired!", SUT.testControl.testTextBlock.Text);
		}

		[TestMethod]
		public void When_ItemsPanelTemplateEvent()
		{
			var SUT = new FrameworkElement_ItemsPanelTemplate_Event();

			Assert.AreEqual(0, SUT.StackPanel_Loaded_Count);

			SUT.ForceLoaded();

			// Count should theoratically be one, but we're testing event
			// invocation here, so change to one if needed.
			Assert.AreEqual(2, SUT.StackPanel_Loaded_Count);
		}

		[TestMethod]
		public void When_ControlTemplateEvent()
		{
			var SUT = new FrameworkElement_ControlTemplate_Event();
			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.CheckBox_Checked_Count);

			SUT.DataContext = true;

			Assert.AreEqual(1, SUT.CheckBox_Checked_Count);
		}
	}
}
