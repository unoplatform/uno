// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.DevTools.Input;

using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	[TestClass]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public partial class ListViewBaseItemIntegrationTests : BaseDxamlTestClass
	{
		// Merges a dictionary that turns on rounded chrome for the lifetime of the helper.
		private sealed class RoundedChromeDictionaryHelper : IAsyncDisposable
		{
			private ResourceDictionary m_customDictionary;

			private RoundedChromeDictionaryHelper()
			{
			}

			public static async Task<RoundedChromeDictionaryHelper> Create(bool useRoundedCorners)
			{
				RoundedChromeDictionaryHelper helper = new();

				// If we aren't using rounded chrome, we don't have to do anything.  This will just be a no-op.
				if (!useRoundedCorners)
				{
					return helper;
				}

				// Set up a dictionary and add it to the application.
				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("Creating custom resource dictionary for rounded chrome.");
					helper.m_customDictionary = (ResourceDictionary)XamlReader.Load(
						"""
						<ResourceDictionary
						    xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
						    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
						    <x:Boolean x:Key="ListViewBaseItemRoundedChromeEnabled">True</x:Boolean>
						</ResourceDictionary>
						""");

					Application.Current.Resources.MergedDictionaries.Add(helper.m_customDictionary);
#if HAS_UNO
					// The chrome caches the resource value; make sure it sees the new dictionary.
					ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
#endif
				});

				return helper;
			}

			public async ValueTask DisposeAsync()
			{
				// If we didn't actually set the dictionary, no-op
				if (m_customDictionary is null)
				{
					return;
				}

				await RunOnUIThread(() =>
				{
					var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

					// Find the custom dictionary
					int index = mergedDictionaries.IndexOf(m_customDictionary);
					VERIFY_IS_TRUE(index >= 0);

					// Then remove it and make sure we tell Xaml to reset any cached dictionaries they might have.
					LOG_OUTPUT("Removing dictionary with ListViewBaseItemRoundedChromeEnabled=True.");
					mergedDictionaries.RemoveAt(index);

					// ListViewBaseItem will cache the value, so make sure to clear that cache
					LOG_OUTPUT("Invoking Utilities.DeleteResourceDictionaryCaches.");
#if HAS_UNO
					ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
#endif
				});
			}
		}

		private static async Task GoToVisualStates<T>(Grid rootPanel)
			where T : Control
		{
			T normal = null;
			T pointerOver = null;
			T disabled = null;
			T normalSelected = null;
			T pointerOverSelected = null;
			T disabledSelected = null;
			T multiSelect = null;
			T multiSelectSelected = null;
			T multiSelectDisabled = null;

			T multiSelectSelectedDisabled = null;

			await RunOnUIThread(() =>
			{
				normal = (T)rootPanel.FindName("normal");
				pointerOver = (T)rootPanel.FindName("pointerOver");
				disabled = (T)rootPanel.FindName("disabled");
				normalSelected = (T)rootPanel.FindName("normalSelected");
				pointerOverSelected = (T)rootPanel.FindName("pointerOverSelected");
				disabledSelected = (T)rootPanel.FindName("disabledSelected");
				multiSelect = (T)rootPanel.FindName("multiSelect");
				multiSelectSelected = (T)rootPanel.FindName("multiSelectSelected");
				multiSelectDisabled = (T)rootPanel.FindName("multiSelectDisabled");
				multiSelectSelectedDisabled = (T)rootPanel.FindName("multiSelectSelectedDisabled");
			});

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(normal, "Normal", false);

				VisualStateManager.GoToState(pointerOver, "PointerOver", false);

				VisualStateManager.GoToState(disabled, "Disabled", false);

				VisualStateManager.GoToState(normalSelected, "Selected", false);

				VisualStateManager.GoToState(pointerOverSelected, "PointerOverSelected", false);

				VisualStateManager.GoToState(disabledSelected, "Disabled", false);
				VisualStateManager.GoToState(disabledSelected, "Selected", false);

				VisualStateManager.GoToState(multiSelect, "MultiSelectEnabled", false);

				VisualStateManager.GoToState(multiSelectSelected, "MultiSelectEnabled", false);
				VisualStateManager.GoToState(multiSelectSelected, "Selected", false);

				VisualStateManager.GoToState(multiSelectDisabled, "MultiSelectEnabled", false);
				VisualStateManager.GoToState(multiSelectDisabled, "Disabled", false);

				VisualStateManager.GoToState(multiSelectSelectedDisabled, "MultiSelectEnabled", false);
				VisualStateManager.GoToState(multiSelectSelectedDisabled, "Selected", false);
				VisualStateManager.GoToState(multiSelectSelectedDisabled, "Disabled", false);
			});
			await WindowHelper.WaitForIdle();
		}

		private static async Task<Grid> SetupMoCoWithVariousVisualStates<T>(string type, string style)
			where T : Control
		{
			Grid rootPanel = null;

			await RunOnUIThread(() =>
			{
				rootPanel = XamlReader.Load(
					$$"""
					<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
					    <Grid.ColumnDefinitions>
					        <ColumnDefinition/>
					        <ColumnDefinition/>
					    </Grid.ColumnDefinitions>
					    <StackPanel Grid.Column='0'>
					        <{{type}} {{style}} x:Name='normal' Content='Normal'/>
					        <{{type}} {{style}} x:Name='pointerOver' Content='Pointer Over'/>
					        <{{type}} {{style}} x:Name='disabled' Content='Disabled'/>
					        <{{type}} {{style}} x:Name='normalSelected' Content='Selected'/>
					        <{{type}} {{style}} x:Name='pointerOverSelected' Content='Pointer Over Selected'/>
					        <{{type}} {{style}} x:Name='disabledSelected' Content='Disabled Selected'/>
					    </StackPanel>
					    <StackPanel Grid.Column='1'>
					        <{{type}} {{style}} x:Name='multiSelect' Content='Multi Select'/>
					        <{{type}} {{style}} x:Name='multiSelectSelected' Content='MS Selected'/>
					        <{{type}} {{style}} x:Name='multiSelectDisabled' Content='MS Disabled'/>
					        <{{type}} {{style}} x:Name='multiSelectSelectedDisabled' Content='MS Selected Disabled'/>
					    </StackPanel>
					</Grid>
					""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await GoToVisualStates<T>(rootPanel);

			return rootPanel;
		}

		private static async Task ValidateVisualStyle<T>(string type, string style)
			where T : Control
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			await SetupMoCoWithVariousVisualStates<T>(type, style);

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		private static Point GetCenterOfElement(FrameworkElement element)
		{
			var topLeft = element.TransformToVisual(WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
			return new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
		}

		// UNO TODO: InputHelper has no PressHoldAndPanFromCenter; this mirrors InputHelper.PanFromCenter with a hold before the pan.
		private static void PressHoldAndPanFromCenter(FrameworkElement element, int relX, int relY, double velocityFactor, int holdTime)
		{
			MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
			{
				using var finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
				var center = GetCenterOfElement(element);

				const uint steps = 10;
				var durationInMilliseconds = Math.Sqrt((double)relX * relX + (double)relY * relY) / velocityFactor;

				finger.Press(center);
				Thread.Sleep(holdTime);
				finger.MoveTo(new Point(center.X + relX, center.Y + relY), steps, (uint)Math.Max(1, durationInMilliseconds / steps));
				finger.Release();
			});
		}

		private static async Task VerifyDragOverState<T>(ListViewBase listViewBase)
			where T : FrameworkElement
		{
			Point itemCenter = default;
			Point folderCenter = default;
			FrameworkElement itemAsFE = null;
			FrameworkElement folderAsFE = null;
			VisualStateGroup commonStatesVisualStateGroup = null;
			var stateChangedEvent = new Event();
			var stateChangedRegistration = new SafeEventRegistration<VisualStateGroup, VisualStateChangedEventHandler>("CurrentStateChanged");

			await RunOnUIThread(() =>
			{
				// get the 1st item (a folder)
				folderAsFE = (FrameworkElement)listViewBase.ContainerFromIndex(0);
				VERIFY_IS_NOT_NULL(folderAsFE);

				// get the 4th item (not a folder)
				itemAsFE = (FrameworkElement)listViewBase.ContainerFromIndex(3);
				VERIFY_IS_NOT_NULL(itemAsFE);

				// get the elements' centers
				folderCenter = GetCenterOfElement(folderAsFE);
				itemCenter = GetCenterOfElement(itemAsFE);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Register for VisualStateGroup.CurrentStateChanged for the Folder's DragStates VSG
				var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild((T)folderAsFE, 0);
				var groups = VisualStateManager.GetVisualStateGroups(templateRoot);

				VERIFY_IS_TRUE(groups.Count > 0);

				// we want to find the DragStates VSG
				for (int i = 0; i < groups.Count; ++i)
				{
					commonStatesVisualStateGroup = groups[i];

					if (commonStatesVisualStateGroup.Name == "DragStates")
					{
						break;
					}
				}

				stateChangedRegistration.Attach(commonStatesVisualStateGroup, (object sender, VisualStateChangedEventArgs args) =>
				{
					if (args.NewState.Name == "DragOver")
					{
						stateChangedEvent.Set();
					}
				});
			});
			await WindowHelper.WaitForIdle();

			// Drag the item to the folder
			PressHoldAndPanFromCenter(itemAsFE,
				(int)(folderCenter.X - itemCenter.X) /* relX */,
				(int)(folderCenter.Y - itemCenter.Y) /* relY */,
				0.1 /* velocityFactor */,
				1000 /* holdTime */);
			await WindowHelper.WaitForIdle();

			// UNO TODO: the desktop-only RestoreForegroundWindow workaround has no equivalent.

			await stateChangedEvent.WaitForDefault();
		}

		private static async Task CanInstantiate<T>()
			where T : DependencyObject, new()
		{
			TestCleanupWrapper cleanup;
			await RunOnUIThread(() =>
			{
				// Validate instantiation from code.
				var obj = new T();
				VERIFY_IS_NOT_NULL(obj);

				// Validate instantiation from Xaml.
				var xamlSnippet = "<" + typeof(T).Name + " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'/>";
				var objFromParser = XamlReader.Load(xamlSnippet) as T;
				VERIFY_IS_NOT_NULL(objFromParser);
			});
		}

		private static async Task CanEnterAndLeaveLiveTree<T>()
			where T : FrameworkElement, new()
		{
			TestCleanupWrapper cleanup;
			T element = null;

			await RunOnUIThread(() =>
			{
				element = new T();
			});

			var hasLoadedEvent = new Event();
			var hasUnloadedEvent = new Event();

			var loadedRegistration = new SafeEventRegistration<FrameworkElement, RoutedEventHandler>("Loaded");
			var unloadedRegistration = new SafeEventRegistration<FrameworkElement, RoutedEventHandler>("Unloaded");

			await RunOnUIThread(() =>
			{
				loadedRegistration.Attach(element, (s, e) =>
				{
					hasLoadedEvent.Set();
				});

				unloadedRegistration.Attach(element, (s, e) =>
				{
					hasUnloadedEvent.Set();
				});

				WindowHelper.WindowContent = element;
			});
			await hasLoadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("CanEnterAndLeaveLiveTree: Loaded.");

			await RunOnUIThread(() =>
			{
				WindowHelper.WindowContent = null;
			});
			await hasUnloadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("CanEnterAndLeaveLiveTree: Unloaded.");
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
			WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//
		[TestMethod]
		[Description("Validates that we can successfully create a ListViewItem and GridViewItem.")]
		public async Task CanInstantiate()
		{
			await CanInstantiate<ListViewItem>();
			await CanInstantiate<GridViewItem>();
		}

		[TestMethod]
		[Description("Validates that we can successfully add/remove a ListViewItem and GridViewItem from the live tree.")]
		public async Task CanEnterAndLeaveLiveTree()
		{
			await CanEnterAndLeaveLiveTree<ListViewItem>();
			await CanEnterAndLeaveLiveTree<GridViewItem>();
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the ListViewItem uses the new visual styles correctly.")]
		public async Task ValidateVisualStyleListViewItem()
		{
			await ValidateVisualStyle<ListViewItem>("ListViewItem", "Style = '{ThemeResource ListViewItemExpanded}'");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the GridViewItem uses the new visual styles correctly.")]
		public async Task ValidateVisualStyleGridViewItem()
		{
			await ValidateVisualStyle<GridViewItem>("GridViewItem", "Style = '{ThemeResource GridViewItemExpanded}'");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the ListViewItem chrome uses the new visual styles correctly.")]
		public async Task ValidateVisualStyleLVIChrome()
		{
			// no style means use default which is Chrome
			await ValidateVisualStyle<ListViewItem>("ListViewItem", "" /* style */);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the GridViewItem chrome uses the new visual styles correctly.")]
		public async Task ValidateVisualStyleGVIChrome()
		{
			// no style means use default which is Chrome
			await ValidateVisualStyle<GridViewItem>("GridViewItem", "" /* style */);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the textblock reflows in multiple selection mode.")]
		public async Task ValidateMultiLineTextBlock()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <Grid.ColumnDefinitions>
				        <ColumnDefinition/>
				        <ColumnDefinition/>
				    </Grid.ColumnDefinitions>
				    <StackPanel Grid.Column='0'>
				        <" + type + L" " + style + L" x:Name='normal' Content='Normal'/>
				        <" + type + L" " + style + L" x:Name='pointerOver' Content='Pointer Over'/>
				        <" + type + L" " + style + L" x:Name='disabled' Content='Disabled'/>
				        <" + type + L" " + style + L" x:Name='normalSelected' Content='Selected'/>
				        <" + type + L" " + style + L" x:Name='pointerOverSelected' Content='Pointer Over Selected'/>
				        <" + type + L" " + style + L" x:Name='disabledSelected' Content='Disabled Selected'/>
				    </StackPanel>
				    <StackPanel Grid.Column='1'>
				        <" + type + L" " + style + L" x:Name='multiSelect' Content='Multi Select'/>
				        <" + type + L" " + style + L" x:Name='multiSelectSelected' Content='MS Selected'/>
				        <" + type + L" " + style + L" x:Name='multiSelectDisabled' Content='MS Disabled'/>
				        <" + type + L" " + style + L" x:Name='multiSelectSelectedDisabled' Content='MS Selected Disabled'/>
				    </StackPanel>
				</Grid>
				""") as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that the checkmark supports RTL situations.")]
		public async Task ValidateRTLSupportForCheckMark()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			ListView listView = null;
			GridView gridView = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListView Margin='0,12,0,0' Background='LightCoral' SelectionMode='Multiple' Width='300'>
				        <ListViewItem Background='LightBlue'>
				            <TextBlock Text='Using long words is important however using longer words is of utmost importance' TextWrapping='WrapWholeWords' />
				        </ListViewItem>
				        <ListViewItem Background='LightGreen'>
				            <TextBlock Text='Using long words is important however using longer words is of utmost importance' TextTrimming='CharacterEllipsis' />
				        </ListViewItem>
				    </ListView>
				    <ListView Margin='0,12,0,0' Background='LightCoral' SelectionMode='Multiple' Width='300' ItemContainerStyle='{ThemeResource ListViewItemExpanded}'>
				        <ListViewItem Background='LightBlue'>
				            <TextBlock Text='Using long words is important however using longer words is of utmost importance' TextWrapping='WrapWholeWords' />
				        </ListViewItem>
				        <ListViewItem Background='LightGreen'>
				            <TextBlock Text='Using long words is important however using longer words is of utmost importance' TextTrimming='CharacterEllipsis' />
				        </ListViewItem>
				    </ListView>
				</StackPanel>
				""") as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				listView = (ListView)rootPanel.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				gridView = (GridView)rootPanel.FindName("gridView");
				VERIFY_IS_NOT_NULL(gridView);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listView.SelectAll();
				gridView.SelectAll();
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Description("Displays ListView and GridView controls with rounded corner styles.")]
		public async Task DisplayListAndGridViews()
		{
			TestCleanupWrapper cleanup;

			await DisplayListAndGridViews(
				false /*isInteractive*/, // Use 'true' for manual interactive testing
				true  /*enableRoundedListViewBaseItemChrome*/,
				false /*forceSelectionIndicatorModeInline*/,
				false /*forceSelectionIndicatorModeOverlay*/);
		}

		[TestMethod]
		[Description("Displays ListView and GridView controls with rounded corner styles and SelectionIndicatorMode forced to Inline.")]
		public async Task DisplayListAndGridViewsWithSelectionIndicatorModeInline()
		{
			TestCleanupWrapper cleanup;

			await DisplayListAndGridViews(
				false /*isInteractive*/, // Use 'true' for manual interactive testing
				true  /*enableRoundedListViewBaseItemChrome*/,
				true  /*forceSelectionIndicatorModeInline*/,
				false /*forceSelectionIndicatorModeOverlay*/);
		}

		[TestMethod]
		[Description("Displays ListView and GridView controls with rounded corner styles and SelectionIndicatorMode forced to Overlay.")]
		public async Task DisplayListAndGridViewsWithSelectionIndicatorModeOverlay()
		{
			TestCleanupWrapper cleanup;

			await DisplayListAndGridViews(
				false /*isInteractive*/, // Use 'true' for manual interactive testing
				true  /*enableRoundedListViewBaseItemChrome*/,
				false /*forceSelectionIndicatorModeInline*/,
				true  /*forceSelectionIndicatorModeOverlay*/);
		}

		[TestMethod]
		[Description("Displays ListView and GridView controls with square corner styles.")]
		public async Task DisplayListAndGridViewsWithOldStyles()
		{
			TestCleanupWrapper cleanup;

			await DisplayListAndGridViews(
				false /*isInteractive*/, // Use 'true' for manual interactive testing
				false /*enableRoundedListViewBaseItemChrome*/,
				false /*forceSelectionIndicatorModeInline*/,
				false /*forceSelectionIndicatorModeOverlay*/);
		}

		private async Task DisplayListAndGridViews(
			bool isInteractive,
			bool enableRoundedListViewBaseItemChrome,
			bool forceSelectionIndicatorModeInline,
			bool forceSelectionIndicatorModeOverlay)
		{
			await using var dictionaryHelper = await RoundedChromeDictionaryHelper.Create(enableRoundedListViewBaseItemChrome);

#if HAS_UNO
			// Scopes are disposed in reverse order of creation, as the product requires.
			using var featureSelectionIndicatorMode = (forceSelectionIndicatorModeInline || forceSelectionIndicatorModeOverlay)
				? ListViewBaseItemChromeRuntimeFeatures.Override(
					forceSelectionIndicatorModeInline: forceSelectionIndicatorModeInline ? true : null,
					forceSelectionIndicatorModeOverlay: forceSelectionIndicatorModeOverlay ? true : null)
				: null;
#endif

			CheckBox chkExit = null;
			CheckBox chkListViewIsEnabled = null;
			CheckBox chkGridViewIsEnabled = null;
			CheckBox chkEnableRoundedListViewBaseItemChrome = null;
			CheckBox chkForceSelectionIndicatorModeInline = null;
			CheckBox chkForceSelectionIndicatorModeOverlay = null;
			ListView listView1 = null;
			GridView gridView1 = null;
			ListViewItem listViewItem1 = null;
			GridViewItem gridViewItem1 = null;
			ComboBox cmbListViewSelectionMode = null;
			ComboBox cmbGridViewSelectionMode = null;

			var chkListViewIsEnabledCheckedRegistration = new SafeEventRegistration<CheckBox, RoutedEventHandler>("Checked");
			var chkListViewIsEnabledUncheckedRegistration = new SafeEventRegistration<CheckBox, RoutedEventHandler>("Unchecked");
			var chkGridViewIsEnabledCheckedRegistration = new SafeEventRegistration<CheckBox, RoutedEventHandler>("Checked");
			var chkGridViewIsEnabledUncheckedRegistration = new SafeEventRegistration<CheckBox, RoutedEventHandler>("Unchecked");
			var cmbListViewSelectionModeSelectionChangedRegistration = new SafeEventRegistration<ComboBox, SelectionChangedEventHandler>("SelectionChanged");
			var cmbGridViewSelectionModeSelectionChangedRegistration = new SafeEventRegistration<ComboBox, SelectionChangedEventHandler>("SelectionChanged");
			var cmbListViewSelectionModeSelectionChangedEvent = new Event();
			var cmbGridViewSelectionModeSelectionChangedEvent = new Event();

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <StackPanel.Resources>
				        <x:Double x:Key='ListViewItemDisabledThemeOpacity'>0.3</x:Double>
				        <x:Boolean x:Key='ListViewItemSelectionIndicatorVisualEnabled'>True</x:Boolean>

				        <CornerRadius x:Key='GridViewItemCornerRadius'>4</CornerRadius>
				        <CornerRadius x:Key='GridViewItemCheckBoxCornerRadius'>3</CornerRadius>
				        <Thickness x:Key='GridViewItemSelectedBorderThickness'>2</Thickness>
				        <SolidColorBrush x:Key='GridViewItemBackground' Color='#00000000' />
				        <SolidColorBrush x:Key='GridViewItemForeground' Color='#FFFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemPointerOverBorderBrush' Color='#37000000' />
				        <SolidColorBrush x:Key='GridViewItemSelectedBorderBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='GridViewItemSelectedPointerOverBorderBrush' Color='#FF49D3FF' />
				        <SolidColorBrush x:Key='GridViewItemSelectedPressedBorderBrush' Color='#FF01B7F6' />
				        <SolidColorBrush x:Key='GridViewItemSelectedDisabledBorderBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemSelectedInnerBorderBrush' Color='#FF454545' />
				        <SolidColorBrush x:Key='GridViewItemCheckBrush' Color='#D2000000' />
				        <SolidColorBrush x:Key='GridViewItemCheckPressedBrush' Color='#80000000' />
				        <SolidColorBrush x:Key='GridViewItemCheckDisabledBrush' Color='#87FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxBrush' Color='#B21C1C1C' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxPointerOverBrush' Color='#FF1A1A1A' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxPressedBrush' Color='#FF131313' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxDisabledBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxSelectedBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxSelectedPointerOverBrush' Color='#FF49D3FF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxSelectedPressedBrush' Color='#FF01B7F6' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxSelectedDisabledBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxPointerOverBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxPressedBorderBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemCheckBoxDisabledBorderBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundPointerOver' Color='#08FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemForegroundPointerOver' Color='#C8FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundSelected' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemForegroundSelected' Color='#FFFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundSelectedPointerOver' Color='#08FFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundPressed' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundSelectedPressed' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='GridViewItemBackgroundSelectedDisabled' Color='#08FFFFFF' />

				        <Style TargetType='GridViewItem'>
				            <Setter Property='FontFamily' Value='{ThemeResource ContentControlThemeFontFamily}' />
				            <Setter Property='FontSize' Value='{ThemeResource ControlContentThemeFontSize}' />
				            <Setter Property='Background' Value='{ThemeResource GridViewItemBackground}' />
				            <Setter Property='Foreground' Value='{ThemeResource GridViewItemForeground}' />
				            <Setter Property='TabNavigation' Value='Local' />
				            <Setter Property='IsHoldingEnabled' Value='True' />
				            <Setter Property='HorizontalContentAlignment' Value='Center' />
				            <Setter Property='VerticalContentAlignment' Value='Center' />
				            <Setter Property='Margin' Value='0,0,4,4' />
				            <Setter Property='MinWidth' Value='{ThemeResource GridViewItemMinWidth}' />
				            <Setter Property='MinHeight' Value='{ThemeResource GridViewItemMinHeight}' />
				            <Setter Property='AllowDrop' Value='True' />
				            <Setter Property='UseSystemFocusVisuals' Value='{StaticResource UseSystemFocusVisuals}' />
				            <Setter Property='FocusVisualMargin' Value='-2' />
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='GridViewItem'>
				                        <ListViewItemPresenter ContentTransitions='{TemplateBinding ContentTransitions}'
				                            x:Name='Root'
				                            Control.IsTemplateFocusTarget='True'
				                            FocusVisualMargin='{TemplateBinding FocusVisualMargin}'
				                            SelectionCheckMarkVisualEnabled='{ThemeResource GridViewItemSelectionCheckMarkVisualEnabled}'
				                            CheckBrush='{ThemeResource GridViewItemCheckBrush}'
				                            CheckPressedBrush='{ThemeResource GridViewItemCheckPressedBrush}'
				                            CheckDisabledBrush='{ThemeResource GridViewItemCheckDisabledBrush}'
				                            CheckBoxBrush='{ThemeResource GridViewItemCheckBoxBrush}'
				                            CheckBoxPointerOverBrush='{ThemeResource GridViewItemCheckBoxPointerOverBrush}'
				                            CheckBoxPressedBrush='{ThemeResource GridViewItemCheckBoxPressedBrush}'
				                            CheckBoxDisabledBrush='{ThemeResource GridViewItemCheckBoxDisabledBrush}'
				                            CheckBoxSelectedBrush='{ThemeResource GridViewItemCheckBoxSelectedBrush}'
				                            CheckBoxSelectedPointerOverBrush='{ThemeResource GridViewItemCheckBoxSelectedPointerOverBrush}'
				                            CheckBoxSelectedPressedBrush='{ThemeResource GridViewItemCheckBoxSelectedPressedBrush}'
				                            CheckBoxSelectedDisabledBrush='{ThemeResource GridViewItemCheckBoxSelectedDisabledBrush}'
				                            CheckBoxBorderBrush='{ThemeResource GridViewItemCheckBoxBorderBrush}'
				                            CheckBoxPointerOverBorderBrush='{ThemeResource GridViewItemCheckBoxPointerOverBorderBrush}'
				                            CheckBoxPressedBorderBrush='{ThemeResource GridViewItemCheckBoxPressedBorderBrush}'
				                            CheckBoxDisabledBorderBrush='{ThemeResource GridViewItemCheckBoxDisabledBorderBrush}'
				                            DragBackground='{ThemeResource GridViewItemDragBackground}'
				                            DragForeground='{ThemeResource GridViewItemDragForeground}'
				                            FocusBorderBrush='{ThemeResource GridViewItemFocusBorderBrush}'
				                            PlaceholderBackground='{ThemeResource GridViewItemPlaceholderBackground}'
				                            PointerOverBorderBrush='{ThemeResource GridViewItemPointerOverBorderBrush}'
				                            PointerOverBackground='{ThemeResource GridViewItemBackgroundPointerOver}'
				                            PointerOverForeground='{ThemeResource GridViewItemForegroundPointerOver}'
				                            SelectedBackground='{ThemeResource GridViewItemBackgroundSelected}'
				                            SelectedForeground='{ThemeResource GridViewItemForegroundSelected}'
				                            SelectedPointerOverBackground='{ThemeResource GridViewItemBackgroundSelectedPointerOver}'
				                            PressedBackground='{ThemeResource GridViewItemBackgroundPressed}'
				                            SelectedPressedBackground='{ThemeResource GridViewItemBackgroundSelectedPressed}'
				                            SelectedDisabledBackground='{ThemeResource GridViewItemBackgroundSelectedDisabled}'
				                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
				                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
				                            ReorderHintOffset='{ThemeResource GridViewItemReorderHintThemeOffset}'
				                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
				                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
				                            ContentMargin='{TemplateBinding Padding}'
				                            CheckMode='{ThemeResource GridViewItemCheckMode}'
				                            SelectedBorderThickness='{ThemeResource GridViewItemSelectedBorderThickness}'
				                            SelectedBorderBrush='{ThemeResource GridViewItemSelectedBorderBrush}'
				                            SelectedPointerOverBorderBrush='{ThemeResource GridViewItemSelectedPointerOverBorderBrush}'
				                            SelectedPressedBorderBrush='{ThemeResource GridViewItemSelectedPressedBorderBrush}'
				                            SelectedDisabledBorderBrush='{ThemeResource GridViewItemSelectedDisabledBorderBrush}'
				                            SelectedInnerBorderBrush='{ThemeResource GridViewItemSelectedInnerBorderBrush}'
				                            CornerRadius='{ThemeResource GridViewItemCornerRadius}'
				                            CheckBoxCornerRadius='{ThemeResource GridViewItemCheckBoxCornerRadius}'>
				                        </ListViewItemPresenter>
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				        </Style>

				        <CornerRadius x:Key='ListViewItemCornerRadius'>4</CornerRadius>
				        <CornerRadius x:Key='ListViewItemCheckBoxCornerRadius'>3</CornerRadius>
				        <CornerRadius x:Key='ListViewItemSelectionIndicatorCornerRadius'>1.5</CornerRadius>
				        <SolidColorBrush x:Key='ListViewItemBackground' Color='#00000000' />
				        <SolidColorBrush x:Key='ListViewItemForeground' Color='#FFFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBrush' Color='#D2000000' />
				        <SolidColorBrush x:Key='ListViewItemCheckPressedBrush' Color='#80000000' />
				        <SolidColorBrush x:Key='ListViewItemCheckDisabledBrush' Color='#87FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxBrush' Color='#B21C1C1C' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxPointerOverBrush' Color='#FF1A1A1A' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxPressedBrush' Color='#FF131313' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxDisabledBrush' Color='#19000000' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxSelectedBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxSelectedPointerOverBrush' Color='#FF49D3FF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxSelectedPressedBrush' Color='#FF01B7F6' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxSelectedDisabledBrush' Color='#28FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxPointerOverBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxPressedBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemCheckBoxDisabledBorderBrush' Color='#8AFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundPointerOver' Color='#08FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemForegroundPointerOver' Color='#C8FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundSelected' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemForegroundSelected' Color='#FFFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundSelectedPointerOver' Color='#08FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundPressed' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundSelectedPressed' Color='#0FFFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemBackgroundSelectedDisabled' Color='#08FFFFFF' />
				        <SolidColorBrush x:Key='ListViewItemSelectionIndicatorBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='ListViewItemSelectionIndicatorPointerOverBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='ListViewItemSelectionIndicatorPressedBrush' Color='#FF01C0FB' />
				        <SolidColorBrush x:Key='ListViewItemSelectionIndicatorDisabledBrush' Color='#28FFFFFF' />
				        <x:Double x:Key='ListViewItemMinHeight'>40</x:Double>

				        <Style TargetType='ListViewItem'>
				            <Setter Property='FontFamily' Value='{ThemeResource ContentControlThemeFontFamily}' />
				            <Setter Property='FontSize' Value='{ThemeResource ControlContentThemeFontSize}' />
				            <Setter Property='Background' Value='{ThemeResource ListViewItemBackground}' />
				            <Setter Property='Foreground' Value='{ThemeResource ListViewItemForeground}' />
				            <Setter Property='TabNavigation' Value='LocaL' />
				            <Setter Property='IsHoldingEnabled' Value='True' />
				            <Setter Property='Padding' Value='16,0,12,0' />
				            <Setter Property='HorizontalContentAlignment' Value='Left' />
				            <Setter Property='VerticalContentAlignment' Value='Center' />
				            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
				            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
				            <Setter Property='AllowDrop' Value='True' />
				            <Setter Property='UseSystemFocusVisuals' Value='{StaticResource UseSystemFocusVisuals}' />
				            <Setter Property='FocusVisualMargin' Value='0' />
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='ListViewItem'>
				                        <ListViewItemPresenter ContentTransitions='{TemplateBinding ContentTransitions}'
				                            x:Name='Root'
				                            Control.IsTemplateFocusTarget='True'
				                            FocusVisualMargin='{TemplateBinding FocusVisualMargin}'
				                            SelectionCheckMarkVisualEnabled='{ThemeResource ListViewItemSelectionCheckMarkVisualEnabled}'
				                            CheckBrush='{ThemeResource ListViewItemCheckBrush}'
				                            CheckPressedBrush='{ThemeResource ListViewItemCheckPressedBrush}'
				                            CheckDisabledBrush='{ThemeResource ListViewItemCheckDisabledBrush}'
				                            CheckBoxBrush='{ThemeResource ListViewItemCheckBoxBrush}'
				                            CheckBoxPointerOverBrush='{ThemeResource ListViewItemCheckBoxPointerOverBrush}'
				                            CheckBoxPressedBrush='{ThemeResource ListViewItemCheckBoxPressedBrush}'
				                            CheckBoxDisabledBrush='{ThemeResource ListViewItemCheckBoxDisabledBrush}'
				                            CheckBoxSelectedBrush='{ThemeResource ListViewItemCheckBoxSelectedBrush}'
				                            CheckBoxSelectedPointerOverBrush='{ThemeResource ListViewItemCheckBoxSelectedPointerOverBrush}'
				                            CheckBoxSelectedPressedBrush='{ThemeResource ListViewItemCheckBoxSelectedPressedBrush}'
				                            CheckBoxSelectedDisabledBrush='{ThemeResource ListViewItemCheckBoxSelectedDisabledBrush}'
				                            CheckBoxBorderBrush='{ThemeResource ListViewItemCheckBoxBorderBrush}'
				                            CheckBoxPointerOverBorderBrush='{ThemeResource ListViewItemCheckBoxPointerOverBorderBrush}'
				                            CheckBoxPressedBorderBrush='{ThemeResource ListViewItemCheckBoxPressedBorderBrush}'
				                            CheckBoxDisabledBorderBrush='{ThemeResource ListViewItemCheckBoxDisabledBorderBrush}'
				                            DragBackground='{ThemeResource ListViewItemDragBackground}'
				                            DragForeground='{ThemeResource ListViewItemDragForeground}'
				                            FocusBorderBrush='{ThemeResource ListViewItemFocusBorderBrush}'
				                            FocusSecondaryBorderBrush='{ThemeResource ListViewItemFocusSecondaryBorderBrush}'
				                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackground}'
				                            PointerOverBackground='{ThemeResource ListViewItemBackgroundPointerOver}'
				                            PointerOverForeground='{ThemeResource ListViewItemForegroundPointerOver}'
				                            SelectedBackground='{ThemeResource ListViewItemBackgroundSelected}'
				                            SelectedForeground='{ThemeResource ListViewItemForegroundSelected}'
				                            SelectedPointerOverBackground='{ThemeResource ListViewItemBackgroundSelectedPointerOver}'
				                            SelectionIndicatorVisualEnabled='{ThemeResource ListViewItemSelectionIndicatorVisualEnabled}'
				                            SelectionIndicatorBrush='{ThemeResource ListViewItemSelectionIndicatorBrush}'
				                            SelectionIndicatorPointerOverBrush='{ThemeResource ListViewItemSelectionIndicatorPointerOverBrush}'
				                            SelectionIndicatorPressedBrush='{ThemeResource ListViewItemSelectionIndicatorPressedBrush}'
				                            SelectionIndicatorDisabledBrush='{ThemeResource ListViewItemSelectionIndicatorDisabledBrush}'
				                            PressedBackground='{ThemeResource ListViewItemBackgroundPressed}'
				                            SelectedPressedBackground='{ThemeResource ListViewItemBackgroundSelectedPressed}'
				                            SelectedDisabledBackground='{ThemeResource ListViewItemBackgroundSelectedDisabled}'
				                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
				                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
				                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
				                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
				                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
				                            ContentMargin='{TemplateBinding Padding}'
				                            CheckMode='{ThemeResource ListViewItemCheckMode}'
				                            CornerRadius='{ThemeResource ListViewItemCornerRadius}'
				                            SelectionIndicatorCornerRadius='{ThemeResource ListViewItemSelectionIndicatorCornerRadius}'
				                            CheckBoxCornerRadius='{ThemeResource ListViewItemCheckBoxCornerRadius}'>
				                        </ListViewItemPresenter>
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				        </Style>

				    </StackPanel.Resources>
				    <StackPanel Orientation='Horizontal'>
				        <CheckBox x:Name='chkExit' Content='Exit?' Margin='2'/>
				        <CheckBox x:Name='chkEnableRoundedListViewBaseItemChrome' Content='EnableRoundedListViewBaseItemChrome?' Margin='2' IsEnabled='False'/>
				    </StackPanel>
				    <StackPanel Orientation='Horizontal'>
				        <CheckBox x:Name='chkForceSelectionIndicatorModeInline' Content='ForceSelectionIndicatorModeInline?' Margin='2' IsEnabled='False'/>
				        <CheckBox x:Name='chkForceSelectionIndicatorModeOverlay' Content='ForceSelectionIndicatorModeOverlay?' Margin='2' IsEnabled='False'/>
				    </StackPanel>
				    <StackPanel Orientation='Horizontal'>
				        <StackPanel>
				            <StackPanel Orientation='Horizontal'>
				                <TextBlock Text='ListView - SelectionMode=' VerticalAlignment='Center' Margin='2'/>
				                <ComboBox x:Name='cmbListViewSelectionMode' SelectedIndex='2'>
				                    <ComboBoxItem>None</ComboBoxItem>
				                    <ComboBoxItem>Single</ComboBoxItem>
				                   <ComboBoxItem>Multiple</ComboBoxItem>
				                   <ComboBoxItem>Extended</ComboBoxItem>
				                </ComboBox>
				            </StackPanel>
				            <CheckBox x:Name='chkListViewIsEnabled' Content='IsEnabled?' IsChecked='True' Margin='2'/>
				            <ListView x:Name='listView1' CanReorderItems='True' AllowDrop='True' SelectionMode='Multiple' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <ListViewItem x:Name='listViewItem1' Content='List Item 1'/>
				                <ListViewItem x:Name='listViewItem2' Content='List Item 2'/>
				                <ListViewItem x:Name='listViewItem3' Content='List Item 3' IsSelected='true'/>
				            </ListView>
				        </StackPanel>
				        <StackPanel>
				            <StackPanel Orientation='Horizontal'>
				                <TextBlock Text='GridView - SelectionMode=' VerticalAlignment='Center' Margin='2'/>
				                <ComboBox x:Name='cmbGridViewSelectionMode' SelectedIndex='2'>
				                    <ComboBoxItem>None</ComboBoxItem>
				                    <ComboBoxItem>Single</ComboBoxItem>
				                    <ComboBoxItem>Multiple</ComboBoxItem>
				                    <ComboBoxItem>Extended</ComboBoxItem>
				                </ComboBox>
				            </StackPanel>
				            <CheckBox x:Name='chkGridViewIsEnabled' Content='IsEnabled?' IsChecked='True' Margin='2'/>
				            <GridView x:Name='gridView1' CanReorderItems='True' AllowDrop='True' SelectionMode='Multiple' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <GridViewItem x:Name='gridViewItem1' Content='Item 1' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem2' Content='Item 2' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem3' Content='Item 3' MinWidth='120' MinHeight='60' IsSelected='true'/>
				            </GridView>
				        </StackPanel>
				    </StackPanel>
				    <StackPanel Orientation='Horizontal'>
				        <StackPanel>
				            <TextBlock Text='ListView - SelectionMode=Extended:' Margin='2'/>
				            <ListView x:Name='listView2' CanReorderItems='True' AllowDrop='True' SelectionMode='Extended' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <ListViewItem x:Name='listViewItem4' Content='List Item 4' Height='5'/>
				                <ListViewItem x:Name='listViewItem5' Content='List Item 5' Height='70'/>
				                <ListViewItem x:Name='listViewItem6' Content='List Item 6' Height='90' IsSelected='true'/>
				            </ListView>
				        </StackPanel>
				        <StackPanel>
				            <TextBlock Text='GridView - SelectionMode=Extended:' Margin='2'/>
				            <GridView x:Name='gridView2' CanReorderItems='True' AllowDrop='True' SelectionMode='Extended' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <GridViewItem x:Name='gridViewItem4' Content='Item 4' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem5' Content='Item 5' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem6' Content='Item 6' MinWidth='120' MinHeight='60' IsSelected='true'/>
				            </GridView>
				        </StackPanel>
				    </StackPanel>
				    <StackPanel Orientation='Horizontal'>
				        <StackPanel>
				            <TextBlock Text='ListView - SelectionMode=Single:' Margin='2'/>
				            <ListView x:Name='listView3' CanReorderItems='True' AllowDrop='True' SelectionMode='Single' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <ListViewItem x:Name='listViewItem7' Content='List Item 7'/>
				                <ListViewItem x:Name='listViewItem8' Content='List Item 8'/>
				                <ListViewItem x:Name='listViewItem9' Content='List Item 9' IsSelected='true'/>
				            </ListView>
				        </StackPanel>
				        <StackPanel>
				            <TextBlock Text='GridView - SelectionMode=Single:' Margin='2'/>
				            <GridView x:Name='gridView3' CanReorderItems='True' AllowDrop='True' SelectionMode='Single' Width='300' HorizontalAlignment='Left' Margin='2'>
				                <GridViewItem x:Name='gridViewItem7' Content='Item 7' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem8' Content='Item 8' MinWidth='120' MinHeight='60'/>
				                <GridViewItem x:Name='gridViewItem9' Content='Item 9' MinWidth='120' MinHeight='60' IsSelected='true'/>
				            </GridView>
				        </StackPanel>
				    </StackPanel>
				</StackPanel>
				""") as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				chkExit = (CheckBox)rootPanel.FindName("chkExit");
				VERIFY_IS_NOT_NULL(chkExit);

				chkEnableRoundedListViewBaseItemChrome = (CheckBox)rootPanel.FindName("chkEnableRoundedListViewBaseItemChrome");
				VERIFY_IS_NOT_NULL(chkEnableRoundedListViewBaseItemChrome);

				chkForceSelectionIndicatorModeInline = (CheckBox)rootPanel.FindName("chkForceSelectionIndicatorModeInline");
				VERIFY_IS_NOT_NULL(chkForceSelectionIndicatorModeInline);

				chkForceSelectionIndicatorModeOverlay = (CheckBox)rootPanel.FindName("chkForceSelectionIndicatorModeOverlay");
				VERIFY_IS_NOT_NULL(chkForceSelectionIndicatorModeOverlay);

				chkListViewIsEnabled = (CheckBox)rootPanel.FindName("chkListViewIsEnabled");
				VERIFY_IS_NOT_NULL(chkListViewIsEnabled);

				chkGridViewIsEnabled = (CheckBox)rootPanel.FindName("chkGridViewIsEnabled");
				VERIFY_IS_NOT_NULL(chkGridViewIsEnabled);

				listView1 = (ListView)rootPanel.FindName("listView1");
				VERIFY_IS_NOT_NULL(listView1);

				listViewItem1 = (ListViewItem)listView1.Items[0]; // UNO TODO: FindName doesn't see items which aren't realized yet.
				VERIFY_IS_NOT_NULL(listViewItem1);

				gridView1 = (GridView)rootPanel.FindName("gridView1");
				VERIFY_IS_NOT_NULL(gridView1);

				gridViewItem1 = (GridViewItem)gridView1.Items[0]; // UNO TODO: FindName doesn't see items which aren't realized yet.
				VERIFY_IS_NOT_NULL(gridViewItem1);

				cmbListViewSelectionMode = (ComboBox)rootPanel.FindName("cmbListViewSelectionMode");
				VERIFY_IS_NOT_NULL(cmbListViewSelectionMode);

				cmbGridViewSelectionMode = (ComboBox)rootPanel.FindName("cmbGridViewSelectionMode");
				VERIFY_IS_NOT_NULL(cmbGridViewSelectionMode);

				chkListViewIsEnabledCheckedRegistration.Attach(chkListViewIsEnabled, (s, e) =>
				{
					listView1.IsEnabled = true;
				});

				chkListViewIsEnabledUncheckedRegistration.Attach(chkListViewIsEnabled, (s, e) =>
				{
					listView1.IsEnabled = false;
				});

				chkGridViewIsEnabledCheckedRegistration.Attach(chkGridViewIsEnabled, (s, e) =>
				{
					gridView1.IsEnabled = true;
				});

				chkGridViewIsEnabledUncheckedRegistration.Attach(chkGridViewIsEnabled, (s, e) =>
				{
					gridView1.IsEnabled = false;
				});

				cmbListViewSelectionModeSelectionChangedRegistration.Attach(cmbListViewSelectionMode, (sender, args) =>
				{
					listView1.SelectionMode = (ListViewSelectionMode)((ComboBox)sender).SelectedIndex;
					cmbListViewSelectionModeSelectionChangedEvent.Set();
				});

				cmbGridViewSelectionModeSelectionChangedRegistration.Attach(cmbGridViewSelectionMode, (sender, args) =>
				{
					gridView1.SelectionMode = (ListViewSelectionMode)((ComboBox)sender).SelectedIndex;
					cmbGridViewSelectionModeSelectionChangedEvent.Set();
				});

				chkEnableRoundedListViewBaseItemChrome.IsChecked = enableRoundedListViewBaseItemChrome;
				chkForceSelectionIndicatorModeInline.IsChecked = forceSelectionIndicatorModeInline;
				chkForceSelectionIndicatorModeOverlay.IsChecked = forceSelectionIndicatorModeOverlay;

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem1.IsSelected = true;
				gridViewItem1.IsSelected = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem1.IsSelected = false;
				gridViewItem1.IsSelected = false;
			});
			await WindowHelper.WaitForIdle();

			if (isInteractive)
			{
				bool exit = false;
				do
				{
					await WindowHelper.SynchronouslyTickUIThread(50);

					await RunOnUIThread(() =>
					{
						exit = chkExit.IsChecked.Value;
					});
				}
				while (!exit);

				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that not setting a pointerover brush retains the existing brush.")]
		public async Task ValidateChromeNoPointerOverBrush()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			ListViewItem pointerOver = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <Grid.Resources>
				        <Style TargetType='ListViewItem'>
				            <Setter Property = 'FontFamily' Value = '{ThemeResource ContentControlThemeFontFamily}' />
				            <Setter Property = 'FontSize' Value = '{ThemeResource ControlContentThemeFontSize}' />
				            <Setter Property='Background' Value='Transparent' />
				            <Setter Property='TabNavigation' Value='Local' />
				            <Setter Property='IsHoldingEnabled' Value='True' />
				            <Setter Property='Padding' Value='12,0,12,0' />
				            <Setter Property='HorizontalContentAlignment' Value='Left' />
				            <Setter Property='VerticalContentAlignment' Value='Center' />
				            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
				            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='ListViewItem'>
				                        <ListViewItemPresenter
				                            ContentTransitions='{TemplateBinding ContentTransitions}'
				                            SelectionCheckMarkVisualEnabled='True'
				                            CheckBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
				                            CheckBoxBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
				                            DragBackground='{ThemeResource ListViewItemDragBackgroundThemeBrush}'
				                            DragForeground='{ThemeResource ListViewItemDragForegroundThemeBrush}'
				                            FocusBorderBrush='{ThemeResource SystemControlForegroundAltHighBrush}'
				                            FocusSecondaryBorderBrush='{ThemeResource SystemControlForegroundBaseHighBrush}'
				                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackgroundThemeBrush}'
				                            PointerOverBackground='{ThemeResource SystemControlForegroundListLowBrush}'
				                            SelectedBackground='{ThemeResource SystemControlHighlightAltListAccentLowBrush}'
				                            SelectedForeground='{ThemeResource ListViewItemSelectedForegroundThemeBrush}'
				                            SelectedPointerOverBackground='{ThemeResource SystemControlHighlightAltListAccentMediumBrush}'
				                            PressedBackground='{ThemeResource SystemControlForegroundListMediumBrush}'
				                            SelectedPressedBackground='{ThemeResource SystemControlHighlightAltListAccentHighBrush}'
				                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
				                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
				                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
				                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
				                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
				                            ContentMargin='{TemplateBinding Padding}'
				                            CheckMode='Inline' />
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				        </Style>
				    </Grid.Resources>
				    <ListViewItem x:Name='pointerOver' Content='Item 1' />
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				pointerOver = (ListViewItem)rootPanel.FindName("pointerOver");
				VERIFY_IS_NOT_NULL(pointerOver);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(pointerOver, "PointerOver", false);
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that setting a selected brush works properly.")]
		public async Task ValidateSelectedForegroundBrush()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			ListViewItem pointerOver = null;
			ListViewItem selected = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <Grid.Resources>
				        <Style TargetType='ListViewItem'>
				            <Setter Property = 'FontFamily' Value = '{ThemeResource ContentControlThemeFontFamily}' />
				            <Setter Property = 'FontSize' Value = '{ThemeResource ControlContentThemeFontSize}' />
				            <Setter Property='Background' Value='Transparent' />
				            <Setter Property='TabNavigation' Value='Local' />
				            <Setter Property='IsHoldingEnabled' Value='True' />
				            <Setter Property='Padding' Value='12,0,12,0' />
				            <Setter Property='HorizontalContentAlignment' Value='Left' />
				            <Setter Property='VerticalContentAlignment' Value='Center' />
				            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
				            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='ListViewItem'>
				                        <ListViewItemPresenter
				                            ContentTransitions='{TemplateBinding ContentTransitions}'
				                            SelectionCheckMarkVisualEnabled='True'
				                            CheckBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
				                            CheckBoxBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
				                            DragBackground='{ThemeResource ListViewItemDragBackgroundThemeBrush}'
				                            DragForeground='{ThemeResource ListViewItemDragForegroundThemeBrush}'
				                            FocusBorderBrush='{ThemeResource SystemControlForegroundAltHighBrush}'
				                            FocusSecondaryBorderBrush='{ThemeResource SystemControlForegroundBaseHighBrush}'
				                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackgroundThemeBrush}'
				                            PointerOverBackground='{ThemeResource SystemControlForegroundListLowBrush}'
				                            PointerOverForeground='Blue'
				                            SelectedBackground='{ThemeResource SystemControlHighlightAltListAccentLowBrush}'
				                            SelectedForeground='Red'
				                            SelectedPointerOverBackground='{ThemeResource SystemControlHighlightAltListAccentMediumBrush}'
				                            PressedBackground='{ThemeResource SystemControlForegroundListMediumBrush}'
				                            SelectedPressedBackground='{ThemeResource SystemControlHighlightAltListAccentHighBrush}'
				                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
				                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
				                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
				                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
				                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
				                            ContentMargin='{TemplateBinding Padding}'
				                            CheckMode='Inline' />
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				        </Style>
				    </Grid.Resources>
				    <StackPanel>
				        <ListViewItem x:Name='pointerOver' Content='PointerOver' />
				        <ListViewItem x:Name='selected' Content='Selected' />
				    </StackPanel>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				pointerOver = (ListViewItem)rootPanel.FindName("pointerOver");
				VERIFY_IS_NOT_NULL(pointerOver);

				selected = (ListViewItem)rootPanel.FindName("selected");
				VERIFY_IS_NOT_NULL(selected);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(pointerOver, "PointerOver", false);

				VisualStateManager.GoToState(selected, "Selected", false);
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that changing the CheckMode property functions properly.")]
		public async Task ValidateChangeCheckMode()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			ListViewItem selected = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListViewItem x:Name='selected' Content='Selected' Width='200' Height='100' />
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				selected = (ListViewItem)rootPanel.FindName("selected");
				VERIFY_IS_NOT_NULL(selected);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(selected, "Selected", false);
				VisualStateManager.GoToState(selected, "MultiSelectEnabled", false);
			});
			await WindowHelper.WaitForIdle();

			// change the CheckMode from inline (default for ListViewItem) to overlay
			await RunOnUIThread(() =>
			{
				ListViewItemPresenter lvip = (ListViewItemPresenter)VisualTreeHelper.GetChild(selected, 0);

				lvip.CheckMode = ListViewItemPresenterCheckMode.Overlay;
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Description("Validates that using GridViewItemPresenter does not cause a crash with PointerOver state.")]
		public async Task CanUseGridViewItemPresenter()
		{
			TestCleanupWrapper cleanup;

			ListViewItem pointerOver = null;
			ListViewItem selected = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <Grid.Resources>
				        <Style TargetType='ListViewItem'>
				            <Setter Property = 'FontFamily' Value = '{ThemeResource ContentControlThemeFontFamily}' />
				            <Setter Property = 'FontSize' Value = '{ThemeResource ControlContentThemeFontSize}' />
				            <Setter Property='Background' Value='Transparent' />
				            <Setter Property='TabNavigation' Value='Local' />
				            <Setter Property='IsHoldingEnabled' Value='True' />
				            <Setter Property='Padding' Value='12,0,12,0' />
				            <Setter Property='HorizontalContentAlignment' Value='Left' />
				            <Setter Property='VerticalContentAlignment' Value='Center' />
				            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
				            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='ListViewItem'>
				                        <GridViewItemPresenter
				                            ContentTransitions='{TemplateBinding ContentTransitions}'
				                            SelectionCheckMarkVisualEnabled='True'
				                            CheckBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
				                            DragBackground='{ThemeResource ListViewItemDragBackgroundThemeBrush}'
				                            DragForeground='{ThemeResource ListViewItemDragForegroundThemeBrush}'
				                            FocusBorderBrush='{ThemeResource SystemControlForegroundAltHighBrush}'
				                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackgroundThemeBrush}'
				                            PointerOverBackground='{ThemeResource SystemControlForegroundListLowBrush}'
				                            SelectedBackground='{ThemeResource SystemControlHighlightAltListAccentLowBrush}'
				                            SelectedForeground='Red'
				                            SelectedPointerOverBackground='{ThemeResource SystemControlHighlightAltListAccentMediumBrush}'
				                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
				                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
				                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
				                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
				                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
				                            ContentMargin='{TemplateBinding Padding}' />
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				        </Style>
				    </Grid.Resources>
				    <StackPanel>
				        <ListViewItem x:Name='pointerOver' Content='PointerOver' />
				        <ListViewItem x:Name='selected' Content='Selected' />
				    </StackPanel>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				pointerOver = (ListViewItem)rootPanel.FindName("pointerOver");
				VERIFY_IS_NOT_NULL(pointerOver);

				selected = (ListViewItem)rootPanel.FindName("selected");
				VERIFY_IS_NOT_NULL(selected);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(pointerOver, "PointerOver", false);

				VisualStateManager.GoToState(selected, "Selected", false);
			});
			await WindowHelper.WaitForIdle();

			// no crash
		}

		[TestMethod]
		[Description("Validates that the ListViewItemPresenter has the same size as the ListViewItem it's contained in.")]
		public async Task VerifyLVIPHasSameSizeAsLVI()
		{
			TestCleanupWrapper cleanup;

			ListViewItem listViewItem = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListViewItem x:Name='listViewItem' Width='88' Content='Item #1'/>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				listViewItem = (ListViewItem)rootPanel.FindName("listViewItem");
				VERIFY_IS_NOT_NULL(listViewItem);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				ListViewItemPresenter lvip = (ListViewItemPresenter)VisualTreeHelper.GetChild(listViewItem, 0);

				VERIFY_ARE_EQUAL(lvip.ActualWidth, listViewItem.ActualWidth);
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Verifies ListViewItem.ContentTemplateRoot property with rounded corner styles.")]
		public async Task VerifyListViewItemContentTemplateRoot()
		{
			TestCleanupWrapper cleanup;

			await VerifyListViewItemContentTemplateRoot(true /*enableRoundedListViewBaseItemChrome*/);
		}

		[TestMethod]
		[Description("Verifies ListViewItem.ContentTemplateRoot property with square corner styles.")]
		public async Task VerifyListViewItemContentTemplateRootWithOldStyles()
		{
			TestCleanupWrapper cleanup;

			await VerifyListViewItemContentTemplateRoot(false /*enableRoundedListViewBaseItemChrome*/);
		}

		private async Task VerifyListViewItemContentTemplateRoot(bool enableRoundedListViewBaseItemChrome)
		{
			await using var dictionaryHelper = await RoundedChromeDictionaryHelper.Create(enableRoundedListViewBaseItemChrome);

			ListViewItem listViewItem1 = null;
			ListViewItem listViewItem2 = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				    <ListViewItem x:Name='listViewItem1' Content='Item #1'/>
				    <ListViewItem x:Name='listViewItem2'>
				        <Button x:Name='btnLVI2' Content='Item #2 Button'/>
				    </ListViewItem>
				</StackPanel>
				""") as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				listViewItem1 = (ListViewItem)rootPanel.FindName("listViewItem1");
				VERIFY_IS_NOT_NULL(listViewItem1);

				listViewItem2 = (ListViewItem)rootPanel.FindName("listViewItem2");
				VERIFY_IS_NOT_NULL(listViewItem2);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var listViewItemPresenter1 = (ListViewItemPresenter)VisualTreeHelper.GetChild(listViewItem1, 0);
				VERIFY_IS_NOT_NULL(listViewItemPresenter1);
				var listViewItemContentTemplateRoot1 = listViewItem1.ContentTemplateRoot;
				VERIFY_IS_NOT_NULL(listViewItemContentTemplateRoot1);
				var textBlock = (TextBlock)listViewItemContentTemplateRoot1;
				VERIFY_IS_NOT_NULL(textBlock);
				var listViewItemPresenterContent1 = listViewItemPresenter1.Content;
				VERIFY_IS_NOT_NULL(listViewItemPresenterContent1);
				var listViewItemPresenterContentAsString1 = listViewItemPresenterContent1 as string;
				VERIFY_ARE_EQUAL(listViewItemPresenterContentAsString1, textBlock.Text);

				var listViewItemPresenter2 = (ListViewItemPresenter)VisualTreeHelper.GetChild(listViewItem2, 0);
				VERIFY_IS_NOT_NULL(listViewItemPresenter2);
				var listViewItemContentTemplateRoot2 = listViewItem2.ContentTemplateRoot;
				VERIFY_IS_NOT_NULL(listViewItemContentTemplateRoot2);
				var button = (Button)listViewItemContentTemplateRoot2;
				VERIFY_IS_NOT_NULL(button);
				var listViewItemPresenterContent2 = listViewItemPresenter2.Content;
				VERIFY_IS_NOT_NULL(listViewItemPresenterContent2);
				VERIFY_ARE_EQUAL((Button)listViewItemPresenterContent2, button);
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that focus rect is rendered correctly.")]
		public async Task VerifyLVIFocusRect()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			ListViewItem listViewItem = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListViewItem x:Name='listViewItem' Width='88' Height='44' Content='Something' />
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				listViewItem = (ListViewItem)rootPanel.FindName("listViewItem");
				VERIFY_IS_NOT_NULL(listViewItem);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VisualStateManager.GoToState(listViewItem, "Focused", false);
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires touch flick injection (InputHelper.Flick is not available)")]
		[Description("Validates that we don't hit the Pressed visual state while panning.")]
		public async Task VerifyNoPressedStateWhenPanning()
		{
			TestCleanupWrapper cleanup;

			ListView listView = null;
			ListViewItem listViewItem = null;

			VisualStateGroup commonStatesVisualStateGroup;
			var stateChangedEvent = new Event();
			var stateChangedRegistration = new SafeEventRegistration<VisualStateGroup, VisualStateChangedEventHandler>("CurrentStateChanged");

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListView x:Name='listView' Width='200' Height='300' HorizontalAlignment='Center' VerticalAlignment='Center' ItemContainerStyle='{ThemeResource ListViewItemExpanded}'>
				    </ListView>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				listView = (ListView)rootPanel.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				for (int i = 0; i < 20; ++i)
				{
					listView.Items.Add(i);
				}

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
				VERIFY_IS_NOT_NULL(listViewItem);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Register for VisualStateGroup.CurrentStateChanged
				var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listViewItem, 0);
				var groups = VisualStateManager.GetVisualStateGroups(templateRoot);

				VERIFY_IS_TRUE(groups.Count > 0);

				commonStatesVisualStateGroup = groups[0];

				stateChangedRegistration.Attach(commonStatesVisualStateGroup, (object sender, VisualStateChangedEventArgs args) =>
				{
					stateChangedEvent.Set();
				});
			});
			await WindowHelper.WaitForIdle();

			// UNO TODO: InputHelper has no Flick(listView, FlickDirection.North); a pan towards the north is the closest available gesture.
			InputHelper.PanFromCenter(listView, 0, -200, 1.0);
			await WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(stateChangedEvent.HasFired());
		}

		[TestMethod]
		[Description("Validates that go into the Pressed visual state when quickly tapping the item.")]
		public async Task VerifyPressedStateWhenTapping()
		{
			TestCleanupWrapper cleanup;

			ListView listView = null;
			ListViewItem listViewItem = null;

			VisualStateGroup commonStatesVisualStateGroup;
			var stateChangedEvent = new Event();
			var stateChangedRegistration = new SafeEventRegistration<VisualStateGroup, VisualStateChangedEventHandler>("CurrentStateChanged");

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListView x:Name='listView' Width='200' Height='300' HorizontalAlignment='Center' VerticalAlignment='Center'
				              ItemContainerStyle='{ThemeResource ListViewItemExpanded}' SelectionMode='Single'>
				    </ListView>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				listView = (ListView)rootPanel.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				for (int i = 0; i < 20; ++i)
				{
					listView.Items.Add(i);
				}

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem = (ListViewItem)listView.ContainerFromIndex(0);
				VERIFY_IS_NOT_NULL(listViewItem);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Register for VisualStateGroup.CurrentStateChanged
				var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(listViewItem, 0);
				var groups = VisualStateManager.GetVisualStateGroups(templateRoot);

				VERIFY_IS_TRUE(groups.Count > 0);

				commonStatesVisualStateGroup = groups[0];

				stateChangedRegistration.Attach(commonStatesVisualStateGroup, (object sender, VisualStateChangedEventArgs args) =>
				{
					stateChangedEvent.Set();
				});
			});
			await WindowHelper.WaitForIdle();

			InputHelper.Tap(listViewItem);
			await WindowHelper.WaitForIdle();

			VERIFY_IS_TRUE(stateChangedEvent.HasFired());
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates that changing the theme updates the multiselect checkbox.")]
		public async Task VerifyChangeThemeInMultiSelect()
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			StackPanel rootPanel = null;

			await RunOnUIThread(() =>
			{
				rootPanel = XamlReader.Load("""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListView Width='300' SelectionMode='Multiple' SelectedIndex='1'>
				        <ListViewItem Content='ListViewItem'/>
				        <ListViewItem Content='Selected ListViewItem'/>
				    </ListView>
				    <GridView Width='300' SelectionMode='Multiple' SelectedIndex='1'>
				        <GridViewItem Content='GridViewItem'/>
				        <GridViewItem Content='Selected GridViewItem'/>
				    </GridView>
				</StackPanel>
				""") as StackPanel;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// change the theme
				rootPanel.RequestedTheme = ElementTheme.Light;
				rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
			});
			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires touch press-hold-and-pan injection; injected touch drags are unreliable")]
		[Description("Validates that DragOver state is being invoked when drag enters drop zone 20/60/20")]
		public async Task VerifyDragOverStateForLVI()
		{
			// Leak: DragOverState tests leaks ContentPresenter
			// UNO TODO: ErrorHandlingHelper.IgnoreLeaksForTest has no equivalent.

			TestCleanupWrapper cleanup;

			const int itemsCount = 20;
			ListView listView = null;
			var choosingItemContainerRegistration = new SafeEventRegistration<ListViewBase, TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs>>("ChoosingItemContainer");

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <ListView x:Name='listView' Width='400' Height='400' CanReorderItems='True' AllowDrop='True'/>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				listView = (ListView)rootPanel.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				choosingItemContainerRegistration.Attach(listView,
					(sender, args) =>
					{
						var lvi = new ListViewItem();

						lvi.Style = XamlReader.Load("""
						<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ListViewItem' BasedOn='{StaticResource ListViewItemExpanded}'>
						  <Setter Property='Template'>
						    <Setter.Value>
						      <ControlTemplate TargetType='ListViewItem'>
						        <Grid x:Name='ContentBorder'
						              Background='{TemplateBinding Background}'
						              BorderBrush='{TemplateBinding BorderBrush}'
						              BorderThickness='{TemplateBinding BorderThickness}'>
						          <VisualStateManager.VisualStateGroups>
						            <VisualStateGroup x:Name='DragStates'>
						              <VisualState x:Name='NotDragging'/>
						              <VisualState x:Name='Dragging'>
						                <Storyboard>
						                  <DoubleAnimation Storyboard.TargetName='ContentBorder'
						                                   Storyboard.TargetProperty='Opacity'
						                                   Duration='0'
						                                   To='{ThemeResource ListViewItemDragThemeOpacity}'/>
						                  <DragItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DraggingTarget'>
						                <Storyboard>
						                  <DropTargetItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DraggedPlaceholder'>
						                <Storyboard>
						                  <FadeOutThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DragOver'>
						                <Storyboard>
						                  <DropTargetItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						            </VisualStateGroup>
						          </VisualStateManager.VisualStateGroups>
						          <ContentPresenter x:Name='ContentPresenter'
						                            ContentTransitions='{TemplateBinding ContentTransitions}'
						                            ContentTemplate='{TemplateBinding ContentTemplate}'
						                            Content='{TemplateBinding Content}'
						                            HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
						                            VerticalAlignment='{TemplateBinding VerticalContentAlignment}'
						                            Margin='{TemplateBinding Padding}'/>
						        </Grid>
						      </ControlTemplate>
						    </Setter.Value>
						  </Setter>
						</Style>
						""") as Style;

						// we will treat the first item as a folder
						if (args.ItemIndex == 0)
						{
							lvi.AllowDrop = true;
						}

						args.ItemContainer = lvi;
						args.IsContainerPrepared = true;
					});

				// The C++ fill loop assigns to by-value copies, so the items stay null.
				List<object> items = new(new object[itemsCount]);
				VERIFY_IS_NOT_NULL(items);

				listView.ItemsSource = items;
				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await VerifyDragOverState<ListViewItem>(listView);
		}

		[TestMethod]
		[Ignore("Requires touch press-hold-and-pan injection; injected touch drags are unreliable")]
		[Description("Validates that DragOver state is being invoked when drag enters drop zone 30/40/30")]
		public async Task VerifyDragOverStateForGVI()
		{
			TestCleanupWrapper cleanup;

			const int itemsCount = 20;
			GridView gridView = null;
			var choosingItemContainerRegistration = new SafeEventRegistration<ListViewBase, TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs>>("ChoosingItemContainer");

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				    <GridView x:Name='gridView' Width='400' Height='400' CanReorderItems='True' AllowDrop='True'/>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				gridView = (GridView)rootPanel.FindName("gridView");
				VERIFY_IS_NOT_NULL(gridView);

				choosingItemContainerRegistration.Attach(gridView,
					(sender, args) =>
					{
						var gvi = new GridViewItem();

						gvi.Style = XamlReader.Load("""
						<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='GridViewItem'>
						  <Setter Property='Template'>
						    <Setter.Value>
						      <ControlTemplate TargetType='GridViewItem'>
						        <Grid x:Name='ContentBorder'
						              Background='{TemplateBinding Background}'
						              BorderBrush='{TemplateBinding BorderBrush}'
						              BorderThickness='{TemplateBinding BorderThickness}'>
						          <VisualStateManager.VisualStateGroups>
						            <VisualStateGroup x:Name='DragStates'>
						              <VisualState x:Name='NotDragging'/>
						              <VisualState x:Name='Dragging'>
						                <Storyboard>
						                  <DoubleAnimation Storyboard.TargetName='ContentBorder'
						                                   Storyboard.TargetProperty='Opacity'
						                                   Duration='0'
						                                   To='{ThemeResource ListViewItemDragThemeOpacity}'/>
						                  <DragItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DraggingTarget'>
						                <Storyboard>
						                  <DropTargetItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DraggedPlaceholder'>
						                <Storyboard>
						                  <FadeOutThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						              <VisualState x:Name='DragOver'>
						                <Storyboard>
						                  <DropTargetItemThemeAnimation TargetName='ContentBorder'/>
						                </Storyboard>
						              </VisualState>
						            </VisualStateGroup>
						          </VisualStateManager.VisualStateGroups>
						          <ContentPresenter x:Name='ContentPresenter'
						                            ContentTransitions='{TemplateBinding ContentTransitions}'
						                            ContentTemplate='{TemplateBinding ContentTemplate}'
						                            Content='{TemplateBinding Content}'
						                            HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
						                            VerticalAlignment='{TemplateBinding VerticalContentAlignment}'
						                            Margin='{TemplateBinding Padding}'/>
						        </Grid>
						      </ControlTemplate>
						    </Setter.Value>
						  </Setter>
						</Style>
						""") as Style;

						// we will treat the first item as a folder
						if (args.ItemIndex == 0)
						{
							gvi.AllowDrop = true;
						}

						args.ItemContainer = gvi;
						args.IsContainerPrepared = true;
					});

				// The C++ fill loop assigns to by-value copies, so the items stay null.
				List<object> items = new(new object[itemsCount]);
				VERIFY_IS_NOT_NULL(items);

				gridView.ItemsSource = items;
				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await VerifyDragOverState<GridViewItem>(gridView);
		}

		[TestMethod]
		[Description("Validates that the chrome animation commands' lifetime is handled correctly in case the ListViewItemPresenter instance doesn't have a child.")]
		public async Task ValidateChromeAnimationCommandsLifetime()
		{
			TestCleanupWrapper cleanup;

			await ValidateChromeAnimationCommandsLifetime(false /*forceSelectionIndicatorVisualEnabled*/);
		}

		[TestMethod]
		[Description("Validates that the chrome animation commands' lifetime is handled correctly in case the ListViewItemPresenter instance uses a selection indicator.")]
		public async Task ValidateChromeAnimationCommandsLifetimeWithSelectionIndicator()
		{
			TestCleanupWrapper cleanup;

			await ValidateChromeAnimationCommandsLifetime(true /*forceSelectionIndicatorVisualEnabled*/);
		}

		private async Task ValidateChromeAnimationCommandsLifetime(bool forceSelectionIndicatorVisualEnabled)
		{
#if HAS_UNO
			using var featureSelectionIndicatorVisualEnabled = forceSelectionIndicatorVisualEnabled
				? ListViewBaseItemChromeRuntimeFeatures.Override(forceSelectionIndicatorVisual: true)
				: null;
#endif

			ListView listView = null;
			var loadedEvent = new Event();
			var loadedRegistration = new SafeEventRegistration<ListView, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				var data = new List<string>();
				data.Add("Item One");

				var emptyItemTemplate = (DataTemplate)XamlReader.Load("""
				<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				</DataTemplate>
				""");

				listView = new ListView();
				listView.ItemsSource = data;
				listView.VerticalAlignment = VerticalAlignment.Top;
				listView.ItemTemplate = emptyItemTemplate;

				loadedRegistration.Attach(listView, (sender, args) =>
				{
					LOG_OUTPUT("Host loaded.");
					loadedEvent.Set();
				});
				WindowHelper.WindowContent = listView;
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			InputHelper.Tap(listView);
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listView = null;
				WindowHelper.WindowContent = null;
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates that ListViewItemPresenter goes to visual states declared in the template.")]
		public async Task VerifyLVIPWorksWithVSM()
		{
			TestCleanupWrapper cleanup;

			ListViewItem listViewItem = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
				   <ListViewItem x:Name='listViewItem'>
				      <ListViewItem.Style>
				        <Style TargetType='ListViewItem'>
				            <Setter Property='Template'>
				                <Setter.Value>
				                    <ControlTemplate TargetType='ListViewItem'>
				                        <ListViewItemPresenter x:Name='Root' Background='Blue'>
				                            <VisualStateManager.VisualStateGroups>
				                                <VisualStateGroup x:Name='CommonStates'>
				                                    <VisualState x:Name='PointerOver'>
				                                        <VisualState.Setters>
				                                            <Setter Target='Root.Background' Value='Green' />
				                                        </VisualState.Setters>
				                                    </VisualState>
				                                </VisualStateGroup>
				                            </VisualStateManager.VisualStateGroups>
				                        </ListViewItemPresenter>
				                    </ControlTemplate>
				                </Setter.Value>
				            </Setter>
				         </Style>
				      </ListViewItem.Style>
				  </ListViewItem>
				</Grid>
				""") as Grid;

				VERIFY_IS_NOT_NULL(rootPanel);

				listViewItem = (ListViewItem)rootPanel.FindName("listViewItem");
				VERIFY_IS_NOT_NULL(listViewItem);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var lvip = (ListViewItemPresenter)VisualTreeHelper.GetChild(listViewItem, 0);

				LOG_OUTPUT("Check that default background is Blue");
				SolidColorBrush scb = (SolidColorBrush)lvip.Background;
				VERIFY_ARE_EQUAL(scb.Color, Microsoft.UI.Colors.Blue);

				VisualStateManager.GoToState(listViewItem, "PointerOver", false);

				LOG_OUTPUT("Check that after going to PointerOver VSM state we switched to Green.");
				scb = (SolidColorBrush)lvip.Background;
				VERIFY_ARE_EQUAL(scb.Color, Microsoft.UI.Colors.Green);
			});
			await WindowHelper.WaitForIdle();
		}

		private async Task LoadXamlAndVerifyMockDCompOutput(string xaml)
		{
			// MockDComp should be injected and detached per test, since it keeps information like the surfaces
			// that get created.
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
			WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load(xaml) as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();

			Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the ListViewItemPresenter's RevealBackground OnTop of Content.")]
		public async Task ValidateLVIPRevealBackgroundOnTopOfContent()
		{
			await LoadXamlAndVerifyMockDCompOutput("""
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <Style TargetType='ListViewItem'>
			            <Setter Property = 'FontFamily' Value = '{ThemeResource ContentControlThemeFontFamily}' />
			            <Setter Property = 'FontSize' Value = '{ThemeResource ControlContentThemeFontSize}' />
			            <Setter Property='Background' Value='Transparent' />
			            <Setter Property='TabNavigation' Value='Local' />
			            <Setter Property='IsHoldingEnabled' Value='True' />
			            <Setter Property='Padding' Value='12,0,12,0' />
			            <Setter Property='HorizontalContentAlignment' Value='Left' />
			            <Setter Property='VerticalContentAlignment' Value='Center' />
			            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
			            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
			            <Setter Property='Template'>
			                <Setter.Value>
			                    <ControlTemplate TargetType='ListViewItem'>
			                        <ListViewItemPresenter
			                            ContentTransitions='{TemplateBinding ContentTransitions}'
			                            SelectionCheckMarkVisualEnabled='True'
			                            CheckBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
			                            CheckBoxBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
			                            DragBackground='{ThemeResource ListViewItemDragBackgroundThemeBrush}'
			                            DragForeground='{ThemeResource ListViewItemDragForegroundThemeBrush}'
			                            FocusBorderBrush='{ThemeResource SystemControlForegroundAltHighBrush}'
			                            FocusSecondaryBorderBrush='{ThemeResource SystemControlForegroundBaseHighBrush}'
			                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackgroundThemeBrush}'
			                            PointerOverBackground='{ThemeResource SystemControlForegroundListLowBrush}'
			                            SelectedBackground='{ThemeResource SystemControlHighlightAltListAccentLowBrush}'
			                            SelectedForeground='{ThemeResource ListViewItemSelectedForegroundThemeBrush}'
			                            SelectedPointerOverBackground='{ThemeResource SystemControlHighlightAltListAccentMediumBrush}'
			                            PressedBackground='{ThemeResource SystemControlForegroundListMediumBrush}'
			                            SelectedPressedBackground='{ThemeResource SystemControlHighlightAltListAccentHighBrush}'
			                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
			                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
			                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
			                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
			                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
			                            ContentMargin='{TemplateBinding Padding}'
			                            CheckMode='Inline'
			                            RevealBackground = '{ThemeResource ListViewItemRevealBackground}'
			                            RevealBorderThickness = '{ThemeResource ListViewItemRevealBorderThemeThickness}'
			                            RevealBorderBrush = '{ThemeResource ListViewItemRevealBorderBrush}'
			                            RevealBackgroundShowsAboveContent = 'True' />
			                    </ControlTemplate>
			                </Setter.Value>
			            </Setter>
			        </Style>
			    </Grid.Resources>
			    <ListViewItem x:Name='pointerOver' Content='Item 1' />
			</Grid>
			""");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the ListViewItemPresenter's RevealBackground below Content.")]
		public async Task ValidateLVIPRevealBackgroundBelowContent()
		{
			await LoadXamlAndVerifyMockDCompOutput("""
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <Style TargetType='ListViewItem'>
			            <Setter Property = 'FontFamily' Value = '{ThemeResource ContentControlThemeFontFamily}' />
			            <Setter Property = 'FontSize' Value = '{ThemeResource ControlContentThemeFontSize}' />
			            <Setter Property='Background' Value='Transparent' />
			            <Setter Property='TabNavigation' Value='Local' />
			            <Setter Property='IsHoldingEnabled' Value='True' />
			            <Setter Property='Padding' Value='12,0,12,0' />
			            <Setter Property='HorizontalContentAlignment' Value='Left' />
			            <Setter Property='VerticalContentAlignment' Value='Center' />
			            <Setter Property='MinWidth' Value='{ThemeResource ListViewItemMinWidth}' />
			            <Setter Property='MinHeight' Value='{ThemeResource ListViewItemMinHeight}' />
			            <Setter Property='Template'>
			                <Setter.Value>
			                    <ControlTemplate TargetType='ListViewItem'>
			                        <ListViewItemPresenter
			                            ContentTransitions='{TemplateBinding ContentTransitions}'
			                            SelectionCheckMarkVisualEnabled='True'
			                            CheckBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
			                            CheckBoxBrush='{ThemeResource SystemControlBackgroundBaseMediumHighBrush}'
			                            DragBackground='{ThemeResource ListViewItemDragBackgroundThemeBrush}'
			                            DragForeground='{ThemeResource ListViewItemDragForegroundThemeBrush}'
			                            FocusBorderBrush='{ThemeResource SystemControlForegroundAltHighBrush}'
			                            FocusSecondaryBorderBrush='{ThemeResource SystemControlForegroundBaseHighBrush}'
			                            PlaceholderBackground='{ThemeResource ListViewItemPlaceholderBackgroundThemeBrush}'
			                            PointerOverBackground='{ThemeResource SystemControlForegroundListLowBrush}'
			                            SelectedBackground='{ThemeResource SystemControlHighlightAltListAccentLowBrush}'
			                            SelectedForeground='{ThemeResource ListViewItemSelectedForegroundThemeBrush}'
			                            SelectedPointerOverBackground='{ThemeResource SystemControlHighlightAltListAccentMediumBrush}'
			                            PressedBackground='{ThemeResource SystemControlForegroundListMediumBrush}'
			                            SelectedPressedBackground='{ThemeResource SystemControlHighlightAltListAccentHighBrush}'
			                            DisabledOpacity='{ThemeResource ListViewItemDisabledThemeOpacity}'
			                            DragOpacity='{ThemeResource ListViewItemDragThemeOpacity}'
			                            ReorderHintOffset='{ThemeResource ListViewItemReorderHintThemeOffset}'
			                            HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}'
			                            VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'
			                            ContentMargin='{TemplateBinding Padding}'
			                            CheckMode='Inline'
			                            RevealBackground = 'Yellow'
			                            RevealBorderThickness = '2'
			                            RevealBorderBrush = 'Blue'
			                            RevealBackgroundShowsAboveContent = 'False' />
			                    </ControlTemplate>
			                </Setter.Value>
			            </Setter>
			        </Style>
			    </Grid.Resources>
			    <ListViewItem x:Name='pointerOver' Content='Item 1' />
			</Grid>
			""");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the ListViewItemRevealBackgroundShowsAboveContentStyle.")]
		public async Task ValidateListViewItemRevealBackgroundShowsAboveContentStyle()
		{
			await LoadXamlAndVerifyMockDCompOutput("""
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <Style TargetType='ListViewItem' BasedOn='{StaticResource ListViewItemRevealBackgroundShowsAboveContentStyle}'>
			        </Style>
			    </Grid.Resources>
			    <ListViewItem Content='Item 1' />
			</Grid>
			""");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the GridListViewItemRevealBackgroundShowsAboveContentStyle.")]
		public async Task ValidateGridViewItemRevealBackgroundShowsAboveContentStyle()
		{
			await LoadXamlAndVerifyMockDCompOutput("""
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <Style TargetType='GridViewItem' BasedOn='{StaticResource GridViewItemRevealBackgroundShowsAboveContentStyle}'>
			        </Style>
			    </Grid.Resources>
			    <GridViewItem Content='Item 1' />
			</Grid>
			""");
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the GridViewItem's reveal background shows in overlay mode and that borders are hidden when disabled.")]
		public async Task ValidateGridViewItemRevealBackgroundAndBorder()
		{
			string xaml = """
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <SolidColorBrush x:Key='GridViewItemRevealBackground' Color='#FF00FF00'/>
			        <SolidColorBrush x:Key='GridViewItemRevealBorderBrush' Color='#FF00FFFF'/>
			        <Style TargetType='GridViewItem' BasedOn='{StaticResource GridViewItemRevealStyle}'>
			        </Style>
			    </Grid.Resources>
			    <StackPanel>
			        <GridViewItem x:Name='GVI1' Content='Item 1' />
			        <GridViewItem x:Name='GVI2' Content='Item 2' IsEnabled='false'/>
			    </StackPanel>
			</Grid>
			""";

			await LoadXamlAndVerifyMockDCompOutput(xaml);
		}


		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the GridViewItem's transitioning between disabled states.")]
		public async Task ValidateGridViewItemRevealDisabledStates()
		{
			string xaml = """
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
			    <Grid.Resources>
			        <SolidColorBrush x:Key='GridViewItemRevealBackground' Color='#FF00FF00'/>
			        <SolidColorBrush x:Key='GridViewItemRevealBorderBrush' Color='#FF00FFFF'/>
			        <Style TargetType='GridViewItem' BasedOn='{StaticResource GridViewItemRevealStyle}'>
			        </Style>
			    </Grid.Resources>
			    <StackPanel>
			        <GridViewItem x:Name='GVI1' Content='Item 1' />
			        <GridViewItem x:Name='GVI2' Content='Item 2' IsEnabled='false'/>
			    </StackPanel>
			</Grid>
			""";

			// UNO TODO: DefaultUIElementTreeValidationRules and VerifyUIElementTreeWithRulesInline have no equivalent;
			// the "Default" and "EnabledChanged" steps use the plain tree verification.

			Grid rootPanel = null;
			await RunOnUIThread(() =>
			{
				// Set a consistent window size and zoom scale, so that the DComp tree is predictable.
				WindowHelper.SetWindowSizeOverride(new Size(400.0f, 600.0f));

				rootPanel = XamlReader.Load(xaml) as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();
			Utilities.VerifyUIElementTree(); // "Default"

			await RunOnUIThread(() =>
			{
				var gvi1 = rootPanel.FindName("GVI1") as GridViewItem;
				var gvi2 = rootPanel.FindName("GVI2") as GridViewItem;
				VERIFY_IS_NOT_NULL(gvi1);
				VERIFY_IS_NOT_NULL(gvi2);

				gvi1.IsEnabled = false;
				gvi2.IsEnabled = true;
			});

			await WindowHelper.WaitForIdle();
			Utilities.VerifyUIElementTree(); // "EnabledChanged"

			WindowHelper.ResetWindowContentAndWaitForIdle();
		}

		private static string ListViewItemFocusPropertyThemeChangeXaml() => """
		<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root' RequestedTheme='Dark'>
		    <Grid.Resources>
		      <ResourceDictionary>
		        <Style TargetType='ListViewItem'>
		          <Setter Property='FocusVisualPrimaryThickness' Value='10' />
		          <Setter Property='FocusVisualSecondaryThickness' Value='5' />
		        </Style>
		        <ResourceDictionary.ThemeDictionaries>
		          <ResourceDictionary x:Key='Light'>
		             <SolidColorBrush x:Key='ListViewItemFocusVisualPrimaryBrush' Color='Red'/>
		             <SolidColorBrush x:Key='ListViewItemFocusVisualSecondaryBrush' Color='Yellow'/>
		          </ResourceDictionary>
		          <ResourceDictionary x:Key='Dark'>
		            <SolidColorBrush x:Key='ListViewItemFocusVisualPrimaryBrush' Color='Blue'/>
		            <SolidColorBrush x:Key='ListViewItemFocusVisualSecondaryBrush' Color='Green'/>
		          </ResourceDictionary>
		        </ResourceDictionary.ThemeDictionaries>
		      </ResourceDictionary>
		    </Grid.Resources>
		    <StackPanel>
		        <ListViewItem x:Name='lvi' Content='Primary' FocusVisualPrimaryBrush='{ThemeResource ListViewItemFocusVisualPrimaryBrush}' FocusVisualSecondaryBrush='{ThemeResource ListViewItemFocusVisualSecondaryBrush}'/>
		        <ListViewItem x:Name='lvi_default' Content='Primary2' />
		    </StackPanel>
		</Grid>
		""";

		private static string GridViewItemFocusPropertyThemeChangeXaml() => """
		<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root' RequestedTheme='Dark'>
		    <Grid.Resources>
		      <ResourceDictionary>
		        <Style TargetType='GridViewItem'>
		          <Setter Property='FocusVisualPrimaryThickness' Value='10' />
		          <Setter Property='FocusVisualSecondaryThickness' Value='5' />
		        </Style>
		        <ResourceDictionary.ThemeDictionaries>
		          <ResourceDictionary x:Key='Light'>
		             <SolidColorBrush x:Key='GridViewItemFocusVisualPrimaryBrush' Color='Red'/>
		             <SolidColorBrush x:Key='GridViewItemFocusVisualSecondaryBrush' Color='Yellow'/>
		          </ResourceDictionary>
		          <ResourceDictionary x:Key='Dark'>
		            <SolidColorBrush x:Key='GridViewItemFocusVisualPrimaryBrush' Color='Blue'/>
		            <SolidColorBrush x:Key='GridViewItemFocusVisualSecondaryBrush' Color='Green'/>
		          </ResourceDictionary>
		        </ResourceDictionary.ThemeDictionaries>
		      </ResourceDictionary>
		    </Grid.Resources>
		    <StackPanel>
		        <GridViewItem x:Name='gvi' Content='Focused' FocusVisualPrimaryBrush='{ThemeResource GridViewItemFocusVisualPrimaryBrush}' FocusVisualSecondaryBrush='{ThemeResource GridViewItemFocusVisualSecondaryBrush}'/>
		        <GridViewItem x:Name='gvi_default' Content='Focused' />
		    </StackPanel>
		</Grid>
		""";

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the ListViewItem's FocusVisual* properties respond to theme changes.")]
		public async Task ValidateListViewItemFocusPropertyThemeChange()
		{
			string xaml = ListViewItemFocusPropertyThemeChangeXaml();

			LOG_OUTPUT("Verify user specified ThemeResource");
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"lvi",
				Microsoft.UI.Colors.Red,
				Microsoft.UI.Colors.Yellow,
				Microsoft.UI.Colors.Blue,
				Microsoft.UI.Colors.Green);

			LOG_OUTPUT("Verify default ThemeResource");

			// Default FocusVisualPrimaryBrush refers to FocusStrokeColorOuterBrush, which is:
			// Light: #E4000000
			// Dark (default): #FFFFFFFF
			// Default FocusVisualSecondaryBrush refers to FocusStrokeColorInnerBrush, which is:
			// Light: #B3FFFFFF
			// Dark (default): #B3000000
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"lvi_default",
				Microsoft.UI.ColorHelper.FromArgb(0xE4, 0x00, 0x00, 0x00),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0x00, 0x00, 0x00));
		}

		[TestMethod]
		[Description("Validates the ListViewItem's FocusVisual* properties respond to theme changes, without the MockDComp masters.")]
		public async Task ValidateListViewItemFocusPropertyThemeChange_NoMaster()
		{
			string xaml = ListViewItemFocusPropertyThemeChangeXaml();

			LOG_OUTPUT("Verify user specified ThemeResource");
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"lvi",
				Microsoft.UI.Colors.Red,
				Microsoft.UI.Colors.Yellow,
				Microsoft.UI.Colors.Blue,
				Microsoft.UI.Colors.Green,
				verifyMasters: false);

			LOG_OUTPUT("Verify default ThemeResource");

			// Default FocusVisualPrimaryBrush refers to FocusStrokeColorOuterBrush, which is:
			// Light: #E4000000
			// Dark (default): #FFFFFFFF
			// Default FocusVisualSecondaryBrush refers to FocusStrokeColorInnerBrush, which is:
			// Light: #B3FFFFFF
			// Dark (default): #B3000000
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"lvi_default",
				Microsoft.UI.ColorHelper.FromArgb(0xE4, 0x00, 0x00, 0x00),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0x00, 0x00, 0x00),
				verifyMasters: false);
		}

		[TestMethod]
		[Ignore("Requires WinUI visual tree masters")]
		[Description("Validates the GridViewItem's FocusVisual* properties respond to theme changes.")]
		public async Task ValidateGridViewItemFocusPropertyThemeChange()
		{
			string xaml = GridViewItemFocusPropertyThemeChangeXaml();

			LOG_OUTPUT("Verify user specified ThemeResource");
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"gvi",
				Microsoft.UI.Colors.Red,
				Microsoft.UI.Colors.Yellow,
				Microsoft.UI.Colors.Blue,
				Microsoft.UI.Colors.Green);

			LOG_OUTPUT("Verify default ThemeResource");

			// Default FocusVisualPrimaryBrush refers to FocusStrokeColorOuterBrush, which is:
			// Light: #E4000000
			// Dark (default): #FFFFFFFF
			// Default FocusVisualSecondaryBrush refers to FocusStrokeColorInnerBrush, which is:
			// Light: #B3FFFFFF
			// Dark (default): #B3000000
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"gvi_default",
				Microsoft.UI.ColorHelper.FromArgb(0xE4, 0x00, 0x00, 0x00),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0x00, 0x00, 0x00));
		}

		[TestMethod]
		[Description("Validates the GridViewItem's FocusVisual* properties respond to theme changes, without the MockDComp masters.")]
		public async Task ValidateGridViewItemFocusPropertyThemeChange_NoMaster()
		{
			string xaml = GridViewItemFocusPropertyThemeChangeXaml();

			LOG_OUTPUT("Verify user specified ThemeResource");
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"gvi",
				Microsoft.UI.Colors.Red,
				Microsoft.UI.Colors.Yellow,
				Microsoft.UI.Colors.Blue,
				Microsoft.UI.Colors.Green,
				verifyMasters: false);

			LOG_OUTPUT("Verify default ThemeResource");

			// Default FocusVisualPrimaryBrush refers to FocusStrokeColorOuterBrush, which is:
			// Light: #E4000000
			// Dark (default): #FFFFFFFF
			// Default FocusVisualSecondaryBrush refers to FocusStrokeColorInnerBrush, which is:
			// Light: #B3FFFFFF
			// Dark (default): #B3000000
			await TestSelectorItemFocusThemeChanges(
				xaml,
				"gvi_default",
				Microsoft.UI.ColorHelper.FromArgb(0xE4, 0x00, 0x00, 0x00),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
				Microsoft.UI.ColorHelper.FromArgb(0xB3, 0x00, 0x00, 0x00),
				verifyMasters: false);
		}

		// Validates ListView item reordering with keyboard in LTR/RTL flows, and horizontal/vertical layouts.
		[TestMethod]
		[Description("Validates ListView item reordering with keyboard in LTR/RTL flows, and horizontal/vertical layouts.")]
		public async Task VerifyKeyboardReordering()
		{
			await VerifyKeyboardReordering(true  /*horizontalPanel*/, false /*rightToLeft*/);
			await VerifyKeyboardReordering(true  /*horizontalPanel*/, true  /*rightToLeft*/);
			await VerifyKeyboardReordering(false /*horizontalPanel*/, false /*rightToLeft*/);
			await VerifyKeyboardReordering(false /*horizontalPanel*/, true  /*rightToLeft*/);
		}

		private async Task VerifyKeyboardReordering(bool horizontalPanel, bool rightToLeft)
		{
			TestCleanupWrapper cleanup;

			ListView listView = null;
			ListViewItem listViewItem = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = XamlReader.Load("""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
				  x:Name='root' FlowDirection='LeftToRight'>
				  <ListView x:Name='listView' Width='400' Height='400' HorizontalAlignment='Left' VerticalAlignment='Top' CanDragItems='True' CanReorderItems='True' AllowDrop='True'/>
				</Grid>
				""") as Grid;
				VERIFY_IS_NOT_NULL(rootPanel);

				if (rightToLeft)
				{
					rootPanel.FlowDirection = FlowDirection.RightToLeft;
				}

				listView = (ListView)rootPanel.FindName("listView");
				VERIFY_IS_NOT_NULL(listView);

				WindowHelper.WindowContent = rootPanel;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				if (horizontalPanel)
				{
					ItemsStackPanel itemsPanel = (ItemsStackPanel)listView.ItemsPanelRoot;
					VERIFY_IS_NOT_NULL(itemsPanel);

					itemsPanel.Orientation = Orientation.Horizontal;
				}

				for (int i = 0; i < 5; ++i)
				{
					listView.Items.Add(i);
				}
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				listViewItem = (ListViewItem)listView.ContainerFromIndex(2);
				VERIFY_IS_NOT_NULL(listViewItem);

				LOG_OUTPUT("Focusing middle item.");
				listViewItem.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Moving it with Shift/Alt/Right.");
			await KeyboardHelper.PressKeySequence("$d$_shift#$d$_alt#$d$_right#$u$_right#$u$_alt#$u$_shift");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// In Horizontal+RightToLeft mode, the moved item's index becomes 1, otherwise it becomes 3.
				VERIFY_ARE_EQUAL(listViewItem, (ListViewItem)listView.ContainerFromIndex((horizontalPanel && rightToLeft) ? 1 : 3));
			});

			LOG_OUTPUT("Moving it with Shift/Alt/Down.");
			await KeyboardHelper.PressKeySequence("$d$_shift#$d$_alt#$d$_down#$u$_down#$u$_alt#$u$_shift");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// In Horizontal+RightToLeft mode, the moved item's index becomes 0, otherwise it becomes 4.
				VERIFY_ARE_EQUAL(listViewItem, (ListViewItem)listView.ContainerFromIndex((horizontalPanel && rightToLeft) ? 0 : 4));
			});

			LOG_OUTPUT("Moving it with Shift/Alt/Up.");
			await KeyboardHelper.PressKeySequence("$d$_shift#$d$_alt#$d$_up#$u$_up#$u$_alt#$u$_shift");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// In Horizontal+RightToLeft mode, the moved item's index becomes 1, otherwise it becomes 3.
				VERIFY_ARE_EQUAL(listViewItem, (ListViewItem)listView.ContainerFromIndex((horizontalPanel && rightToLeft) ? 1 : 3));
			});

			LOG_OUTPUT("Moving it with Shift/Alt/Left.");
			await KeyboardHelper.PressKeySequence("$d$_shift#$d$_alt#$d$_left#$u$_left#$u$_alt#$u$_shift");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// In all modes, the moved item's index becomes 2.
				VERIFY_ARE_EQUAL(listViewItem, (ListViewItem)listView.ContainerFromIndex(2));
			});
		}

		//
		// Private Methods
		//
		private async Task TestSelectorItemFocusThemeChanges(
			string xaml,
			string elementName,
			global::Windows.UI.Color lightPrimary,
			global::Windows.UI.Color lightSecondary,
			global::Windows.UI.Color darkPrimary,
			global::Windows.UI.Color darkSecondary,
			bool verifyMasters = true)
		{
			// UNO TODO: WUCRenderingScopeGuard has no equivalent.

			SelectorItem item = null;
			Panel rootPanel = null;
			await RunOnUIThread(() =>
			{
				rootPanel = XamlReader.Load(xaml) as Panel;
				VERIFY_IS_NOT_NULL(rootPanel);

				WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();
			if (verifyMasters)
			{
				Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, elementName + "_None");
			}
			await RunOnUIThread(() =>
			{
				item = rootPanel.FindName(elementName) as SelectorItem;
				VERIFY_IS_NOT_NULL(item);

				VERIFY_ARE_EQUAL(darkPrimary, ((SolidColorBrush)item.FocusVisualPrimaryBrush).Color);
				VERIFY_ARE_EQUAL(darkSecondary, ((SolidColorBrush)item.FocusVisualSecondaryBrush).Color);
				item.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();
			if (verifyMasters)
			{
				Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, elementName + "_Dark");
			}

			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Light;
				// FocusVisual properties don't update while element is focused. We need to remove
				// keyboard focus first
				item.Focus(FocusState.Pointer);
			});
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				item.Focus(FocusState.Keyboard); // Refocus to get updates
			});
			await WindowHelper.WaitForIdle();
			if (verifyMasters)
			{
				Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, elementName + "_Light");
			}

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(lightPrimary, ((SolidColorBrush)item.FocusVisualPrimaryBrush).Color);
				VERIFY_ARE_EQUAL(lightSecondary, ((SolidColorBrush)item.FocusVisualSecondaryBrush).Color);
			});
		}
	}
}
