// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Common;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Windows.UI.Xaml;
using Windows.UI;
using Private.Infrastructure;
using System.Threading.Tasks;

#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	public class RadioButtonsTests : MUXApiTestBase
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task VerifyCustomItemTemplate()
		{
			RadioButtons radioButtons = null;
			RadioButtons radioButtons2 = null;
			RunOnUIThread.Execute(() =>
			{
				radioButtons = new RadioButtons();
				radioButtons.ItemsSource = new List<string>() { "Option 1", "Option 2" };

				// Set a custom ItemTemplate to be wrapped in a RadioButton.
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text = '{Binding}'/>
                        </DataTemplate>");

				radioButtons.ItemTemplate = itemTemplate;

				radioButtons2 = new RadioButtons();
				radioButtons2.ItemsSource = new List<string>() { "Option 1", "Option 2" };

				// Set a custom ItemTemplate which is already a RadioButton. No wrapping should be performed.
				var itemTemplate2 = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <RadioButton Foreground='Blue'>
                              <TextBlock Text = '{Binding}'/>
                            </RadioButton>
                        </DataTemplate>");

				radioButtons2.ItemTemplate = itemTemplate2;

				var stackPanel = new StackPanel();
				stackPanel.Children.Add(radioButtons);
				stackPanel.Children.Add(radioButtons2);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var radioButton1 = radioButtons.ContainerFromIndex(0) as RadioButton;
				var radioButton2 = radioButtons2.ContainerFromIndex(0) as RadioButton;
				Verify.IsNotNull(radioButton1, "Our custom ItemTemplate should have been wrapped in a RadioButton.");
				Verify.IsNotNull(radioButton2, "Our custom ItemTemplate should have been wrapped in a RadioButton.");

				bool testCondition = !(radioButton1.Foreground is SolidColorBrush brush && brush.Color == Colors.Blue);
				Verify.IsTrue(testCondition, "Default foreground color of the RadioButton should not have been [blue].");

				testCondition = radioButton2.Foreground is SolidColorBrush brush2 && brush2.Color == Colors.Blue;
				Verify.IsTrue(testCondition, "The foreground color of the RadioButton should have been [blue].");
			});
		}

		[TestMethod]
		public async Task VerifyIsEnabledChangeUpdatesVisualState()
		{
			RadioButtons radioButtons = null; ;
			VisualStateGroup commonStatesGroup = null;
			RunOnUIThread.Execute(() =>
			{
				radioButtons = new RadioButtons();

				// Check 1: Set IsEnabled to true.
				radioButtons.IsEnabled = true;

				Content = radioButtons;
				Content.UpdateLayout();

				var radioButtonsLayoutRoot = (FrameworkElement)VisualTreeHelper.GetChild(radioButtons, 0);
				commonStatesGroup = VisualStateManager.GetVisualStateGroups(radioButtonsLayoutRoot).First(vsg => vsg.Name.Equals("CommonStates"));

				Verify.AreEqual("Normal", commonStatesGroup.CurrentState.Name);

				// Check 2: Set IsEnabled to false.
				radioButtons.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("Disabled", commonStatesGroup.CurrentState.Name);

				// Check 3: Set IsEnabled back to true.
				radioButtons.IsEnabled = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("Normal", commonStatesGroup.CurrentState.Name);
			});
		}
	}
}
