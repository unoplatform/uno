// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;

using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public partial class ListViewBaseItemAutomationIntegrationTests : BaseDxamlTestClass
	{
		private const string RootXaml =
			"""
			<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <ListView x:Name='listViewDMP' Width='300' AutomationProperties.Name='TestListViewName'>
			        <ListViewItem Content = 'One' AutomationProperties.AutomationId = 'ListViewItem1' />
			        <ListViewItem Content = '2' AutomationProperties.Name = 'TWO!' />
			        <ListViewItem AutomationProperties.AutomationId = 'ListViewItem3' />
			    </ListView>
			</StackPanel>
			""";

		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			WindowHelper.VerifyTestCleanup();
		}

		[TestMethod]
		[Ignore("Requires a UI Automation client (IUIAutomation); see VerifyDefaultAutomationNameContent_NoUIAClient")]
		[Description("Validates that we return a reasonable AutomationProperties.Name in the event the app developer does not specify one.")]
		public async Task VerifyDefaultAutomationNameContent()
		{
			TestCleanupWrapper cleanup;

			await RunOnUIThread(() =>
			{
				// Set up data items
				var rootPanel = XamlReader.Load(RootXaml) as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();

			// UNO TODO: The original walks the UIA client tree (IUIAutomation TreeWalker) starting at the element named
			// "TestListViewName" and checks UIA_NamePropertyId / UIA_ControlTypePropertyId of its three children:
			//   item 1: Name "One",   ControlType ListItem
			//   item 2: Name "TWO!",  ControlType ListItem
			//   item 3: Name "",      ControlType ListItem
			// Uno has no UIA client, so the same checks run against the automation peers in
			// VerifyDefaultAutomationNameContent_NoUIAClient.
		}

		[TestMethod]
		[Description("Validates that we return a reasonable AutomationProperties.Name in the event the app developer does not specify one, using automation peers instead of a UI Automation client.")]
		public async Task VerifyDefaultAutomationNameContent_NoUIAClient()
		{
			TestCleanupWrapper cleanup;
			ListView listView = null;

			await RunOnUIThread(() =>
			{
				// Set up data items
				var rootPanel = XamlReader.Load(RootXaml) as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				listView = (ListView)rootPanel.FindName("listViewDMP");
				VERIFY_IS_NOT_NULL(listView);

				WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var listViewPeer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);

				LOG_OUTPUT("Verifying the automation peer for spListViewBaseWithName exists.");
				VERIFY_IS_NOT_NULL(listViewPeer);
				VERIFY_ARE_EQUAL(listViewPeer.GetName(), "TestListViewName");

				var children = listViewPeer.GetChildren();
				VERIFY_IS_NOT_NULL(children);
				VERIFY_ARE_EQUAL(children.Count, 3);

				LOG_OUTPUT("Verifying the Name property of the first list view item.");
				VERIFY_ARE_EQUAL(children[0].GetName(), "One");
				VERIFY_ARE_EQUAL(children[0].GetAutomationControlType(), AutomationControlType.ListItem);

				LOG_OUTPUT("Verifying the Name property of the second list view item.");
				VERIFY_ARE_EQUAL(children[1].GetName(), "TWO!");
				VERIFY_ARE_EQUAL(children[1].GetAutomationControlType(), AutomationControlType.ListItem);

				LOG_OUTPUT("Verifying the Name property of the third list view item.");
				VERIFY_ARE_EQUAL(children[2].GetName(), "");
				VERIFY_ARE_EQUAL(children[2].GetAutomationControlType(), AutomationControlType.ListItem);
			});
		}
	}
}
