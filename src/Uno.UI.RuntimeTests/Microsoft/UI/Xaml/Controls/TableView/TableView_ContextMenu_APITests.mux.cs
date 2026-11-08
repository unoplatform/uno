// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_ContextMenu_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.ContextMenuTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewAutomationTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Context menu resolution (cell, header, row, content and TableView flyouts). Requests go through
// an automation peer's ShowContextMenu; real input is covered by the interaction tests.
[TestClass]
public class TableViewContextMenuTests : TableViewApiTestBase
{
	// ---- A. Body precedence -------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "A column's CellContextFlyout wins over the row style menu and anchors to the cell.")]
	public async Task CellMenuWinsOverRowMenu()
	{
		var f = await LoadFixture();

		await RequestOnCell(f, 3, NameCol);

		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, NameCol), f.P(3), "UIA on row 3 / Name");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A cell without a CellContextFlyout falls back to the row style menu, anchored to the row.")]
	public async Task RowMenuUsedWhenColumnHasNoCellMenu()
	{
		var f = await LoadFixture();

		await RequestOnCell(f, 3, OfficeCol);

		VerifyOpened(f, "RowMenu", () => RowOf(f, 3), f.P(3), "UIA on row 3 / Office");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "With no cell or row menu the TableView.ContextFlyout opens at the requested cell.")]
	public async Task TableMenuUsedWhenNoCellOrRowMenu()
	{
		var f = await LoadFixture(configure: x => x.Table.Resources.Remove(typeof(TableViewRow)));

		await RequestOnCell(f, 3, OfficeCol);

		VerifyOpened(f, "TableMenu", () => f.Table, expectedItem: null, "UIA on row 3 / Office, no row style menu");
		VerifyMenuPlacedAtTarget(f, () => CellOf(f, 3, OfficeCol));
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "With no TableView candidate the request is left unhandled and an ancestor's ContextFlyout opens.")]
	public async Task UnhandledWhenNoCandidateBubblesToAncestor()
	{
		var f = await LoadFixture(configure: x =>
		{
			x.NameColumn.CellContextFlyout = null;
			x.NameColumn.HeaderContextFlyout = null;
			x.Table.Resources.Remove(typeof(TableViewRow));
			x.Table.ContextFlyout = null;
			x.Root.ContextFlyout = x.Recorder.Create("OuterMenu");
		});

		await RequestOnCell(f, 3, NameCol);

		VerifyOpened(f, "OuterMenu", () => f.Root, expectedItem: null, "UIA on row 3 / Name, no TableView menus");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "CellContextFlyout is read per request; a new value applies and null falls back to the row menu.")]
	public async Task CellMenuChangesApplyOnNextRequest()
	{
		var f = await LoadFixture();
		MenuFlyout cellMenu2 = null;
		RunOnUIThread.Execute(() => cellMenu2 = f.Recorder.Create("CellMenu2"));

		await RequestOnCell(f, 3, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, NameCol), f.P(3), "initial CellMenu");
		await CloseMenus(f);

		RunOnUIThread.Execute(() => f.NameColumn.CellContextFlyout = cellMenu2);
		await RequestOnCell(f, 3, NameCol);
		VerifyOpened(f, "CellMenu2", () => CellOf(f, 3, NameCol), f.P(3), "after replacing CellContextFlyout");
		await CloseMenus(f);

		RunOnUIThread.Execute(() => f.NameColumn.CellContextFlyout = null);
		await RequestOnCell(f, 3, NameCol);
		VerifyOpened(f, "RowMenu", () => RowOf(f, 3), f.P(3), "after clearing CellContextFlyout");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout on the cell template root wins over the column's CellContextFlyout, including one assigned after load.")]
	public async Task CellTemplateFlyoutWinsOverCellMenu()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(RootFlyoutContent),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		var content = TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");

		await RequestOnCell(f, 3, DepartmentCol);
		VerifyOpened(f, "ContentMenu", () => content, f.P(3), "UIA on row 3 / Department");
		await CloseMenus(f);

		RunOnUIThread.Execute(() => f.DepartmentColumn.CellContextFlyout = f.Recorder.Create("CellMenu2"));
		await RequestOnCell(f, 3, DepartmentCol);
		VerifyOpened(f, "ContentMenu", () => content, f.P(3), "UIA on row 3 / Department, CellContextFlyout replaced after load");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout on a nested (non-root) element in the cell template is found.")]
	public async Task CellTemplateInnerFlyoutFound()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(
				"<Grid>" +
				"  <Border x:Name='ContentRoot' Background='Transparent'>" +
				"    <Border.ContextFlyout><MenuFlyout/></Border.ContextFlyout>" +
				"    <TextBlock Text='{Binding Role}'/>" +
				"  </Border>" +
				"</Grid>"),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		var content = TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");

		await RequestOnCell(f, 3, DepartmentCol);

		VerifyOpened(f, "ContentMenu", () => content, f.P(3), "UIA on row 3 / Department");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "With nested content flyouts, UIA/keyboard requests pick the outermost one.")]
	public async Task OutermostContentFlyoutWins()
	{
		var f = await LoadFixture(departmentTemplate: ContentTemplate(
			"<Grid x:Name='ContentRoot' Background='Transparent'>" +
			"  <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
			"  <Border x:Name='ContentInner' Background='Transparent'>" +
			"    <Border.ContextFlyout><MenuFlyout/></Border.ContextFlyout>" +
			"    <TextBlock Text='{Binding Role}'/>" +
			"  </Border>" +
			"</Grid>"));
		var outer = TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");
		TrackContentMenu(f, 3, DepartmentCol, "ContentInner", "ContentMenu2");

		await RequestOnCell(f, 3, DepartmentCol);

		VerifyOpened(f, "ContentMenu", () => outer, f.P(3), "UIA on row 3 / Department");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout on collapsed content is not a candidate.")]
	public async Task CollapsedContentFlyoutIgnored()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(
				"<Grid>" +
				"  <Border x:Name='ContentRoot' Visibility='Collapsed'>" +
				"    <Border.ContextFlyout><MenuFlyout/></Border.ContextFlyout>" +
				"  </Border>" +
				"  <TextBlock Text='{Binding Role}'/>" +
				"</Grid>"),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");

		await RequestOnCell(f, 3, DepartmentCol);

		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, DepartmentCol), f.P(3), "UIA on row 3 / Department");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A text control's built-in copy/select flyout is not an app menu; the cell menu still opens.")]
	public async Task TextControlDefaultFlyoutIgnored()
	{
		var cases = new[]
		{
			("TextBox", "<TextBox Text='{Binding Name}' IsReadOnly='True'/>"),
			("selectable TextBlock", "<TextBlock Text='{Binding Name}' IsTextSelectionEnabled='True'/>"),
		};

		foreach (var (label, xaml) in cases)
		{
			var f = await LoadFixture(nameTemplate: ContentTemplate(xaml));

			await RequestOnCell(f, 3, NameCol);

			VerifyOpened(f, "CellMenu", () => CellOf(f, 3, NameCol), f.P(3), $"Name cell is a {label}");
			await CloseMenus(f);
		}
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout set locally on a text control is an app menu and wins.")]
	public async Task TextControlLocalFlyoutCounts()
	{
		var f = await LoadFixture(nameTemplate: ContentTemplate(
			"<TextBox x:Name='ContentRoot' Text='{Binding Name}' IsReadOnly='True'>" +
			"  <TextBox.ContextFlyout><MenuFlyout/></TextBox.ContextFlyout>" +
			"</TextBox>"));
		var content = TrackContentMenu(f, 3, NameCol, ContentRootName, "ContentMenu");

		await RequestOnCell(f, 3, NameCol);

		VerifyOpened(f, "ContentMenu", () => content, f.P(3), "UIA on row 3 / Name (TextBox)");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "The content search stops at a nested TableView.")]
	public async Task NestedTableViewFlyoutsNotPickedByOuter()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(
				"<tv:TableView x:Name='ContentRoot' Height='40' Width='120'>" +
				"  <tv:TableView.ContextFlyout><MenuFlyout/></tv:TableView.ContextFlyout>" +
				"</tv:TableView>"),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "InnerTableMenu");

		await RequestOnCell(f, 3, DepartmentCol);

		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, DepartmentCol), f.P(3), "UIA on row 3 / Department");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "The content search stops at a nested items control, like it does at a nested TableView.")]
	public async Task NestedItemsControlFlyoutNotPicked()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(
				"<ListView Height='40' Width='120'>" +
				"  <ListViewItem x:Name='ContentRoot'>" +
				"    <ListViewItem.ContextFlyout><MenuFlyout/></ListViewItem.ContextFlyout>" +
				"    <TextBlock Text='item'/>" +
				"  </ListViewItem>" +
				"</ListView>"),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");

		await RequestOnCell(f, 3, DepartmentCol);

		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, DepartmentCol), f.P(3), "UIA on row 3 / Department (ListView content)");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A request raised on an element inside the cell template opens the template flyout, not CellContextFlyout.")]
	public async Task RequestFromCellContentWinsOverCellMenu()
	{
		var f = await LoadFixture(
			departmentTemplate: ContentTemplate(ButtonFlyoutContent("{Binding Role}")),
			configure: x => x.DepartmentColumn.CellContextFlyout = x.CellMenu);
		var content = TrackContentMenu(f, 3, DepartmentCol, ContentRootName, "ContentMenu");

		await RequestOnElement(() => FindNamed(CellOf(f, 3, DepartmentCol), ContentButtonName));

		VerifyOpened(f, "ContentMenu", () => content, expectedItem: null, "UIA on the button in row 3 / Department");
		await CloseMenus(f);
	}

	// ---- B. Row targets -----------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "A row peer request targets the row even after a cell request made Name the current column.")]
	public async Task RowPeerShowContextMenuOpensRowMenu()
	{
		var f = await LoadFixture();

		await RequestOnCell(f, 3, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 3, NameCol), f.P(3), "precondition: Name is now the current column");
		await CloseMenus(f);

		await RequestOnRow(f, 3);

		VerifyOpened(f, "RowMenu", () => RowOf(f, 3), f.P(3), "UIA on row 3's peer");
		await CloseMenus(f);
	}

	// ---- C. Column headers --------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "A header request opens the column's HeaderContextFlyout anchored to that header; its items see no row item, even after a body request.")]
	public async Task HeaderMenuOpensOnHeader()
	{
		var f = await LoadFixture();
		await RequestOnCell(f, 3, OfficeCol);
		await CloseMenus(f);

		await RequestOnHeader(f, NameCol);

		VerifyOpened(f, "HeaderMenu", () => HeaderOf(f, NameCol), NoItem, "UIA on the Name header");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A header without a menu falls back to TableView.ContextFlyout, never to cell or row menus.")]
	public async Task HeaderWithoutMenuFallsBackToTableMenu()
	{
		var f = await LoadFixture(configure: x => x.OfficeColumn.CellContextFlyout = x.CellMenu);

		await RequestOnHeader(f, OfficeCol);

		VerifyOpened(f, "TableMenu", () => f.Table, expectedItem: null, "UIA on the Office header");
		VerifyMenuPlacedAtTarget(f, () => HeaderOf(f, OfficeCol));
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout in the header template wins over HeaderContextFlyout.")]
	public async Task HeaderTemplateFlyoutWinsOverHeaderMenu()
	{
		var f = await LoadFixture(configure: x => x.NameColumn.HeaderTemplate = ContentTemplate(
			"<Grid x:Name='ContentRoot' Background='Transparent'>" +
			"  <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
			"  <TextBlock Text='{Binding}'/>" +
			"</Grid>"));
		FrameworkElement content = null;
		RunOnUIThread.Execute(() => content = TrackNamedMenu(f, HeaderOf(f, NameCol), ContentRootName, "ContentMenu"));

		await RequestOnHeader(f, NameCol);

		VerifyOpened(f, "ContentMenu", () => content, expectedItem: null, "UIA on the Name header");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A header request focuses the header and leaves body selection unchanged; focus returns to the header when the menu closes.")]
	public async Task HeaderRequestDoesNotMoveBodyState()
	{
		var f = await LoadFixture();
		await SelectRow(f, 2);
		await FocusRow(f, 2);

		await RequestOnHeader(f, NameCol);
		VerifyOpened(f, "HeaderMenu", () => HeaderOf(f, NameCol), expectedItem: null, "UIA on the Name header");
		await CloseMenus(f);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(f.P(2), f.Table.SelectedItem, "A header menu must not change the selection.");
			VerifyFocusWithin(f, HeaderOf(f, NameCol), "after the header menu closes");
		});
	}

	[TestMethod]
	[TestProperty("Description", "A header peer for a collapsed column must not open any menu.")]
	public async Task CollapsedColumnHeaderPeerOpensNoMenu()
	{
		var f = await LoadFixture(configure: x => x.OfficeColumn.HeaderContextFlyout = x.HeaderMenu);
		await SelectRow(f, 2);
		await FocusRow(f, 2);

		AutomationPeer peer = null;
		RunOnUIThread.Execute(() => f.OfficeColumn.Visibility = Visibility.Collapsed);
		await SettleLayout(f.Table);
		RunOnUIThread.Execute(() =>
		{
			peer = new TableViewColumnHeaderAutomationPeer(f.Table, f.OfficeColumn);
			peer.ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(0, f.Recorder.Opened.Count,
				$"No menu may open for a collapsed column's header. Opened: {f.Recorder.Describe()}");
			Verify.AreSame(f.P(2), f.Table.SelectedItem, "Selection must be unchanged.");
		});
		await CloseMenus(f);
		RunOnUIThread.Execute(() => VerifyFocusWithin(f, RowOf(f, 2), "after the request"));
	}

	[TestMethod]
	[TestProperty("Description", "A TableView-owned header peer (public constructor) forwards ShowContextMenu to the realized header, so the column's header menu opens rather than the table menu.")]
	public async Task TableOwnedHeaderPeerOpensHeaderMenu()
	{
		var f = await LoadFixture();

		RunOnUIThread.Execute(() => new TableViewColumnHeaderAutomationPeer(f.Table, f.NameColumn).ShowContextMenu());
		await TestServices.WindowHelper.WaitForIdle();

		VerifyOpened(f, "HeaderMenu", () => HeaderOf(f, NameCol), expectedItem: null, "UIA on a TableView-owned Name header peer");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A header template flyout wins over TableView.ContextFlyout when the column has no HeaderContextFlyout, and cell menus never apply.")]
	public async Task HeaderTemplateFlyoutWinsOverTableMenu()
	{
		var f = await LoadFixture(configure: x =>
		{
			x.OfficeColumn.CellContextFlyout = x.CellMenu;
			x.OfficeColumn.HeaderTemplate = ContentTemplate(
				"<Grid x:Name='ContentRoot' Background='Transparent'>" +
				"  <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
				"  <TextBlock Text='{Binding}'/>" +
				"</Grid>");
		});
		FrameworkElement content = null;
		RunOnUIThread.Execute(() => content = TrackNamedMenu(f, HeaderOf(f, OfficeCol), ContentRootName, "ContentMenu"));

		await RequestOnHeader(f, OfficeCol);

		VerifyOpened(f, "ContentMenu", () => content, expectedItem: null, "UIA on the Office header");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A request raised on an element inside the header template opens the template flyout, not HeaderContextFlyout.")]
	public async Task RequestFromHeaderContentWinsOverHeaderMenu()
	{
		var f = await LoadFixture(configure: x => x.NameColumn.HeaderTemplate = ContentTemplate(ButtonFlyoutContent("{Binding}")));
		FrameworkElement content = null;
		RunOnUIThread.Execute(() => content = TrackNamedMenu(f, HeaderOf(f, NameCol), ContentRootName, "ContentMenu"));

		await RequestOnElement(() => FindNamed(HeaderOf(f, NameCol), ContentButtonName));

		VerifyOpened(f, "ContentMenu", () => content, expectedItem: null, "UIA on the button in the Name header");
		await CloseMenus(f);
	}

	// ---- D. Group headers ---------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "Group headers never use cell or row menus; the table menu opens.")]
	public async Task GroupHeaderUsesTableMenu()
	{
		var f = await LoadFixture(grouped: true);

		RunOnUIThread.Execute(() => GetGroupHeaderPeer(f.Table, 0).ShowContextMenu());
		await TestServices.WindowHelper.WaitForIdle();

		VerifyOpened(f, "TableMenu", () => f.Table, expectedItem: null, "UIA on the first group header");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A group header ContextFlyout from an implicit style wins over TableView.ContextFlyout, and its items see the TableViewGroupInfo.")]
	public async Task GroupHeaderStyleMenuWins()
	{
		var f = await LoadFixture(grouped: true, configure: x =>
		{
			var style = (Style)XamlReader.Load(
				"<Style " + XamlNamespaces + " TargetType='tv:TableViewGroupHeader'>" +
				"  <Setter Property='ContextFlyout'><Setter.Value><MenuFlyout><MenuFlyoutItem Text='{Binding KeyText}'/></MenuFlyout></Setter.Value></Setter>" +
				"</Style>");
			x.Recorder.Track("GroupHeaderMenu", SetterFlyout(style));
			x.Table.Resources[typeof(TableViewGroupHeader)] = style;
		});

		RunOnUIThread.Execute(() => GetGroupHeaderPeer(f.Table, 0).ShowContextMenu());
		await TestServices.WindowHelper.WaitForIdle();

		object groupInfo = null;
		RunOnUIThread.Execute(() => groupInfo = GetGroupHeader(f.Table, 0).Content);
		Verify.IsTrue(groupInfo is TableViewGroupInfo, "Precondition: a group header's Content is its TableViewGroupInfo.");
		VerifyOpened(f, "GroupHeaderMenu", () => GetGroupHeader(f.Table, 0), groupInfo, "UIA on the first group header");
		RunOnUIThread.Execute(() =>
		{
			var item = (MenuFlyoutItem)((MenuFlyout)f.Recorder.Opened[0].Flyout).Items[0];
			Verify.AreEqual(((TableViewGroupInfo)groupInfo).KeyText, item.Text, "{Binding KeyText} on a group header menu item must resolve.");
		});
		await CloseMenus(f);
	}

	// ---- E. Side effects ----------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "A body request does not change the selection.")]
	public async Task BodyRequestDoesNotChangeSelection()
	{
		var f = await LoadFixture();
		await SelectRow(f, 1);

		await RequestOnCell(f, 5, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 5, NameCol), f.P(5), "UIA on row 5 / Name");

		RunOnUIThread.Execute(() => Verify.AreSame(f.P(1), f.Table.SelectedItem, "Opening a menu must not select the target row."));
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A body request moves focus into the target row, and focus is still there after the menu closes.")]
	public async Task BodyRequestMovesFocusIntoTargetRow()
	{
		var f = await LoadFixture();
		await FocusRow(f, 1);

		await RequestOnCell(f, 5, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 5, NameCol), f.P(5), "UIA on row 5 / Name");
		RunOnUIThread.Execute(() => VerifyWithin(f.Recorder.Opened[0].FocusAtOpening, RowOf(f, 5), "focus when the menu opens"));

		await CloseMenus(f);
		RunOnUIThread.Execute(() => VerifyFocusWithin(f, RowOf(f, 5), "after the menu closes"));
	}

	[TestMethod]
	[TestProperty("Description", "A cell request focuses the exact target cell; a row request focuses the row itself.")]
	public async Task BodyRequestFocusesTargetCell()
	{
		var f = await LoadFixture();
		await FocusRow(f, 1);

		await RequestOnCell(f, 5, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 5, NameCol), f.P(5), "UIA on row 5 / Name");
		RunOnUIThread.Execute(() => Verify.AreSame(CellOf(f, 5, NameCol), f.Recorder.Opened[0].FocusAtOpening,
			$"Focus must be on the row 5 Name cell when the menu opens; was {Describe(f.Recorder.Opened[0].FocusAtOpening)}."));
		await CloseMenus(f);

		await RequestOnRow(f, 5);
		VerifyOpened(f, "RowMenu", () => RowOf(f, 5), f.P(5), "UIA on row 5's peer");
		RunOnUIThread.Execute(() => Verify.AreSame(RowOf(f, 5), f.Recorder.Opened[0].FocusAtOpening,
			$"Focus must be on row 5 itself for a row request; was {Describe(f.Recorder.Opened[0].FocusAtOpening)}."));
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A row-level menu request records its row as the current row even though the menu has taken focus by the time the row's GotFocus runs (GH #12442).")]
	public async Task RowRequestRecordsRowAsCurrentWhileMenuHasFocus()
	{
		Button after = null;
		var f = await LoadFixture(configure: x =>
		{
			// Headers hidden: the body is the table's tab stop.
			x.Table.HeadersVisibility = TableViewHeadersVisibility.None;
			after = new Button { Content = "After" };
			x.Root.Child = null;
			var panel = new StackPanel();
			panel.Children.Add(x.Table);
			panel.Children.Add(after);
			x.Root.Child = panel;
		});
		await FocusRow(f, 1);

		await RequestOnRow(f, 4);
		VerifyOpened(f, "RowMenu", () => RowOf(f, 4), f.P(4), "UIA on row 4's peer");

		// A menu command that moves focus out of the table, so closing the menu does not refocus row 4.
		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(after.Focus(FocusState.Keyboard), "Precondition: the button takes focus.");
			f.Recorder.HideAll();
			f.Recorder.Clear();
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(after, FocusManager.GetFocusedElement(f.Table.XamlRoot), "Precondition: focus stays on the button.");
			Verify.IsTrue(FocusManager.TryMoveFocus(FocusNavigationDirection.Previous,
				new FindNextElementOptions { SearchRoot = f.Table.XamlRoot.Content }), "Shift+Tab from the button must move focus.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.AreSame(RowOf(f, 4), FocusManager.GetFocusedElement(f.Table.XamlRoot),
			$"Shift+Tab back in must return to row 4, the row the menu was opened on; was {Describe(FocusManager.GetFocusedElement(f.Table.XamlRoot) as DependencyObject)}."));
	}

	[TestMethod]
	[TestProperty("Description", "A row menu requested in the same turn as a cell focus on that drilled-in row leaves the row at row level, so closing the menu restores focus to the row (GH #12442).")]
	public async Task RowRequestAfterSameTurnCellFocusKeepsRowLevel()
	{
		var f = await LoadFixture(configure: x => x.OfficeColumn.CellContextFlyout = x.CellMenu);
		RunOnUIThread.Execute(() => GetCellPeer(f.Table, f.P(4), NameCol).SetFocus());
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => Verify.IsFalse(RowOf(f, 4).IsTabStop, "Precondition: row 4 is at cell level."));

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(CellOf(f, 4, OfficeCol).Focus(FocusState.Keyboard), "Precondition: the Office cell takes focus.");
			GetRowPeer(RowOf(f, 4)).ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();

		VerifyOpened(f, "RowMenu", () => RowOf(f, 4), f.P(4), "UIA on row 4's peer");
		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(RowOf(f, 4).IsTabStop, "Row 4 must be at row level while its row menu is open.");
			Verify.IsFalse(CellOf(f, 4, OfficeCol).IsTabStop, "Row 4's cells must be out of the tab order.");
		});

		await CloseMenus(f);
		RunOnUIThread.Execute(() => Verify.AreSame(RowOf(f, 4), FocusManager.GetFocusedElement(f.Table.XamlRoot),
			$"Closing the menu must restore focus to row 4; was {Describe(FocusManager.GetFocusedElement(f.Table.XamlRoot) as DependencyObject)}."));
	}

	[TestMethod]
	[TestProperty("Description", "A row menu opened from a request on an inactive cell focuses that cell's row and leaves the selection unchanged.")]
	public async Task RowMenuFromInactiveCellRequestFocusesRow()
	{
		var f = await LoadFixture();
		await SelectRow(f, 1);
		await FocusRow(f, 1);

		await RequestOnCell(f, 5, OfficeCol);
		VerifyOpened(f, "RowMenu", () => RowOf(f, 5), f.P(5), "UIA on row 5 / Office");

		RunOnUIThread.Execute(() =>
		{
			// A read-only cell with no tooltip, control or menu is not a focus target.
			Verify.AreSame(RowOf(f, 5), f.Recorder.Opened[0].FocusAtOpening,
				$"Focus must be on row 5 when the row menu opens; was {Describe(f.Recorder.Opened[0].FocusAtOpening)}.");
			Verify.AreSame(f.P(1), f.Table.SelectedItem, "Selection must be unchanged.");
		});
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "While any cell is editing, a request on another row's cell opens no menu (neither its cell, row nor table menu).")]
	public async Task CellRequestOnOtherRowSuppressedWhileEditing()
	{
		var f = await LoadEditableFixture();
		await RequestWhileEditing(f, () => GetCellPeer(f.Table, f.P(6), NameCol).ShowContextMenu());
		await VerifyNoMenuOpened(f, "UIA on row 6 / Name while row 3 / Name is editing");
	}

	[TestMethod]
	[TestProperty("Description", "While any cell is editing, a row request opens no menu.")]
	public async Task RowRequestSuppressedWhileEditing()
	{
		var f = await LoadEditableFixture();
		await RequestWhileEditing(f, () => ((AutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(RowOf(f, 6))).ShowContextMenu());
		await VerifyNoMenuOpened(f, "UIA on row 6 while row 3 / Name is editing");
	}

	[TestMethod]
	[TestProperty("Description", "While any cell is editing, a column header request opens no menu (neither HeaderContextFlyout nor the table menu).")]
	public async Task HeaderRequestSuppressedWhileEditing()
	{
		var f = await LoadEditableFixture();
		await RequestWhileEditing(f, () => new TableViewColumnHeaderAutomationPeer(f.Table, f.NameColumn).ShowContextMenu());
		await VerifyNoMenuOpened(f, "UIA on the Name header while row 3 / Name is editing");
	}

	[TestMethod]
	[TestProperty("Description", "While any cell is editing, a group header request opens no menu (neither its style ContextFlyout nor the table menu).")]
	public async Task GroupHeaderRequestSuppressedWhileEditing()
	{
		var f = await LoadEditableFixture(grouped: true, configure: x =>
		{
			var style = (Style)XamlReader.Load(
				"<Style " + XamlNamespaces + " TargetType='tv:TableViewGroupHeader'>" +
				"  <Setter Property='ContextFlyout'><Setter.Value><MenuFlyout><MenuFlyoutItem Text='Group'/></MenuFlyout></Setter.Value></Setter>" +
				"</Style>");
			x.Recorder.Track("GroupHeaderMenu", SetterFlyout(style));
			x.Table.Resources[typeof(TableViewGroupHeader)] = style;
		});
		await RequestWhileEditing(f, () => GetGroupHeaderPeer(f.Table, 0).ShowContextMenu());
		await VerifyNoMenuOpened(f, "UIA on the first group header while row 3 / Name is editing");
	}

	[TestMethod]
	[TestProperty("Description", "While any cell is editing, a request on the TableView itself opens no menu.")]
	public async Task TableRequestSuppressedWhileEditing()
	{
		var f = await LoadEditableFixture();
		await RequestWhileEditing(f, () => ((AutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(f.Table)).ShowContextMenu());
		await VerifyNoMenuOpened(f, "UIA on the TableView while row 3 / Name is editing");
	}

	[TestMethod]
	public async Task NativeRowMenuSuppressedAfterVetoedCommit()
	{
		await VerifyNativeOwnerMenuSuppressedAfterVetoedCommit(grouped: false);
	}

	[TestMethod]
	public async Task NativeGroupHeaderMenuSuppressedAfterVetoedCommit()
	{
		await VerifyNativeOwnerMenuSuppressedAfterVetoedCommit(grouped: true);
	}

	[TestMethod]
	public async Task SharedRowMenuRemainsAvailableOutsideTableWhileEditing()
	{
		Button other = null;
		var f = await LoadEditableFixture(configure: x =>
		{
			other = new Button { Content = "Other owner", ContextFlyout = x.RowMenu };
			x.Root.Child = null;
			x.Root.Child = new StackPanel { Children = { x.Table, other } };
		});
		bool veto = true;
		RunOnUIThread.Execute(() =>
		{
			f.Table.CellEditEnding += (s, e) => e.Cancel = veto;
			var provider = GetCellPeer(f.Table, f.P(3), NameCol).GetPattern(PatternInterface.Value) as IValueProvider;
			Verify.IsNotNull(provider);
			Verify.Throws<Exception>(() => provider.SetValue("Uncommitted"));
		});

		await RequestOnElement(() => other);

		VerifyOpened(f, "RowMenu", () => other, expectedItem: null, "a shared flyout on an unrelated control");
		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(f.Table.IsEditing, "An unrelated menu must not discard the vetoed edit.");
			veto = false;
			f.Table.CancelEdit();
		});
		await CloseMenus(f);
	}

	private async Task VerifyNativeOwnerMenuSuppressedAfterVetoedCommit(bool grouped)
	{
		var f = await LoadEditableFixture(grouped: grouped);
		bool veto = true;
		FlyoutBase menu = null;
		int opened = 0;
		RunOnUIThread.Execute(() =>
		{
			f.Table.CellEditEnding += (s, e) => e.Cancel = veto;
			var provider = GetCellPeer(f.Table, f.P(3), NameCol).GetPattern(PatternInterface.Value) as IValueProvider;
			Verify.IsNotNull(provider);
			Verify.Throws<Exception>(() => provider.SetValue("Uncommitted"));
			Verify.IsTrue(f.Table.IsEditing, "Precondition: the vetoed commit leaves an edit open.");

			FrameworkElement owner = grouped ? (FrameworkElement)GetGroupHeader(f.Table, 0) : RowOf(f, 6);
			menu = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Native menu" } } };
			menu.Opened += (s, e) => opened++;
			menu.Closing += (s, e) => e.Cancel = true;
			owner.ContextFlyout = menu;
			Verify.IsTrue(owner.Focus(FocusState.Programmatic), "Precondition: focus leaves the editor.");
		});
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() =>
		{
			FrameworkElement owner = grouped ? (FrameworkElement)GetGroupHeader(f.Table, 0) : RowOf(f, 6);
			// Bypass the custom peer's guard: keyboard requests originate directly on the owner.
			new FrameworkElementAutomationPeer(owner).ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(menu.IsOpen, "An owner-originated native request must not open a menu while editing.");
			Verify.AreEqual(0, opened, "Suppression must cancel opening before the popup becomes visible.");
			Verify.IsTrue(f.Table.IsEditing, "Suppressing the menu must preserve the uncommitted edit.");
			veto = false;
			f.Table.CancelEdit();
		});
		await CloseMenus(f);
	}

	private Task<ContextMenuFixture> LoadEditableFixture(bool grouped = false, Action<ContextMenuFixture> configure = null) => LoadFixture(grouped: grouped, configure: x =>
	{
		x.Table.IsReadOnly = false;
		((TableViewTextColumn)x.NameColumn).Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.TwoWay };
		configure?.Invoke(x);
	});

	// Raises the request from BeginningEdit, where IsEditing is already true, so the edit does not
	// have to be held open past the SetValue call.
	internal static async Task RequestWhileEditing(ContextMenuFixture f, Action request)
	{
		bool? wasEditing = null;
		RunOnUIThread.Execute(() =>
		{
			f.Table.BeginningEdit += (s, e) =>
			{
				if (wasEditing.HasValue)
				{
					return;
				}

				wasEditing = f.Table.IsEditing;
				request();
			};

			var provider = GetCellPeer(f.Table, f.P(3), NameCol).GetPattern(PatternInterface.Value) as IValueProvider;
			Verify.IsNotNull(provider, "Precondition: the Name cell must be editable.");
			provider.SetValue("Edited");
		});
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => Verify.IsTrue(wasEditing == true, "Precondition: IsEditing must be true inside BeginningEdit."));
	}

	internal static async Task VerifyNoMenuOpened(ContextMenuFixture f, string when)
	{
		RunOnUIThread.Execute(() => Verify.AreEqual(0, f.Recorder.Opened.Count,
			$"No TableView menu may open while a cell is editing ({when}). Opened: {f.Recorder.Describe()}"));
		await CloseMenus(f);
	}

	// ---- F. DataContext, sharing and recycling ------------------------------------------

	[TestMethod]
	[TestProperty("Description", "One shared cell menu sees the requested row's item every time.")]
	public async Task SharedCellMenuSeesRequestedRowItem()
	{
		var f = await LoadFixture();

		await RequestOnCell(f, 2, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 2, NameCol), f.P(2), "first request, row 2");
		await CloseMenus(f);

		await RequestOnCell(f, 7, NameCol);
		VerifyOpened(f, "CellMenu", () => CellOf(f, 7, NameCol), f.P(7), "second request, row 7");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A recycled row container's menu sees its new item.")]
	public async Task RecycledRowMenuSeesNewItem()
	{
		var f = await LoadFixture(itemCount: 300);
		var original = new Dictionary<TableViewRow, object>();

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in GetRealizedRows(f.Table))
			{
				original[row] = row.DataContext;
			}
		});
		await RequestOnRow(f, 1);
		VerifyOpened(f, "RowMenu", () => RowOf(f, 1), f.P(1), "row container before scrolling");
		await CloseMenus(f);

		// Any original container that is now the live container for a different item was recycled.
		TableViewRow container = null;
		object recycledItem = null;
		for (var offset = 2000.0; offset <= 8000.0 && container == null; offset += 2000.0)
		{
			await ScrollBodyTo(f.Table, ScrollAxis.Vertical, offset);
			RunOnUIThread.Execute(() =>
			{
				foreach (var pair in original)
				{
					var item = pair.Key.DataContext;
					if (item != null && !ReferenceEquals(item, pair.Value) &&
						ReferenceEquals(GetRowForItem(f.Table, item), pair.Key))
					{
						container = pair.Key;
						recycledItem = item;
						break;
					}
				}
			});
		}

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(container, "Precondition: a row container must be reused for a different item.");
			GetRowPeer(container).ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();

		VerifyOpened(f, "RowMenu", () => container, recycledItem, "the recycled row container");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A ContextFlyout on the row template root opens with the row's item.")]
	public async Task RowTemplateMenuSmoke()
	{
		var f = await LoadFixture(configure: x =>
		{
			x.Table.Resources[typeof(TableViewRow)] = (Style)XamlReader.Load(RowTemplateStyleXaml);
		});
		FrameworkElement templateRoot = null;
		RunOnUIThread.Execute(() => templateRoot = TrackNamedMenu(f, RowOf(f, 3), "TemplateRoot", "RowTemplateMenu"));

		await RequestOnCell(f, 3, OfficeCol);

		VerifyOpened(f, "RowTemplateMenu", () => templateRoot, f.P(3), "UIA on row 3 / Office");
		await CloseMenus(f);
	}

	[TestMethod]
	public async Task RowPeerResolvesTemplateMenusBeforeRowMenu()
	{
		var f = await LoadFixture(configure: x =>
			x.Table.Resources[typeof(TableViewRow)] = (Style)XamlReader.Load(RowTemplateStyleXaml));
		FrameworkElement root = null;
		RunOnUIThread.Execute(() =>
		{
			var row = RowOf(f, 3);
			row.ContextFlyout = f.RowMenu;
			root = TrackNamedMenu(f, row, "TemplateRoot", "RowTemplateMenu");
		});

		await RequestOnRow(f, 3);

		VerifyOpened(f, "RowTemplateMenu", () => root, f.P(3), "UIA on the row rather than a cell");
		await CloseMenus(f);

		FrameworkElement host = null;
		RunOnUIThread.Execute(() =>
		{
			host = FindNamed(RowOf(f, 3), "PART_CellsHost");
			host.ContextFlyout = f.Recorder.Create("CellsHostMenu");
		});
		await RequestOnRow(f, 3);
		VerifyOpened(f, "CellsHostMenu", () => host, f.P(3), "cells-host menu before template-root and row menus");
		await CloseMenus(f);
	}

	[TestMethod]
	public async Task OpenGroupMenuKeepsOriginalGroupAfterHeaderRecycling()
	{
		var f = await LoadFixture(grouped: true, itemCount: 200);
		TableViewGroupHeader header = null;
		TableViewGroupInfo original = null;
		object originalKey = null;
		MenuFlyout menu = null;
		RunOnUIThread.Execute(() =>
		{
			header = GetGroupHeader(f.Table, 0);
			original = (TableViewGroupInfo)header.Content;
			originalKey = original.Key;
			menu = f.Recorder.Create("GroupMenu");
			header.ContextFlyout = menu;
			GetGroupHeaderPeer(f.Table, 0).ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => Verify.AreSame(original, menu.Items[0].DataContext,
			"Precondition: the open menu holds the original group projection."));

		bool recycled = false;
		for (double offset = 2000; offset <= 12000 && !recycled; offset += 2000)
		{
			await ScrollBodyTo(f.Table, ScrollAxis.Vertical, offset);
			RunOnUIThread.Execute(() =>
			{
				var current = header.Content as TableViewGroupInfo;
				recycled = current != null && !Equals(current.Key, originalKey);
			});
		}

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(recycled, "Precondition: the original header must be reused for another group.");
			Verify.AreEqual(originalKey, original.Key, "A retained projection must never change group identity.");
			if (menu.IsOpen)
			{
				Verify.AreSame(original, menu.Items[0].DataContext,
					"Commands in an open menu must still receive the originally requested group.");
			}
		});
		await CloseMenus(f);
	}

	// ---- G. Robustness ------------------------------------------------------------------

	[TestMethod]
	[TestProperty("Description", "An item removed while focus moves to the target row suppresses the menu.")]
	public async Task ItemRemovedDuringFocusMoveSuppressesMenu()
	{
		var f = await LoadFixture();
		await FocusRow(f, 1);
		Person target = null;
		RunOnUIThread.Execute(() => target = f.P(5));
		var focusMoves = OnFirstFocusInto(f, 5, () => f.Items.Remove(target));

		await RequestOnCell(f, 5, NameCol);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, focusMoves(), "Precondition: the request must move focus into row 5 exactly once.");
			Verify.AreEqual(0, f.Recorder.Opened.Count, $"No menu may open for a removed item. Opened: {f.Recorder.Describe()}");
		});
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A column removed while focus moves to the target row is not a target.")]
	public async Task ColumnRemovedDuringFocusMoveSuppressesMenu()
	{
		var f = await LoadFixture();
		await FocusRow(f, 1);
		var focusMoves = OnFirstFocusInto(f, 5, () => f.Table.Columns.Remove(f.NameColumn));

		await RequestOnCell(f, 5, NameCol);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, focusMoves(), "Precondition: the request must move focus into row 5 exactly once.");
			Verify.AreEqual(0, f.Recorder.Opened.Count,
				$"A request whose column was removed must be suppressed, not passed to another menu. Opened: {f.Recorder.Describe()}");
		});
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A CellContextFlyout replaced while focus moves to the target row is the menu that opens.")]
	public async Task CellMenuReplacedDuringFocusMoveShowsNewMenu()
	{
		MenuFlyout replacement = null;
		var f = await LoadFixture();
		await FocusRow(f, 1);
		RunOnUIThread.Execute(() => replacement = f.Recorder.Create("ReplacementMenu"));
		var focusMoves = OnFirstFocusInto(f, 5, () => f.NameColumn.CellContextFlyout = replacement);

		await RequestOnCell(f, 5, NameCol);

		RunOnUIThread.Execute(() => Verify.IsTrue(focusMoves() >= 1, "Precondition: the request must move focus into row 5."));
		VerifyOpened(f, "ReplacementMenu", () => CellOf(f, 5, NameCol), f.P(5), "UIA on row 5 / Name");
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A row or cell peer whose container went back to the pool raises ElementNotAvailable from ShowContextMenu and opens no menu.")]
	public async Task PooledRowPeerShowContextMenuOpensNoMenu()
	{
		var f = await LoadFixture();
		AutomationPeer rowPeer = null;
		AutomationPeer cellPeer = null;
		RunOnUIThread.Execute(() =>
		{
			rowPeer = GetRowPeer(RowOf(f, 5));
			cellPeer = GetCellPeer(f.Table, f.P(5), OfficeCol);
			f.Items.Clear();
		});
		await SettleLayout(f.Table);

		foreach (var (peer, name) in new[] { (rowPeer, "row"), (cellPeer, "cell") })
		{
			RunOnUIThread.Execute(() => Verify.Throws<Exception>(() => peer.ShowContextMenu()));
			await TestServices.WindowHelper.WaitForIdle();
			RunOnUIThread.Execute(() => Verify.AreEqual(0, f.Recorder.Opened.Count,
				$"No menu may open from the pooled {name} peer. Opened: {f.Recorder.Describe()}"));
		}
		await CloseMenus(f);
	}

	[TestMethod]
	public async Task PooledGroupHeaderPeerShowContextMenuOpensNoMenu()
	{
		var f = await LoadFixture(grouped: true);
		AutomationPeer peer = null;
		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(f.Table, 0);
			header.ContextFlyout = f.Recorder.Create("GroupMenu");
			peer = GetGroupHeaderPeer(f.Table, 0);
			f.Items.Clear();
		});
		await SettleLayout(f.Table);
		RunOnUIThread.Execute(() => Verify.Throws<Exception>(() => peer.ShowContextMenu()));
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => Verify.AreEqual(0, f.Recorder.Opened.Count,
			"A recycled group-header peer must not open either its menu or the table fallback."));
		await CloseMenus(f);
	}

	[TestMethod]
	[TestProperty("Description", "A TableViewColumn in a content element's Tag does not redirect the request to that column.")]
	public async Task ColumnTagOnContentDoesNotRedirect()
	{
		var f = await LoadFixture(departmentTemplate: ContentTemplate("<TextBlock x:Name='ContentRoot' Text='{Binding Role}'/>"));
		RunOnUIThread.Execute(() =>
		{
			var content = FindNamed(CellOf(f, 3, DepartmentCol), ContentRootName);
			content.Tag = f.NameColumn;
			FrameworkElementAutomationPeer.CreatePeerForElement(content).ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();

		VerifyOpened(f, "RowMenu", () => RowOf(f, 3), f.P(3), "UIA on Department content tagged with the Name column");
		await CloseMenus(f);
	}

	// ---- fixture loading ----------------------------------------------------------------

	private async Task<ContextMenuFixture> LoadFixture(
		Action<ContextMenuFixture> configure = null,
		DataTemplate nameTemplate = null,
		DataTemplate departmentTemplate = null,
		bool grouped = false,
		int itemCount = 10)
	{
		ContextMenuFixture f = null;
		RunOnUIThread.Execute(() =>
		{
			f = CreateContextMenuFixture(nameTemplate, departmentTemplate, grouped, itemCount);
			configure?.Invoke(f);
			LoadContent(f.Root);
		});

		await SettleLayout(f.Table);
		return f;
	}
}

// Records which tracked flyout opened, its Target, focus at Opening, and what its items see.
internal sealed class MenuRecorder
{
	internal sealed class Entry
	{
		public string Name;
		public FlyoutBase Flyout;
		public DependencyObject Target;
		public DependencyObject FocusAtOpening;
		public object ItemsSee;
	}

	private readonly List<Entry> _opened = new List<Entry>();
	private readonly List<FlyoutBase> _tracked = new List<FlyoutBase>();

	public IReadOnlyList<Entry> Opened => _opened;

	public MenuFlyout Create(string name)
	{
		var flyout = new MenuFlyout();
		Track(name, flyout);
		return flyout;
	}

	// Every tracked MenuFlyout gets a probe item so the DataContext its items see is observable.
	public void Track(string name, FlyoutBase flyout)
	{
		Verify.IsNotNull(flyout, $"{name} must exist to be tracked.");
		if (_tracked.Contains(flyout))
		{
			return;
		}

		_tracked.Add(flyout);
		if (flyout is MenuFlyout menu && menu.Items.Count == 0)
		{
			menu.Items.Add(new MenuFlyoutItem { Text = name });
		}

		flyout.Opening += (s, e) =>
		{
			var root = flyout.Target?.XamlRoot;
			_opened.Add(new Entry
			{
				Name = name,
				Flyout = flyout,
				Target = flyout.Target,
				FocusAtOpening = root == null ? null : FocusManager.GetFocusedElement(root) as DependencyObject,
			});
		};
		flyout.Opened += (s, e) =>
		{
			var entry = _opened.LastOrDefault(o => o.Flyout == flyout);
			if (entry != null && flyout is MenuFlyout menu && menu.Items.Count > 0)
			{
				entry.ItemsSee = menu.Items[0].DataContext;
			}
		};
	}

	public void HideAll()
	{
		foreach (var flyout in _tracked.Where(fl => fl.IsOpen))
		{
			flyout.Hide();
		}
	}

	public void Clear() => _opened.Clear();

	public string Describe() => _opened.Count == 0 ? "none" : string.Join(", ", _opened.Select(o => o.Name));
}

// Three columns over 10 people: Name (text, CellMenu, HeaderMenu), Department (template) and
// Office (text). RowMenu is set on every row by an implicit row style and TableMenu is the TableView.ContextFlyout.
internal sealed class ContextMenuFixture
{
	public Border Root;
	public TableView Table;
	public ObservableCollection<Person> Items;
	public TableViewColumn NameColumn;
	public TableViewColumn DepartmentColumn;
	public TableViewColumn OfficeColumn;
	public MenuRecorder Recorder = new MenuRecorder();
	public MenuFlyout CellMenu;
	public MenuFlyout RowMenu;
	public MenuFlyout HeaderMenu;
	public MenuFlyout TableMenu;

	// 1-based: P(1) is "Person 0", the first row.
	public Person P(int n) => Items.First(p => p.Name == $"Person {n - 1}");
}

internal static class ContextMenuTestHelpers
{
	internal const int NameCol = 0;
	internal const int DepartmentCol = 1;
	internal const int OfficeCol = 2;
	internal const string ContentRootName = "ContentRoot";
	internal const string ContentButtonName = "ContentButton";

	internal const string XamlNamespaces =
		"xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
		"xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
		"xmlns:tv='using:Microsoft.UI.Xaml.Controls.Tabular'";

	internal const string RootFlyoutContent =
		"<Grid x:Name='ContentRoot' Background='Transparent'>" +
		"  <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
		"  <TextBlock Text='{Binding Role}'/>" +
		"</Grid>";

	// A root flyout around a Button, so a request can start on a control inside the template.
	internal static string ButtonFlyoutContent(string contentBinding) =>
		"<Grid x:Name='ContentRoot' Background='Transparent'>" +
		"  <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
		"  <Button x:Name='ContentButton' Content='" + contentBinding + "'/>" +
		"</Grid>";

	// The default row template's parts, without visual states, under a root Grid with a ContextFlyout.
	internal const string RowTemplateStyleXaml =
		"<Style " + XamlNamespaces + " TargetType='tv:TableViewRow'>" +
		"  <Setter Property='Template'>" +
		"    <Setter.Value>" +
		"      <ControlTemplate TargetType='tv:TableViewRow'>" +
		"        <Grid x:Name='TemplateRoot' Background='Transparent'>" +
		"          <Grid.ContextFlyout><MenuFlyout/></Grid.ContextFlyout>" +
		"          <Border x:Name='PART_RootBorder' Background='{TemplateBinding Background}'" +
		"                  BorderThickness='{TemplateBinding BorderThickness}' Padding='{TemplateBinding Padding}'>" +
		"            <Grid>" +
		"              <ContentPresenter x:Name='PART_CellForegroundPresenter' Foreground='{TemplateBinding Foreground}'" +
		"                                HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'>" +
		"                <Grid>" +
		"                  <tv:TableViewCellsPanel x:Name='PART_CellsHost' HorizontalAlignment='Left'/>" +
		"                  <Border x:Name='PART_RowExpanderGutter' Width='16' Background='Transparent'" +
		"                          HorizontalAlignment='Left' Visibility='Collapsed'>" +
		"                    <FontIcon x:Name='PART_RowExpanderIcon' FontSize='12' Glyph='&#xE76C;'/>" +
		"                  </Border>" +
		"                </Grid>" +
		"              </ContentPresenter>" +
		"              <Rectangle x:Name='PART_SelectionIndicator' Width='3' Height='16' Fill='Blue'" +
		"                         HorizontalAlignment='Left' IsHitTestVisible='False' Opacity='0'/>" +
		"            </Grid>" +
		"          </Border>" +
		"          <Border x:Name='PART_GridLineBorder' BorderBrush='{TemplateBinding BorderBrush}'" +
		"                  BorderThickness='{TemplateBinding BorderThickness}' IsHitTestVisible='False'/>" +
		"        </Grid>" +
		"      </ControlTemplate>" +
		"    </Setter.Value>" +
		"  </Setter>" +
		"</Style>";

	// Call on the UI thread.
	internal static ContextMenuFixture CreateContextMenuFixture(
		DataTemplate nameTemplate,
		DataTemplate departmentTemplate,
		bool grouped,
		int itemCount)
	{
		var f = new ContextMenuFixture
		{
			Items = new ObservableCollection<Person>(MakeManyItems(itemCount)),
		};

		object source = f.Items;
		if (grouped)
		{
			var grouping = TableViewSource.From(f.Items);
			grouping.GroupBy(item => (object)((Person)item).Role);
			source = grouping;
		}

		var cellWidth = new GridLength(150.0, GridUnitType.Pixel);
		f.Table = CreateTableViewShell(source, 600, 560);
		f.NameColumn = nameTemplate == null
			? MakeTextColumn("Name", width: cellWidth)
			: MakeTemplateColumn("Name", nameTemplate, cellWidth);
		f.DepartmentColumn = MakeTemplateColumn("Department", departmentTemplate ?? CreateBoundTextTemplate("Role"), cellWidth);
		f.OfficeColumn = MakeTextColumn("Office", "Role", cellWidth);
		f.Table.Columns.Add(f.NameColumn);
		f.Table.Columns.Add(f.DepartmentColumn);
		f.Table.Columns.Add(f.OfficeColumn);

		f.CellMenu = f.Recorder.Create("CellMenu");
		f.RowMenu = f.Recorder.Create("RowMenu");
		f.HeaderMenu = f.Recorder.Create("HeaderMenu");
		f.TableMenu = f.Recorder.Create("TableMenu");

		f.NameColumn.CellContextFlyout = f.CellMenu;
		f.NameColumn.HeaderContextFlyout = f.HeaderMenu;
		f.Table.ContextFlyout = f.TableMenu;
		f.Table.Resources[typeof(TableViewRow)] = new Style(typeof(TableViewRow))
		{
			Setters = { new Setter(UIElement.ContextFlyoutProperty, f.RowMenu) },
		};

		f.Root = new Border { Child = f.Table, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
		return f;
	}

	// XamlReader must run on the UI thread; safe to call from either thread.
	internal static T LoadXaml<T>(string xaml) where T : class
	{
		T result = null;
		RunOnUIThread.Execute(() => result = (T)XamlReader.Load(xaml));
		return result;
	}

	internal static DataTemplate ContentTemplate(string content) => LoadXaml<DataTemplate>(
		"<DataTemplate " + XamlNamespaces + ">" + content + "</DataTemplate>");

	internal static FlyoutBase SetterFlyout(Style style) => style.Setters
		.OfType<Setter>()
		.Select(s => s.Value as FlyoutBase)
		.FirstOrDefault(v => v != null);

	// ---- requests (call from the test thread) ----

	internal static async Task RequestOnCell(ContextMenuFixture f, int row, int column)
	{
		RunOnUIThread.Execute(() => GetCellPeer(f.Table, f.P(row), column).ShowContextMenu());
		await TestServices.WindowHelper.WaitForIdle();
	}

	internal static async Task RequestOnRow(ContextMenuFixture f, int row)
	{
		RunOnUIThread.Execute(() => GetRowPeer(RowOf(f, row)).ShowContextMenu());
		await TestServices.WindowHelper.WaitForIdle();
	}

	internal static async Task RequestOnHeader(ContextMenuFixture f, int column)
	{
		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(HeaderOf(f, column));
			Verify.IsNotNull(peer, "The header cell must produce an automation peer.");
			peer.ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Raises the request on an arbitrary element, e.g. a control inside a cell or header template.
	internal static async Task RequestOnElement(Func<FrameworkElement> element)
	{
		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element());
			Verify.IsNotNull(peer, "The element must produce an automation peer.");
			peer.ShowContextMenu();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	internal static async Task CloseMenus(ContextMenuFixture f)
	{
		RunOnUIThread.Execute(() =>
		{
			f.Recorder.HideAll();
			f.Recorder.Clear();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	// ---- state (call on the UI thread) ----

	internal static TableViewRow RowOf(ContextMenuFixture f, int row) => GetRowForItem(f.Table, f.P(row));

	internal static Grid CellOf(ContextMenuFixture f, int row, int column) => GetRowCell(RowOf(f, row), column);

	internal static FrameworkElement HeaderOf(ContextMenuFixture f, int column)
	{
		var headers = GetHeaderCells(f.Table);
		Verify.IsGreaterThan(headers.Count, column, "The header cell must be realized.");
		return headers[column];
	}

	internal static FrameworkElement FindNamed(DependencyObject root, string name)
	{
		var element = FindVisualChildrenByType<FrameworkElement>(root).FirstOrDefault(e => e.Name == name);
		Verify.IsNotNull(element, $"'{name}' must be realized under {root}.");
		return element;
	}

	// Tracks the ContextFlyout of a named element under root. Returns the element.
	internal static FrameworkElement TrackNamedMenu(ContextMenuFixture f, DependencyObject root, string elementName, string menuName)
	{
		var element = FindNamed(root, elementName);
		f.Recorder.Track(menuName, element.ContextFlyout);
		return element;
	}

	// Call from the test thread.
	internal static FrameworkElement TrackContentMenu(ContextMenuFixture f, int row, int column, string elementName, string menuName)
	{
		FrameworkElement element = null;
		RunOnUIThread.Execute(() => element = TrackNamedMenu(f, CellOf(f, row, column), elementName, menuName));
		return element;
	}

	internal static async Task SelectRow(ContextMenuFixture f, int row)
	{
		RunOnUIThread.Execute(() =>
		{
			var provider = GetRowPeer(RowOf(f, row)).GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
			Verify.IsNotNull(provider, "Rows must advertise SelectionItem.");
			provider.Select();
		});
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => Verify.AreSame(f.P(row), f.Table.SelectedItem, $"Precondition: row {row} is selected."));
	}

	internal static async Task FocusRow(ContextMenuFixture f, int row)
	{
		RunOnUIThread.Execute(() => Verify.IsTrue(RowOf(f, row).Focus(FocusState.Programmatic), $"Precondition: row {row} takes focus."));
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Runs action on the UI thread the first time focus moves into the row. Returns a counter of
	// focus moves into the row, read on the UI thread.
	internal static Func<int> OnFirstFocusInto(ContextMenuFixture f, int row, Action action)
	{
		int count = 0;
		RunOnUIThread.Execute(() => RowOf(f, row).GettingFocus += (s, e) =>
		{
			if (count++ == 0)
			{
				action();
			}
		});
		return () => count;
	}

	internal static bool IsWithin(DependencyObject element, DependencyObject ancestor)
	{
		for (var node = element; node != null; node = VisualTreeHelper.GetParent(node))
		{
			if (node == ancestor)
			{
				return true;
			}
		}

		return false;
	}

	internal static void VerifyWithin(DependencyObject element, DependencyObject ancestor, string when)
		=> Verify.IsTrue(IsWithin(element, ancestor), $"Focus must be within {Describe(ancestor)} {when}; was {Describe(element)}.");

	internal static void VerifyFocusWithin(ContextMenuFixture f, DependencyObject ancestor, string when)
		=> VerifyWithin(FocusManager.GetFocusedElement(f.Table.XamlRoot) as DependencyObject, ancestor, when);

	internal static string Describe(DependencyObject element)
	{
		if (element == null)
		{
			return "null";
		}

		var name = (element as FrameworkElement)?.Name;
		var dc = (element as FrameworkElement)?.DataContext as Person;
		return $"{element.GetType().Name}{(string.IsNullOrEmpty(name) ? "" : $" '{name}'")}{(dc == null ? "" : $" [{dc.Name}]")}";
	}

	// Pass as expectedItem to assert the menu's items see no DataContext; null skips the check.
	internal static readonly object NoItem = new object();

	// Exactly one tracked menu opened, it is the expected one, anchored to the expected element,
	// and its items see the expected item (when one is given).
	internal static void VerifyOpened(
		ContextMenuFixture f,
		string expectedName,
		Func<DependencyObject> expectedTarget,
		object expectedItem,
		string context)
	{
		RunOnUIThread.Execute(() =>
		{
			var opened = f.Recorder.Opened;
			Verify.AreEqual(1, opened.Count, $"Exactly one menu must open ({context}). Opened: {f.Recorder.Describe()}");
			if (opened.Count != 1)
			{
				return;
			}

			var entry = opened[0];
			Verify.AreEqual(expectedName, entry.Name, $"Menu ({context}).");

			var target = expectedTarget();
			Verify.AreSame(target, entry.Target, $"Target ({context}): expected {Describe(target)}, was {Describe(entry.Target)}.");

			if (expectedItem == NoItem)
			{
				Verify.IsNull(entry.ItemsSee, $"Menu items must see no DataContext ({context}); saw {entry.ItemsSee}.");
			}
			else if (expectedItem != null)
			{
				Verify.AreSame(expectedItem, entry.ItemsSee, $"Menu items' DataContext ({context}): saw {entry.ItemsSee}.");
			}
		});
	}

	internal static void VerifyMenuPlacedAtTarget(ContextMenuFixture f, Func<FrameworkElement> getTarget)
	{
		RunOnUIThread.Execute(() =>
		{
			var target = getTarget();
			var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(f.Table.XamlRoot)
				.Select(p => p.Child)
				.OfType<MenuFlyoutPresenter>()
				.Single();
			var cellBounds = target.TransformToVisual(null).TransformBounds(
				new global::Windows.Foundation.Rect(0, 0, target.ActualWidth, target.ActualHeight));
			var menuBounds = presenter.TransformToVisual(null).TransformBounds(
				new global::Windows.Foundation.Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));
			const double tolerance = 16;
			Verify.IsTrue(Math.Abs(menuBounds.Left - cellBounds.Left) <= tolerance ||
				Math.Abs(menuBounds.Right - cellBounds.Right) <= tolerance,
				"The table fallback must align horizontally with the requested cell.");
			Verify.IsTrue(Math.Abs(menuBounds.Top - cellBounds.Bottom) <= tolerance ||
				Math.Abs(menuBounds.Bottom - cellBounds.Top) <= tolerance,
				"The table fallback must open beside the requested cell, not the whole table.");
		});
	}
}
