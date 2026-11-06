// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Controls;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class ListViewBaseHeaderItemIntegrationTests : BaseDxamlTestClass
{
	private static async Task SetupMoCoWithVariousVisualStates<T>(string type)
		where T : Control
	{
		T normal = null;
		T pointerOver = null;
		T pressed = null;
		T disabled = null;
		T focused = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>" +
				"    <StackPanel>" +
				"        <" + type + " x:Name='normal' Content='Normal' AutomationProperties.Name='item1'/>" +
				"        <" + type + " x:Name='pointerOver' Content='Pointer Over'/>" +
				"        <" + type + " x:Name='pressed' Content='Pressed'/>" +
				"        <" + type + " x:Name='disabled' Content='Disabled'/>" +
				"        <" + type + " x:Name='focused' Content='Focused'/>" +
				"    </StackPanel>" +
				"</Grid>") as Grid;
			VERIFY_IS_NOT_NULL(rootPanel);

			normal = (T)rootPanel.FindName("normal");
			pointerOver = (T)rootPanel.FindName("pointerOver");
			pressed = (T)rootPanel.FindName("pressed");
			disabled = (T)rootPanel.FindName("disabled");
			focused = (T)rootPanel.FindName("focused");

			WindowHelper.WindowContent = rootPanel;
		});
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VisualStateManager.GoToState(normal, "Normal", false);

			VisualStateManager.GoToState(pointerOver, "PointerOver", false);

			VisualStateManager.GoToState(pressed, "Pressed", false);

			VisualStateManager.GoToState(disabled, "Disabled", false);

			VisualStateManager.GoToState(focused, "Focused", false);
		});
		await WindowHelper.WaitForIdle();
	}

	private static async Task VerifyCanEnterAndLeaveLiveTree<T>()
		where T : FrameworkElement, new()
	{
		var loadedEvent = new Event();
		var unloadedEvent = new Event();

		await RunOnUIThread(() =>
		{
			var element = new T();
			element.Loaded += (s, e) => loadedEvent.Set();
			element.Unloaded += (s, e) => unloadedEvent.Set();
			WindowHelper.WindowContent = element;
		});
		await loadedEvent.WaitForDefault();

		await RunOnUIThread(() => WindowHelper.WindowContent = null);
		await unloadedEvent.WaitForDefault();
	}

	//
	// Class & Test Setup
	//
	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
	}

	[TestCleanup]
	public void TestCleanup()
	{
		WindowHelper.VerifyTestCleanup();
	}

	//
	// Test Cases
	//
	[TestMethod]
	[RunsOnUIThread]
	public void CanInstantiate()
	{
		var listViewHeaderItem = () => new ListViewHeaderItem();
		listViewHeaderItem.Should().NotThrow();

		var gridViewHeaderItem = () => new GridViewHeaderItem();
		gridViewHeaderItem.Should().NotThrow();
	}

	[TestMethod]
	public async Task CanEnterAndLeaveLiveTree()
	{
		await VerifyCanEnterAndLeaveLiveTree<ListViewHeaderItem>();
		await VerifyCanEnterAndLeaveLiveTree<GridViewHeaderItem>();
	}

	[TestMethod]
	[Ignore("Compares the composition tree against a MockDComp master file; Uno has no MockDComp and TestServices.Utilities.VerifyMockDCompOutput is a no-op.")]
	public async Task VisualStyleListViewHeaderItem()
	{
		// MockDComp should be injected and detached per test, since it keeps information like the surfaces
		// that get created.
		//WUCRenderingScopeGuard guard(DCompRendering::WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

		// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
		WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(400.0f, 600.0f));

		await SetupMoCoWithVariousVisualStates<ListViewHeaderItem>("ListViewHeaderItem");

		Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
	}

	[TestMethod]
	[Ignore("Compares the composition tree against a MockDComp master file; Uno has no MockDComp and TestServices.Utilities.VerifyMockDCompOutput is a no-op.")]
	public async Task VisualStyleGridViewHeaderItem()
	{
		// MockDComp should be injected and detached per test, since it keeps information like the surfaces
		// that get created.
		//WUCRenderingScopeGuard guard(DCompRendering::WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

		// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
		WindowHelper.SetWindowSizeOverride(new global::Windows.Foundation.Size(400.0f, 600.0f));

		await SetupMoCoWithVariousVisualStates<GridViewHeaderItem>("GridViewHeaderItem");

		Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
	}

	[TestMethod]
	[Ignore("Disabled upstream: the WinUI test is marked Ignore=True (and excluded on WindowsCore).")]
	public async Task CanGotoPointerOverVisualState()
	{
		Grid rootPanel = null;
		ListViewHeaderItem headerItem = null;
		Storyboard pointerOverStoryboard = null;
		await RunOnUIThread(() =>
		{
			rootPanel = (Grid)XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Black'/> ");

			WindowHelper.WindowContent = rootPanel;
			headerItem = new ListViewHeaderItem();

			headerItem.Content = "ListViewHeaderItem with all normal visual states";

			headerItem.Style = (Style)XamlReader.Load(
				"""
				<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ListViewHeaderItem'>
				  <Setter Property='Template'>
				    <Setter.Value>
				      <ControlTemplate TargetType='ListViewHeaderItem'>
				        <Border x:Name='Border'>
				          <VisualStateManager.VisualStateGroups>
				            <VisualStateGroup x:Name='CommonStates'>
				              <VisualState x:Name='Normal'>
				                <Storyboard>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='Border' Storyboard.TargetProperty='Background'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Transparent' />
				                  </ObjectAnimationUsingKeyFrames>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='ContentPresenter' Storyboard.TargetProperty='Foreground'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Red' />
				                  </ObjectAnimationUsingKeyFrames>
				                </Storyboard>
				              </VisualState>
				              <VisualState x:Name='PointerOver'>
				                <Storyboard>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='Border' Storyboard.TargetProperty='Background'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Black' />
				                  </ObjectAnimationUsingKeyFrames>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='ContentPresenter' Storyboard.TargetProperty='Foreground'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='White' />
				                  </ObjectAnimationUsingKeyFrames>
				                </Storyboard>
				              </VisualState>
				              <VisualState x:Name='Pressed'>
				                <Storyboard>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='Border' Storyboard.TargetProperty='Background'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Green' />
				                  </ObjectAnimationUsingKeyFrames>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='ContentPresenter' Storyboard.TargetProperty='Foreground'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Blue' />
				                  </ObjectAnimationUsingKeyFrames>
				                </Storyboard>
				              </VisualState>
				              <VisualState x:Name='Disabled'>
				                <Storyboard>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='Border' Storyboard.TargetProperty='Background'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Orange' />
				                  </ObjectAnimationUsingKeyFrames>
				                  <ObjectAnimationUsingKeyFrames Storyboard.TargetName='ContentPresenter' Storyboard.TargetProperty='Foreground'>
				                    <DiscreteObjectKeyFrame KeyTime='0' Value='Red' />
				                  </ObjectAnimationUsingKeyFrames>
				                </Storyboard>
				              </VisualState>
				            </VisualStateGroup>
				          </VisualStateManager.VisualStateGroups>
				          <ContentPresenter x:Name='ContentPresenter' Content='{TemplateBinding Content}'/>
				        </Border>
				      </ControlTemplate>
				    </Setter.Value>
				  </Setter>
				</Style>
				""");
			rootPanel.Children.Add(headerItem);
		});

		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// find the root border and find the Month to Year transition.
			var rootBorder = (Border)VisualTreeHelper.GetChild(headerItem, 0);
			VERIFY_IS_NOT_NULL(rootBorder);
			var vsgs = VisualStateManager.GetVisualStateGroups(rootBorder);

			VisualState pointerOverState = null;

			foreach (var vsg in vsgs)
			{
				if (vsg.Name == "CommonStates")
				{
					foreach (var state in vsg.States)
					{
						if (state.Name == "PointerOver")
						{
							pointerOverState = state;
							break;
						}
					}
					if (pointerOverState != null)
					{
						break;
					}
				}
			}

			VERIFY_IS_NOT_NULL(pointerOverState);
			pointerOverStoryboard = pointerOverState.Storyboard;
			VERIFY_IS_NOT_NULL(pointerOverStoryboard);
		});

		var storyboardCompletedEvent = new Event();
		var storyboardCompletedRegistration = CreateSafeEventRegistration<Storyboard, EventHandler<object>>("Completed");

		await RunOnUIThread(() =>
		{
			storyboardCompletedRegistration.Attach(pointerOverStoryboard, (sender, e) =>
			{
				storyboardCompletedEvent.Set();
			});
		});

		InputHelper.MoveMouse(headerItem);

		await WindowHelper.WaitForIdle();

		await storyboardCompletedEvent.WaitForDefault();
		VERIFY_IS_TRUE(storyboardCompletedEvent.HasFired());

		InputHelper.MoveMouse(new global::Windows.Foundation.Point(0, 0));
	}
}
