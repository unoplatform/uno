using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using FluentAssertions;
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
	public Task When_NoSelection_Then_PointersEvents()
		=> RunTest("_noSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public Task When_SingleSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_singleSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public Task When_MultipleSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_multipleSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public Task When_ExtendedSelectionWithoutItemClick_Then_PointersEvents()
		=> RunTest("_extendedSelection_noClick", clicked: false);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public Task When_NoSelectionWithItemClick_Then_PointersEvents()
		=> RunTest("_noSelection_withClick", clicked: true);

	[Test]
	[AutoRetry]
	[InjectedPointer(PointerDeviceType.Touch)]
	public Task When_SingleSelectionWithItemClick_Then_PointersEvents()
		=> RunTest("_singleSelection_withClick", clicked: true);

	private async Task RunTest(string test, bool clicked)
	{
		await RunAsync("UITests.Windows_UI_Xaml_Controls.ListView.ListView_Selection_Pointers");

		var d = App.Query(q => q.Marked(test).Descendant().Marked(test + "_item")).Skip(3).First();

		App.TapCoordinates(d.Rect.CenterX, d.Rect.CenterY);

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

		pageEntered.Should().Be("D");
		itemEntered.Should().Be("D");
		//pageExited.Should().Be("D");
		itemExited.Should().Be("D");
		pagePressed.Should().Be("-");
		itemPressed.Should().Be("D");
		pageReleased.Should().Be("-");
		itemReleased.Should().Be("D");
		pageTapped.Should().Be("D");
		itemTapped.Should().Be("D");
		itemClicked.Should().Be(clicked ? "D" : "-");
	}
}
