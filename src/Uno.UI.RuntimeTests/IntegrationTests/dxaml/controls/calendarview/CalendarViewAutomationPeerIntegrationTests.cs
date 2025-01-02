// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0168 // Variable is declared but never used - Disabled the warning for TestCleanupWrapper

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Tests.Common;
using Windows.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using static Private.Infrastructure.CalendarHelper;

namespace Windows.UI.Tests.Enterprise.CalendarViewTests
{
	[TestClass]
	public class CalendarViewAutomationPeerIntegrationTests : BaseDxamlTestClass
	{
		// PeerFromProvider is protected even though conceptually it's a static method.
		// BridgeAutomationPeer is a little hack to allow us to call PeerFromProvider in our tests.
		private class BridgeAutomationPeer : AutomationPeer
		{
			// public
			internal AutomationPeer CallPeerFromProvider(IRawElementProviderSimple provider)
			{
				return PeerFromProvider(provider);
			}
		};

		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void TestCleanup()
		{
			TestServices.WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//

		//[TestMethod]
		//public async Task VerifyUIATree()
		//{
		//	TestCleanupWrapper cleanup;
		//	Grid rootPanel = null;

		//	var helper = new CalendarHelper.CalendarViewHelper();
		//	helper.PrepareLoadedEvent();
		//	CalendarView cv = await helper.GetCalendarView();

		//	AutomationPropertyChangedHandler spAutomationPropertyChangedEventHandler;
		//	Automation.AutomationClient.UIAElementInfo uiaInfo;
		//	uiaInfo.m_Name = "CalendarView";
		//	uiaInfo.m_AutomationID = "CalendarView";
		//	uiaInfo.m_cType = UIA_CalendarControlTypeId;

		//	IUIAutomationElement spUIAutomationElement;
		//	IUIAutomation spAutomation;
		//	IUIAutomationElement spAutomationHeaderElement;
		//	IUIAutomationInvokePattern spInvokePattern;

		//	CreateTestResources(rootPanel);

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.MinDate = ConvertToDateTime(1, 2000, 11, 15);
		//		cv.MaxDate = ConvertToDateTime(1, 2001, 1, 15);
		//		AutomationProperties.SetName(cv, uiaInfo.m_Name);
		//		rootPanel.Children.Add(cv);
		//	});

		//	helper.WaitForLoaded();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		var spAutomationClientManager =
		//			AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
		//		spAutomationClientManager.GetCurrentUIAutomationElement(spUIAutomationElement);
		//		VERIFY_IS_NOT_NULL(spUIAutomationElement);

		//		spAutomationClientManager.GetAutomation(spAutomation);
		//		VERIFY_IS_NOT_NULL(spAutomation);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationMonthViewSVCondition;
		//		Common.AutoVariant autoMonthViewSV;
		//		autoMonthViewSV.SetString("MonthViewScrollViewer");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoMonthViewSV.Storage()), spUIAutomationMonthViewSVCondition));

		//		wrl.ComPtr<IUIAutomationElement> spAutomationMonthViewSVElement;
		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationMonthViewSVCondition, spAutomationMonthViewSVElement));
		//		VERIFY_IS_NOT_NULL(spAutomationMonthViewSVElement);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDatesCondition;
		//		Common.AutoVariant autoVarType;
		//		autoVarType.SetInt(UIA_DataItemControlTypeId);
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_ControlTypePropertyId,
		//			*(autoVarType.Storage()), spUIAutomationDatesCondition));
		//		wrl.ComPtr<IUIAutomationElementArray> spDateItems;
		//		VERIFY_SUCCEEDED(spAutomationMonthViewSVElement.FindAll(TreeScope.TreeScope_Children,
		//			spUIAutomationDatesCondition, spDateItems));
		//		int size = 0;
		//		size = VERIFY_SUCCEEDED(spDateItems.Length);
		//		WEX.Logging.Log.Comment("Visible Dates are less than or equal to 42");
		//		VERIFY_IS_LESS_THAN_OR_EQUAL(size, 42);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationAutomationIDCondition;
		//		Common.AutoVariant autoHeaderButton;
		//		autoHeaderButton.SetString("HeaderButton");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoHeaderButton.Storage()), spUIAutomationAutomationIDCondition));

		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationAutomationIDCondition, spAutomationHeaderElement));
		//		VERIFY_IS_NOT_NULL(spAutomationHeaderElement);

		//		VERIFY_SUCCEEDED(spAutomationHeaderElement.GetCurrentPatternAs(UIA_InvokePatternId,
		//			__uuidof(IUIAutomationInvokePattern), spInvokePattern));
		//		VERIFY_IS_NOT_NULL(spInvokePattern);

		//		VERIFY_SUCCEEDED(spInvokePattern.Invoke());
		//	});

		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationYearViewSVCondition;
		//		Common.AutoVariant autoYearViewSV;
		//		autoYearViewSV.SetString("YearViewScrollViewer");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoYearViewSV.Storage()), spUIAutomationYearViewSVCondition));

		//		wrl.ComPtr<IUIAutomationElement> spAutomationYearViewSVElement;
		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationYearViewSVCondition, spAutomationYearViewSVElement));
		//		VERIFY_IS_NOT_NULL(spAutomationYearViewSVElement);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDatesCondition;
		//		Common.AutoVariant autoVarType;
		//		autoVarType.SetInt(UIA_ButtonControlTypeId);
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_ControlTypePropertyId,
		//			*(autoVarType.Storage()), spUIAutomationDatesCondition));
		//		wrl.ComPtr<IUIAutomationElementArray> spDateItems;
		//		VERIFY_SUCCEEDED(spAutomationYearViewSVElement.FindAll(TreeScope.TreeScope_Children,
		//			spUIAutomationDatesCondition, spDateItems));
		//		int size = 0;
		//		size = VERIFY_SUCCEEDED(spDateItems.Length);
		//		WEX.Logging.Log.Comment("There are 3 month in the view between 2000-11-15 and 2001-1-15");
		//		VERIFY_ARE_EQUAL(size, 3);

		//		VERIFY_SUCCEEDED(spInvokePattern.Invoke());
		//	});

		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDecadeViewSVCondition;
		//		Common.AutoVariant autoDecadeViewSV;
		//		autoDecadeViewSV.SetString("DecadeViewScrollViewer");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoDecadeViewSV.Storage()), spUIAutomationDecadeViewSVCondition));

		//		wrl.ComPtr<IUIAutomationElement> spAutomationDecadeViewSVElement;
		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationDecadeViewSVCondition, spAutomationDecadeViewSVElement));
		//		VERIFY_IS_NOT_NULL(spAutomationDecadeViewSVElement);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDatesCondition;
		//		Common.AutoVariant autoVarType;
		//		autoVarType.SetInt(UIA_ButtonControlTypeId);
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_ControlTypePropertyId,
		//			*(autoVarType.Storage()), spUIAutomationDatesCondition));
		//		wrl.ComPtr<IUIAutomationElementArray> spDateItems;
		//		VERIFY_SUCCEEDED(spAutomationDecadeViewSVElement.FindAll(TreeScope.TreeScope_Children,
		//			spUIAutomationDatesCondition, spDateItems));
		//		int size = 0;
		//		size = VERIFY_SUCCEEDED(spDateItems.Length);
		//		WEX.Logging.Log.Comment("There are 2 years in the view between 2000-11-15 and 2001-1-15");
		//		VERIFY_ARE_EQUAL(size, 2);
		//	});
		//}

		//[TestMethod]
		//public async Task VerifyElementPatterns()
		//{
		//	TestCleanupWrapper cleanup;
		//	Grid rootPanel = null;

		//	CalendarHelper.CalendarViewHelper helper = new CalendarHelper.CalendarViewHelper();
		//	helper.PrepareLoadedEvent();
		//	CalendarView cv = ConfiguredTaskAwaitable<> helper.GetCalendarView();

		//	 AutomationClient.AutomationPropertyChangedHandler <
		//		3 >> spAutomationPropertyChangedEventHandler;
		//	Automation.AutomationClient.UIAElementInfo uiaInfo;
		//	uiaInfo.m_Name = "CalendarView";
		//	uiaInfo.m_AutomationID = "CalendarView";
		//	uiaInfo.m_cType = UIA_CalendarControlTypeId;

		//	wrl.ComPtr<IUIAutomationElement> spUIAutomationElement;
		//	wrl.ComPtr<IUIAutomation> spAutomation;
		//	wrl.ComPtr<IUIAutomationElement> spAutomationHeaderElement;
		//	wrl.ComPtr<IUIAutomationSelectionItemPattern> spSelectionItemPattern;
		//	Windows.Foundation.DateTime testdate = ConvertToDateTime(1, 2000, 11, 15);
		//	CreateTestResources(rootPanel);

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.MinDate = ConvertToDateTime(1, 2000, 11, 15);
		//		cv.MaxDate = ConvertToDateTime(1, 2001, 1, 15);
		//		AutomationProperties.SetName(cv, uiaInfo.m_Name);
		//		rootPanel.Children.Add(cv);
		//	});

		//	helper.WaitForLoaded();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		var spAutomationClientManager =
		//			AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
		//		spAutomationClientManager.GetCurrentUIAutomationElement(spUIAutomationElement);
		//		VERIFY_IS_NOT_NULL(spUIAutomationElement);

		//		spAutomationClientManager.GetAutomation(spAutomation);
		//		VERIFY_IS_NOT_NULL(spAutomation);
		//	});

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.SetDisplayDate(ConvertToDateTime(1, 2000, 11, 15));
		//		cv.UpdateLayout();
		//	});

		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationMonthViewSVCondition;
		//		Common.AutoVariant autoMonthViewSV;
		//		autoMonthViewSV.SetString("MonthViewScrollViewer");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoMonthViewSV.Storage()), spUIAutomationMonthViewSVCondition));

		//		wrl.ComPtr<IUIAutomationElement> spAutomationMonthViewSVElement;
		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationMonthViewSVCondition, spAutomationMonthViewSVElement));
		//		VERIFY_IS_NOT_NULL(spAutomationMonthViewSVElement);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDatesCondition;
		//		Common.AutoVariant autoVarType;
		//		autoVarType.SetInt(UIA_DataItemControlTypeId);
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_ControlTypePropertyId,
		//			*(autoVarType.Storage()), spUIAutomationDatesCondition));
		//		wrl.ComPtr<IUIAutomationElement> spAutomationFirstElement;
		//		VERIFY_SUCCEEDED(spAutomationMonthViewSVElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationDatesCondition, spAutomationFirstElement));

		//		wrl.ComPtr<IUIAutomationGridItemPattern> spGridItemPattern;
		//		VERIFY_SUCCEEDED(spAutomationFirstElement.GetCurrentPatternAs(UIA_GridItemPatternId,
		//			__uuidof(IUIAutomationGridItemPattern), spGridItemPattern));
		//		VERIFY_IS_NOT_NULL(spGridItemPattern);

		//		int col = 0, row = 0;
		//		col = VERIFY_SUCCEEDED(spGridItemPattern.CurrentColumn);
		//		WEX.Logging.Log.Comment("2000-11-15 is on Wednesday");
		//		VERIFY_ARE_EQUAL(col, 3);
		//		row = VERIFY_SUCCEEDED(spGridItemPattern.CurrentRow);
		//		VERIFY_ARE_EQUAL(row, 0);

		//		wrl.ComPtr<IUIAutomationTableItemPattern> spTableItemPattern;
		//		VERIFY_SUCCEEDED(spAutomationFirstElement.GetCurrentPatternAs(UIA_TableItemPatternId,
		//			__uuidof(IUIAutomationTableItemPattern), spTableItemPattern));
		//		VERIFY_IS_NOT_NULL(spTableItemPattern);

		//		wrl.ComPtr<IUIAutomationElementArray> spRowHeaderItems;
		//		VERIFY_SUCCEEDED(spTableItemPattern.GetCurrentRowHeaderItems(spRowHeaderItems));

		//		int rowSize = 0;
		//		rowSize = VERIFY_SUCCEEDED(spRowHeaderItems.Length);
		//		VERIFY_ARE_EQUAL(rowSize, 1);

		//		wrl.ComPtr<IUIAutomationElement> spRowHeaderAutomationElement;
		//		VERIFY_SUCCEEDED(spRowHeaderItems.GetElement(0, spRowHeaderAutomationElement));
		//		VERIFY_IS_NOT_NULL(spRowHeaderAutomationElement);

		//		AutoBSTR rowHeaderElementName;
		//		VERIFY_SUCCEEDED(spRowHeaderAutomationElement.get_CurrentName(rowHeaderElementName));
		//		Common.AutoBSTR.VerifyAreEqual("\u200eNovember\u200e \u200e2000", rowHeaderElementName);

		//		wrl.ComPtr<IUIAutomationElementArray> spHeaderItems;
		//		VERIFY_SUCCEEDED(spTableItemPattern.GetCurrentColumnHeaderItems(spHeaderItems));

		//		int size = 0;
		//		size = VERIFY_SUCCEEDED(spHeaderItems.Length);
		//		VERIFY_ARE_EQUAL(size, 1);

		//		wrl.ComPtr<IUIAutomationElement> spAutomationHeaderElement;
		//		VERIFY_SUCCEEDED(spHeaderItems.GetElement(0, spAutomationHeaderElement));
		//		VERIFY_IS_NOT_NULL(spAutomationHeaderElement);

		//		AutoBSTR elementName;
		//		VERIFY_SUCCEEDED(spAutomationHeaderElement.get_CurrentName(elementName));
		//		WEX.Logging.Log.Comment("2000-11-15 is on Wednesday");
		//		VERIFY_ARE_EQUAL(wcscmp(elementName, "Wednesday"), 0);

		//		VERIFY_SUCCEEDED(spAutomationFirstElement.GetCurrentPatternAs(UIA_SelectionItemPatternId,
		//			__uuidof(IUIAutomationSelectionItemPattern), spSelectionItemPattern));
		//		VERIFY_IS_NOT_NULL(spSelectionItemPattern);

		//		BOOL isSelected = true;
		//		isSelected = VERIFY_SUCCEEDED(spSelectionItemPattern.CurrentIsSelected);
		//		VERIFY_IS_false(!!isSelected);
		//	});

		//	helper.PrepareSelectedDatesChangedEvent();
		//	helper.ExpectAddedDate(testdate);
		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		VERIFY_SUCCEEDED(spSelectionItemPattern.Select());
		//	});
		//	helper.WaitForSelectedDatesChanged();


		//	helper.PrepareSelectedDatesChangedEvent();
		//	helper.ExpectRemovedDate(testdate);
		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		VERIFY_SUCCEEDED(spSelectionItemPattern.RemoveFromSelection());
		//	});
		//	helper.WaitForSelectedDatesChanged();

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.SelectedDates.Add(testdate);
		//	});

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		BOOL isSelected = true;
		//		isSelected = VERIFY_SUCCEEDED(spSelectionItemPattern.CurrentIsSelected);
		//		VERIFY_IS_true(!!isSelected);
		//	});
		//}


		//[TestMethod]
		//public async Task VerifySelectionChangedEvent()
		//{
		//	TestCleanupWrapper cleanup;
		//	Grid rootPanel = null;

		//	CalendarHelper.CalendarViewHelper helper = new CalendarViewHelper();
		//	helper.PrepareLoadedEvent();
		//	CalendarView cv = await helper.GetCalendarView();

		//	 AutomationClient.AutomationPropertyChangedHandler <
		//		3 >> spAutomationPropertyChangedEventHandler;
		//	Automation.AutomationClient.UIAElementInfo uiaInfo;
		//	uiaInfo.m_Name = "CalendarView";
		//	uiaInfo.m_AutomationID = "CalendarView";
		//	uiaInfo.m_cType = UIA_CalendarControlTypeId;

		//	wrl.ComPtr<IUIAutomationElement> spUIAutomationElement;
		//	wrl.ComPtr<IUIAutomation> spAutomation;
		//	wrl.ComPtr<IUIAutomationElement> spAutomationHeaderElement;
		//	wrl.ComPtr<IUIAutomationSelectionItemPattern> spSelectionItemPattern;
		//	CreateTestResources(rootPanel);

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.MinDate = ConvertToDateTime(1, 2000, 11, 15);
		//		cv.MaxDate = ConvertToDateTime(1, 2001, 1, 15);
		//		AutomationProperties.SetName(cv, uiaInfo.m_Name);
		//		rootPanel.Children.Add(cv);
		//	});

		//	helper.WaitForLoaded();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		cv.SetDisplayDate(ConvertToDateTime(1, 2000, 11, 15));
		//		cv.UpdateLayout();
		//	});

		//	await TestServices.WindowHelper.WaitForIdle();

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		var spAutomationClientManager =
		//			AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
		//		spAutomationClientManager.GetCurrentUIAutomationElement(spUIAutomationElement);
		//		VERIFY_IS_NOT_NULL(spUIAutomationElement);

		//		spAutomationClientManager.GetAutomation(spAutomation);
		//		VERIFY_IS_NOT_NULL(spAutomation);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationMonthViewSVCondition;
		//		Common.AutoVariant autoMonthViewSV;
		//		autoMonthViewSV.SetString("MonthViewScrollViewer");
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoMonthViewSV.Storage()), spUIAutomationMonthViewSVCondition));

		//		wrl.ComPtr<IUIAutomationElement> spAutomationMonthViewSVElement;
		//		VERIFY_SUCCEEDED(spUIAutomationElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationMonthViewSVCondition, spAutomationMonthViewSVElement));
		//		VERIFY_IS_NOT_NULL(spAutomationMonthViewSVElement);

		//		wrl.ComPtr<IUIAutomationCondition> spUIAutomationDatesCondition;
		//		Common.AutoVariant autoVarType;
		//		autoVarType.SetInt(UIA_DataItemControlTypeId);
		//		VERIFY_SUCCEEDED(spAutomation.CreatePropertyCondition(UIA_ControlTypePropertyId,
		//			*(autoVarType.Storage()), spUIAutomationDatesCondition));
		//		wrl.ComPtr<IUIAutomationElement> spAutomationFirstElement;
		//		VERIFY_SUCCEEDED(spAutomationMonthViewSVElement.FindFirst(TreeScope.TreeScope_Children,
		//			spUIAutomationDatesCondition, spAutomationFirstElement));

		//		wrl.ComPtr<IUIAutomationSelectionItemPattern> spSelectionItemPattern;
		//		VERIFY_SUCCEEDED(spAutomationFirstElement.GetCurrentPatternAs(UIA_SelectionItemPatternId,
		//			__uuidof(IUIAutomationSelectionItemPattern), spSelectionItemPattern));
		//		VERIFY_IS_NOT_NULL(spSelectionItemPattern);

		//		await TestServices.RunOnUIThread(() =>
		//		{
		//			cv.SelectionMode = CalendarViewSelectionMode.Multiple;
		//		});
		//		var spEvent = new Event();
		//		wrl.ComPtr<Patterns.SelectionItemPatternHandler> spAutomationPatternHandler;

		//		spAutomationPatternHandler.Attach(new Patterns.SelectionItemPatternHandler(spAutomationClientManager,
		//			spEvent, TreeScope_Subtree, UIA_SelectionItem_ElementSelectedEventId));
		//		spAutomationPatternHandler.AttachEventHandler();
		//		Windows.Foundation.DateTime testdate = ConvertToDateTime(1, 2000, 11, 15);
		//		await TestServices.RunOnUIThread(() =>
		//		{
		//			WEX.Logging.Log.Comment("Selecting Date 2000-11-15");
		//			cv.SelectedDates.Add(testdate);
		//		});
		//		spAutomationPatternHandler.ConfirmAndUnregister();
		//		WEX.Logging.Log.Comment("UIA_SelectionItem_ElementSelectedEventId Recieved");

		//		spAutomationPatternHandler = null;

		//		spAutomationPatternHandler.Attach(new Patterns.SelectionItemPatternHandler(spAutomationClientManager,
		//			spEvent, TreeScope_Subtree, UIA_SelectionItem_ElementAddedToSelectionEventId));
		//		spAutomationPatternHandler.AttachEventHandler();
		//		await TestServices.RunOnUIThread(() =>
		//		{
		//			testdate = ConvertToDateTime(1, 2000, 11, 16);
		//			WEX.Logging.Log.Comment("Selecting Date 2000-11-16");
		//			cv.SelectedDates.Add(testdate);
		//		});
		//		spAutomationPatternHandler.ConfirmAndUnregister();
		//		WEX.Logging.Log.Comment("UIA_SelectionItem_ElementAddedToSelectionEventId Recieved");

		//		spAutomationPatternHandler = null;

		//		spAutomationPatternHandler.Attach(new Patterns.SelectionItemPatternHandler(spAutomationClientManager,
		//			spEvent, TreeScope_Subtree, UIA_SelectionItem_ElementRemovedFromSelectionEventId));
		//		spAutomationPatternHandler.AttachEventHandler();
		//		await TestServices.RunOnUIThread(() =>
		//		{
		//			WEX.Logging.Log.Comment("Removed one selection");
		//			cv.SelectedDates.RemoveAt(0);
		//		});
		//		spAutomationPatternHandler.ConfirmAndUnregister();
		//		WEX.Logging.Log.Comment("UIA_SelectionItem_ElementRemovedFromSelectionEventId Recieved");


		//		spAutomationPatternHandler = null;

		//		spAutomationPatternHandler.Attach(new Patterns.SelectionItemPatternHandler(spAutomationClientManager,
		//			spEvent, TreeScope_Subtree, UIA_SelectionItem_ElementRemovedFromSelectionEventId));
		//		spAutomationPatternHandler.AttachEventHandler();
		//		await TestServices.RunOnUIThread(() =>
		//		{
		//			WEX.Logging.Log.Comment("Removed last selection");
		//			cv.SelectedDates.RemoveAt(0);
		//		});
		//		spAutomationPatternHandler.ConfirmAndUnregister();
		//		WEX.Logging.Log.Comment("UIA_SelectionItem_ElementRemovedFromSelectionEventId Recieved");
		//	});
		//}

		[TestMethod]
		[Ignore("Test using AutomationPeers not implemented yet on Uno")]
		public async Task VerifyDayItemRowHeaders()
		{
			TestCleanupWrapper cleanup;
			CalendarView calendar = default;

			await TestServices.RunOnUIThread(() =>
			{
				calendar = new CalendarView();
				calendar.SetDisplayDate(ConvertToDateTime(1, 2016, 04, 01));
				TestServices.WindowHelper.WindowContent = calendar;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.RunOnUIThread(() =>
			{
				var dayItems = new List<CalendarViewDayItem>();
				TreeHelper.GetVisualChildrenByType(calendar, ref dayItems);

				// So since there is no mapping APIs on CalendarView, to get to the month in view, we divide by 2
				// To get to the previous/next months, we remove/add 30 days.
				var marchDay = dayItems.GetAt((int)(dayItems.Count * 0.5 - 30));
				var aprilDay = dayItems.GetAt((int)(dayItems.Count * 0.5));
				var mayDay = dayItems.GetAt((int)(dayItems.Count * 0.5 + 30));

				// We want to validate we get different peers for day items that belong to different months.
				var marchDayPeer = FrameworkElementAutomationPeer.CreatePeerForElement(marchDay);
				var aprilDayPeer = FrameworkElementAutomationPeer.CreatePeerForElement(aprilDay);
				var mayDayPeer = FrameworkElementAutomationPeer.CreatePeerForElement(mayDay);

				var marchRowHeaders = ((ITableItemProvider)marchDayPeer.GetPattern(
					PatternInterface.TableItem)).GetRowHeaderItems();
				var aprilRowHeaders = ((ITableItemProvider)aprilDayPeer.GetPattern(
					PatternInterface.TableItem)).GetRowHeaderItems();
				var mayRowHeaders = ((ITableItemProvider)mayDayPeer.GetPattern(
					PatternInterface.TableItem)).GetRowHeaderItems();

				VERIFY_ARE_EQUAL(1, marchRowHeaders.Length);
				VERIFY_ARE_EQUAL(1, aprilRowHeaders.Length);
				VERIFY_ARE_EQUAL(1, mayRowHeaders.Length);

				var bridge = new BridgeAutomationPeer();
				VERIFY_ARE_EQUAL("\u200eMarch\u200e \u200e2016",
					bridge.CallPeerFromProvider(marchRowHeaders[0]).GetName());
				VERIFY_ARE_EQUAL("\u200eApril\u200e \u200e2016",
					bridge.CallPeerFromProvider(aprilRowHeaders[0]).GetName());
				VERIFY_ARE_EQUAL("\u200eMay\u200e \u200e2016",
					bridge.CallPeerFromProvider(mayRowHeaders[0]).GetName());
			});
		}

		//[TestMethod]
		//public async Task VerifyOutOfViewItemsDoNotSupportGridItemPattern()
		//{
		//	TestCleanupWrapper cleanup;

		//	Automation.AutomationClient.UIAElementInfo uiaInfo;
		//	uiaInfo.m_Name = "CalendarView";
		//	uiaInfo.m_AutomationID = "CalendarView";
		//	uiaInfo.m_cType = UIA_CalendarControlTypeId;

		//	CalendarView calendarView = null;

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		calendarView = new CalendarView();
		//		calendarView.SetDisplayDate(ConvertToDateTime(1, 2016, 04, 01));

		//		AutomationProperties.SetName(calendarView, uiaInfo.m_Name);

		//		TestServices.WindowHelper.WindowContent = calendarView;
		//	});
		//	await TestServices.WindowHelper.WaitForIdle();

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		calendarView.Focus(FocusState.Keyboard);
		//	});
		//	await TestServices.WindowHelper.WaitForIdle();

		//	LOG_OUTPUT("Get the automation element corresponding to the first visible day item.");
		//	wrl.ComPtr<IUIAutomationElement> monthScrollViewerChildElement;
		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		wrl.ComPtr<IUIAutomationElement> automationElement;

		//		var automationClientManager =
		//			AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
		//		automationClientManager.GetCurrentUIAutomationElement(automationElement);

		//		wrl.ComPtr<IUIAutomation> automation;
		//		automationClientManager.GetAutomation(automation);

		//		wrl.ComPtr<IUIAutomationCondition> monthViewScrollViewerCondition;
		//		Common.AutoVariant autoMonthViewSV;
		//		autoMonthViewSV.SetString("MonthViewScrollViewer");
		//		LogThrow_IfFailed(automation.CreatePropertyCondition(UIA_AutomationIdPropertyId,
		//			*(autoMonthViewSV.Storage()), monthViewScrollViewerCondition));

		//		wrl.ComPtr<IUIAutomationElement> monthViewScrollViewerElement;
		//		LogThrow_IfFailed(automationElement.FindFirst(TreeScope.TreeScope_Children,
		//			monthViewScrollViewerCondition, monthViewScrollViewerElement));

		//		wrl.ComPtr<IUIAutomationCondition> trueCondition;
		//		LogThrow_IfFailed(automation.CreateTrueCondition(trueCondition));
		//		LogThrow_IfFailed(monthViewScrollViewerElement.FindFirst(TreeScope.TreeScope_Children, trueCondition,
		//			&monthScrollViewerChildElement));
		//	});

		//	LOG_OUTPUT("Tab past the month header button.");
		//	TestServices.KeyboardHelper.Tab();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	LOG_OUTPUT("Tab past the previous month button.");
		//	TestServices.KeyboardHelper.Tab();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	LOG_OUTPUT("Tab past the next month button.");
		//	TestServices.KeyboardHelper.Tab();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	LOG_OUTPUT(
		//		"Focus is now on a day item. Press PageDown to change the month and move our previously queried automation element out of view.");
		//	TestServices.KeyboardHelper.PageDown();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	LOG_OUTPUT(
		//		"Attempt to query for the grid item pattern on our previously queried automation element, which should return null.");
		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		wrl.ComPtr<IUIAutomationGridItemPattern> gridItemPattern;
		//		LogThrow_IfFailed(monthScrollViewerChildElement.GetCurrentPatternAs(UIA_GridItemPatternId,
		//			__uuidof(IUIAutomationGridItemPattern), gridItemPattern));

		//		VERIFY_IS_null(gridItemPattern);
		//	});
		//}

		//[TestMethod]
		//public async Task VerifyAutomationNotificationEventAfterClickingNavigationButton()
		//{
		//	TestCleanupWrapper cleanup;

		//	Automation.AutomationClient.UIAElementInfo uiaInfo;
		//	uiaInfo.m_Name = "CalendarView";
		//	uiaInfo.m_AutomationID = "CalendarView";
		//	uiaInfo.m_cType = UIA_CalendarControlTypeId;

		//	CalendarView calendarView = null;
		//	Button previousButton = null;
		//	Button nextButton = null;

		//	var loadedEvent = new Event();
		//	var previousButtonClickedEvent = new Event();
		//	var nextButtonClickedEvent = new Event();

		//	var loadedRegistration = CreateSafeEventRegistration<CalendarView>("Loaded");
		//	var previousButtonClickRegistration = CreateSafeEventRegistration<Button>("Click");
		//	var nextButtonClickRegistration = CreateSafeEventRegistration<Button>("Click");

		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		calendarView = new CalendarView();
		//		calendarView.SetDisplayDate(ConvertToDateTime(1 /* era */, 2016 /* year */, 04 /* month */, 01 /* day */));

		//		loadedRegistration.Attach(calendarView, () =>
		//		{
		//			LOG_OUTPUT("calendarView raised Loaded event.");
		//			loadedEvent.Set();
		//		});

		//		AutomationProperties.SetName(calendarView, uiaInfo.m_Name);

		//		TestServices.WindowHelper.WindowContent = calendarView;
		//	});
		//	loadedEvent.WaitForDefault();
		//	await TestServices.WindowHelper.WaitForIdle();

		//	// find the template parts
		//	await TestServices.RunOnUIThread(() =>
		//	{
		//		previousButton = Button(TreeHelper.GetVisualChildByName(calendarView, "PreviousButton"));
		//		nextButton = Button(TreeHelper.GetVisualChildByName(calendarView, "NextButton"));

		//		previousButtonClickRegistration.Attach(previousButton, () =>
		//		{
		//			LOG_OUTPUT("previousButton raised Click event.");
		//			previousButtonClickedEvent.Set();
		//		});

		//		nextButtonClickRegistration.Attach(nextButton, () =>
		//		{
		//			LOG_OUTPUT("nextButton raised Click event.");
		//			nextButtonClickedEvent.Set();
		//		});
		//	});

		//	// How to use and init AutomationNotificationHandler, please refer to the source code of following two files:
		//	// AutomationClient\AutomationEventHandler.h
		//	// events\NotificationEventTests.cpp
		//	wrl.ComPtr<AutomationClient.AutomationNotificationHandler> notificationEventHandler;

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		var notifyEvent = new Event();
		//		var automationClientManager =
		//			AutomationClient.AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
		//		notificationEventHandler.Attach(
		//			new AutomationClient.AutomationNotificationHandler(automationClientManager, notifyEvent,
		//				TreeScope_Subtree));
		//		notificationEventHandler.Init(
		//			NotificationKind_ActionCompleted,
		//			NotificationProcessing_MostRecent,
		//			null, // ignore expectedDisplayString
		//			"CalenderViewNavigationButtonCompleted");
		//		notificationEventHandler.AttachEventHandler();
		//	});

		//	LOG_OUTPUT("Left-mouse-click on previousButton.");
		//	TestServices.InputHelper.LeftMouseClick(previousButton);
		//	previousButtonClickedEvent.WaitForDefault();
		//	await TestServices.WindowHelper.WaitForIdle();
		//	// WaitForIdle is not reliable on this test case. and Confirm would block the UI thread. SynchronouslyTickUIThread to give more time to make animation complete.
		//	await TestServices.WindowHelper.SynchronouslyTickUIThread(3);

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		notificationEventHandler.Confirm();
		//	});

		//	LOG_OUTPUT("Left-mouse-click on nextButton.");
		//	TestServices.InputHelper.LeftMouseClick(nextButton);
		//	nextButtonClickedEvent.WaitForDefault();
		//	await TestServices.WindowHelper.WaitForIdle();
		//	await TestServices.WindowHelper.SynchronouslyTickUIThread(3);

		//	UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		//	{
		//		notificationEventHandler.ConfirmAndUnregister();
		//	});
		//}
	}
}
