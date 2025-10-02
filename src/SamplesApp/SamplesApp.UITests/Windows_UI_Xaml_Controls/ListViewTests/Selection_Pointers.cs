using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.Testing;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ListViewTests;

[TestFixture]
public partial class ListViewTests_Tests : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !IS_RUNTIME_UI_TESTS
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native
#endif
	public Task When_NoSelection_Then_PointersEvents()
		=> RunTest("_noSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	[InjectedPointer(PointerDeviceType.Mouse)]
	public Task When_SingleSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_singleSelection_noClick", clicked: false);

	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
#if !__SKIA__ // Mouse test on skia is flaky 
	[InjectedPointer(PointerDeviceType.Mouse)]
#endif
#if IS_RUNTIME_UI_TESTS
	// https://github.com/unoplatform/uno/issues/9080
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Skia)]
#else
	[Test]
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native https://github.com/unoplatform/uno/issues/9080
#endif
	public Task When_MultipleSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_multipleSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !IS_RUNTIME_UI_TESTS
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native
#endif
	public Task When_ExtendedSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_extendedSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	[InjectedPointer(PointerDeviceType.Mouse)]

#if !IS_RUNTIME_UI_TESTS
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native
#endif
	public Task When_NoSelectionWithItemClick_Then_PointersEvents()
		=> RunTest("_noSelection_withClick", clicked: true);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !IS_RUNTIME_UI_TESTS
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native
#endif
	public Task When_SingleSelectionWithItemClick_Then_PointersEvents()
		=> RunTest("_singleSelection_withClick", clicked: true);

	private async Task RunTest(string test, bool clicked)
	{
		await RunAsync("UITests.Windows_UI_Xaml_Controls.ListView.ListView_Selection_Pointers");

		var d = App.Query(q => q.Marked(test).Descendant().Marked(test + "_item")).Skip(3).First();

		App.TapCoordinates(d.Rect.CenterX, d.Rect.CenterY);

#if !IS_RUNTIME_UI_TESTS
		//	Delay may be required for Xamarin.UITest for iOS
		await Task.Delay(250);
#endif

		var pageEntered = App.Marked("_pageEntered").GetDependencyPropertyValue<string>("Text");
		var itemEntered = App.Marked("_itemEntered").GetDependencyPropertyValue<string>("Text");
		var pageExited = App.Marked("_pageExited").GetDependencyPropertyValue<string>("Text");
		var itemExited = App.Marked("_itemExited").GetDependencyPropertyValue<string>("Text");
		var pagePressed = App.Marked("_pagePressed").GetDependencyPropertyValue<string>("Text");
		var itemPressed = App.Marked("_itemPressed").GetDependencyPropertyValue<string>("Text");
		var pageReleased = App.Marked("_pageReleased").GetDependencyPropertyValue<string>("Text");
		var itemReleased = App.Marked("_itemReleased").GetDependencyPropertyValue<string>("Text");
		var pageTapped = App.Marked("_pageTapped").GetDependencyPropertyValue<string>("Text");
		var itemTapped = App.Marked("_itemTapped").GetDependencyPropertyValue<string>("Text");
		var itemClicked = App.Marked("_itemClicked").GetDependencyPropertyValue<string>("Text");

		if (CurrentPointerType is PointerDeviceType.Mouse)
		{
			// With mouse, we will have pointer enter on the page as soon as pointer is over (so datacontext won't be D)
			// and we won't get any pointer leave after test.
			itemEntered.Should().Be("D", "entered item");
		}
		else
		{
			pageEntered.Should().Be("D", "entered page");
			itemEntered.Should().Be("D", "entered item");
#if false // Frequently fails on CI for Skia and Android
			pageExited.Should().Be("D", "exited page");
#endif
			itemExited.Should().Be("D", "exited item");
		}

		pagePressed.Should().Be("-", "item should have handled pressed event");
		itemPressed.Should().Be("D", "item pressed");
		pageReleased.Should().Be("-", "item should have handled released event");
		itemReleased.Should().Be("D", "item released");
		pageTapped.Should().Be("D", "page has been tapped");
		itemTapped.Should().Be("D", "item has been tapped");
		itemClicked.Should().Be(clicked ? "D" : "-", clicked ? "item has been clicked" : "item click is disabled.");
	}
}
