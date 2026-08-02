#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_MobileAccessibilityCapabilityMatrix
{
	#region Types

	private enum MatrixClass
	{
		Direct,
		DirectDerived,    // Direct/Derived in matrix
		DirectCustom,     // Direct/Custom in matrix
		Derived,
		DerivedCustom,    // Derived/Custom in matrix
		Custom,
		CustomInternal,   // Custom/Internal in matrix
		InternalCustom,   // Internal/Custom in matrix
		Internal,
		Unsupported,
	}

	private enum PatternCoverage
	{
		Range,           // AccessibilityNativeRangeDetails
		TextState,       // AccessibilityNativeTextStateDetails
		Scroll,          // AccessibilityNativeScrollDetails
		Collection,      // AccessibilityNativeCollectionDetails
		CollectionItem,  // AccessibilityNativeCollectionItemDetails
		Actions,         // AccessibilityNativeNodeDetails.SupportedActions
		SnapshotField,   // top-level snapshot field (Heading / Checkable / Password)
		FallbackContent, // hint / value / custom-content channel; no dedicated sub-object
		Internal,        // Internal or Unsupported -- no native dispatch required
	}

	private sealed record PropertyRow(AutomationProperty Property, string Label, MatrixClass Class);
	private sealed record PatternRow(PatternInterface Pattern, MatrixClass Class, PatternCoverage Coverage);
	private sealed record EventRow(AutomationEvents Event, MatrixClass Class);
	private sealed record RelationRow(string Name, MatrixClass Class, bool NoDanglingTarget);
	private sealed record StateGroupRow(PatternInterface Pattern, string StateGroup);

	#endregion

	#region Tables

	private static readonly PropertyRow[] s_propertyRows =
	[
		// Direct
		new(AutomationElementIdentifiers.BoundingRectangleProperty,    "BoundingRectangle",    MatrixClass.Direct),
		new(AutomationElementIdentifiers.HelpTextProperty,             "HelpText",             MatrixClass.Direct),
		new(AutomationElementIdentifiers.IsEnabledProperty,            "IsEnabled",            MatrixClass.Direct),
		new(AutomationElementIdentifiers.IsPasswordProperty,           "IsPassword",           MatrixClass.Direct),
		new(AutomationElementIdentifiers.NameProperty,                 "Name",                 MatrixClass.Direct),
		new(AutomationElementIdentifiers.AutomationIdProperty,         "AutomationId",         MatrixClass.Direct),
		new(AutomationElementIdentifiers.ClickablePointProperty,       "ClickablePoint",       MatrixClass.Direct),
		new(AutomationElementIdentifiers.CultureProperty,              "Culture",              MatrixClass.Direct),
		// DirectDerived
		new(AutomationElementIdentifiers.HasKeyboardFocusProperty,     "HasKeyboardFocus",     MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.IsOffscreenProperty,          "IsOffscreen",          MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.IsRequiredForFormProperty,    "IsRequiredForForm",    MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.LabeledByProperty,            "LabeledBy",            MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.LiveSettingProperty,          "LiveSetting",          MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.OrientationProperty,          "Orientation",          MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.ClassNameProperty,            "ClassName",            MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.PositionInSetProperty,        "PositionInSet",        MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.SizeOfSetProperty,            "SizeOfSet",            MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.HeadingLevelProperty,         "HeadingLevel",         MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.IsDialogProperty,             "IsDialog",             MatrixClass.DirectDerived),
		new(AutomationElementIdentifiers.IsDataValidForFormProperty,   "IsDataValidForForm",   MatrixClass.DirectDerived),
		// Derived
		new(AutomationElementIdentifiers.IsContentElementProperty,     "IsContentElement",     MatrixClass.Derived),
		new(AutomationElementIdentifiers.IsControlElementProperty,     "IsControlElement",     MatrixClass.Derived),
		new(AutomationElementIdentifiers.IsKeyboardFocusableProperty,  "IsKeyboardFocusable",  MatrixClass.Derived),
		new(AutomationElementIdentifiers.ItemStatusProperty,           "ItemStatus",           MatrixClass.Derived),
		new(AutomationElementIdentifiers.ItemTypeProperty,             "ItemType",             MatrixClass.Derived),
		new(AutomationElementIdentifiers.LocalizedControlTypeProperty, "LocalizedControlType", MatrixClass.Derived),
		new(AutomationElementIdentifiers.ControlTypeProperty,          "ControlType",          MatrixClass.Derived),
		new(AutomationElementIdentifiers.LevelProperty,                "Level",                MatrixClass.Derived),
		new(AutomationElementIdentifiers.LandmarkTypeProperty,         "LandmarkType",         MatrixClass.Derived),
		new(AutomationElementIdentifiers.LocalizedLandmarkTypeProperty, "LocalizedLandmarkType", MatrixClass.Derived),
		new(AutomationElementIdentifiers.DescribedByProperty,          "DescribedBy",          MatrixClass.Derived),
		new(AutomationElementIdentifiers.FlowsFromProperty,            "FlowsFrom",            MatrixClass.Derived),
		new(AutomationElementIdentifiers.FlowsToProperty,              "FlowsTo",              MatrixClass.Derived),
		new(AutomationElementIdentifiers.FullDescriptionProperty,      "FullDescription",      MatrixClass.Derived),
		// DerivedCustom
		new(AutomationElementIdentifiers.AnnotationsProperty,          "Annotations",          MatrixClass.DerivedCustom),
		// InternalCustom
		new(AutomationElementIdentifiers.AcceleratorKeyProperty,       "AcceleratorKey",       MatrixClass.InternalCustom),
		new(AutomationElementIdentifiers.AccessKeyProperty,            "AccessKey",            MatrixClass.InternalCustom),
		new(AutomationElementIdentifiers.ControlledPeersProperty,      "ControlledPeers",      MatrixClass.InternalCustom),
		// Internal
		new(AutomationElementIdentifiers.IsPeripheralProperty,         "IsPeripheral",         MatrixClass.Internal),
	];

	private static readonly PatternRow[] s_patternRows =
	[
		// Direct
		new(PatternInterface.Invoke,           MatrixClass.Direct,       PatternCoverage.Actions),
		new(PatternInterface.RangeValue,       MatrixClass.Direct,       PatternCoverage.Range),
		new(PatternInterface.Scroll,           MatrixClass.Direct,       PatternCoverage.Scroll),
		new(PatternInterface.SelectionItem,    MatrixClass.Direct,       PatternCoverage.Actions),
		new(PatternInterface.Toggle,           MatrixClass.Direct,       PatternCoverage.SnapshotField),
		// DirectDerived
		new(PatternInterface.Selection,        MatrixClass.DirectDerived, PatternCoverage.Collection),
		new(PatternInterface.Value,            MatrixClass.DirectDerived, PatternCoverage.TextState),
		new(PatternInterface.ScrollItem,       MatrixClass.DirectDerived, PatternCoverage.Actions),
		new(PatternInterface.Grid,             MatrixClass.DirectDerived, PatternCoverage.Collection),
		new(PatternInterface.GridItem,         MatrixClass.DirectDerived, PatternCoverage.CollectionItem),
		new(PatternInterface.Window,           MatrixClass.DirectDerived, PatternCoverage.Actions),
		new(PatternInterface.Table,            MatrixClass.DirectDerived, PatternCoverage.Collection),
		new(PatternInterface.TableItem,        MatrixClass.DirectDerived, PatternCoverage.CollectionItem),
		new(PatternInterface.Text,             MatrixClass.DirectDerived, PatternCoverage.TextState),
		new(PatternInterface.TextEdit,         MatrixClass.DirectDerived, PatternCoverage.TextState),
		// DirectCustom
		new(PatternInterface.ExpandCollapse,   MatrixClass.DirectCustom,  PatternCoverage.Actions),
		new(PatternInterface.Drag,             MatrixClass.InternalCustom, PatternCoverage.FallbackContent),
		new(PatternInterface.DropTarget,       MatrixClass.InternalCustom, PatternCoverage.FallbackContent),
		// Custom
		new(PatternInterface.MultipleView,     MatrixClass.Custom,        PatternCoverage.Actions),
		new(PatternInterface.Transform,        MatrixClass.Custom,        PatternCoverage.Actions),
		new(PatternInterface.Transform2,       MatrixClass.Custom,        PatternCoverage.Actions),
		// CustomInternal
		new(PatternInterface.Dock,             MatrixClass.CustomInternal, PatternCoverage.Actions),
		// Derived
		new(PatternInterface.VirtualizedItem,  MatrixClass.Derived,       PatternCoverage.Actions),
		new(PatternInterface.Text2,            MatrixClass.Derived,       PatternCoverage.TextState),
		new(PatternInterface.TextChild,        MatrixClass.Derived,       PatternCoverage.FallbackContent),
		new(PatternInterface.TextRange,        MatrixClass.Derived,       PatternCoverage.FallbackContent),
		new(PatternInterface.Spreadsheet,      MatrixClass.Derived,       PatternCoverage.Collection),
		new(PatternInterface.SpreadsheetItem,  MatrixClass.Derived,       PatternCoverage.CollectionItem),
		new(PatternInterface.Styles,           MatrixClass.Derived,       PatternCoverage.FallbackContent),
		// DerivedCustom
		new(PatternInterface.Annotation,       MatrixClass.DerivedCustom, PatternCoverage.FallbackContent),
		new(PatternInterface.CustomNavigation, MatrixClass.DerivedCustom, PatternCoverage.FallbackContent),
		// Internal
		new(PatternInterface.ItemContainer,    MatrixClass.Internal,      PatternCoverage.Internal),
		// Unsupported
		new(PatternInterface.ObjectModel,      MatrixClass.Unsupported,   PatternCoverage.Internal),
		new(PatternInterface.SynchronizedInput, MatrixClass.Unsupported,  PatternCoverage.Internal),
	];

	private static readonly EventRow[] s_eventRows =
	[
		new(AutomationEvents.ToolTipOpened,                                    MatrixClass.Derived),
		new(AutomationEvents.ToolTipClosed,                                    MatrixClass.Derived),
		new(AutomationEvents.MenuOpened,                                       MatrixClass.DirectDerived),
		new(AutomationEvents.MenuClosed,                                       MatrixClass.DirectDerived),
		new(AutomationEvents.AutomationFocusChanged,                           MatrixClass.Direct),
		new(AutomationEvents.InvokePatternOnInvoked,                           MatrixClass.DirectDerived),
		new(AutomationEvents.SelectionItemPatternOnElementAddedToSelection,    MatrixClass.Direct),
		new(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection, MatrixClass.Direct),
		new(AutomationEvents.SelectionItemPatternOnElementSelected,            MatrixClass.Direct),
		new(AutomationEvents.SelectionPatternOnInvalidated,                    MatrixClass.DirectDerived),
		new(AutomationEvents.TextPatternOnTextSelectionChanged,                MatrixClass.DirectDerived),
		new(AutomationEvents.TextPatternOnTextChanged,                         MatrixClass.DirectDerived),
		new(AutomationEvents.AsyncContentLoaded,                               MatrixClass.Derived),
		new(AutomationEvents.PropertyChanged,                                  MatrixClass.DirectDerived),
		new(AutomationEvents.StructureChanged,                                 MatrixClass.Direct),
		new(AutomationEvents.DragStart,                                        MatrixClass.InternalCustom),
		new(AutomationEvents.DragCancel,                                       MatrixClass.InternalCustom),
		new(AutomationEvents.DragComplete,                                     MatrixClass.InternalCustom),
		new(AutomationEvents.DragEnter,                                        MatrixClass.InternalCustom),
		new(AutomationEvents.DragLeave,                                        MatrixClass.InternalCustom),
		new(AutomationEvents.Dropped,                                          MatrixClass.InternalCustom),
		new(AutomationEvents.LiveRegionChanged,                                MatrixClass.Direct),
		new(AutomationEvents.InputReachedTarget,                               MatrixClass.Unsupported),
		new(AutomationEvents.InputReachedOtherElement,                         MatrixClass.Unsupported),
		new(AutomationEvents.InputDiscarded,                                   MatrixClass.Unsupported),
		new(AutomationEvents.WindowClosed,                                     MatrixClass.Direct),
		new(AutomationEvents.WindowOpened,                                     MatrixClass.Direct),
		new(AutomationEvents.ConversionTargetChanged,                          MatrixClass.Derived),
		new(AutomationEvents.TextEditTextChanged,                              MatrixClass.DirectDerived),
		new(AutomationEvents.LayoutInvalidated,                                MatrixClass.Direct),
	];

	private static readonly RelationRow[] s_relationRows =
	[
		new("LabeledBy",         MatrixClass.DirectDerived, NoDanglingTarget: true),
		new("ControlledPeers",   MatrixClass.InternalCustom, NoDanglingTarget: false),
		new("DescribedBy",       MatrixClass.Derived,        NoDanglingTarget: false),
		new("FlowsTo/FlowsFrom", MatrixClass.Derived,        NoDanglingTarget: true),
		new("Annotations",       MatrixClass.DerivedCustom,  NoDanglingTarget: false),
	];

	private static readonly StateGroupRow[] s_stateGroupRows =
	[
		new(PatternInterface.Toggle,          "ToggleState"),
		new(PatternInterface.Value,           "Value, IsReadOnly"),
		new(PatternInterface.RangeValue,      "Value, Minimum, Maximum, SmallChange, LargeChange, IsReadOnly"),
		new(PatternInterface.ExpandCollapse,  "ExpandCollapseState"),
		new(PatternInterface.Selection,       "CanSelectMultiple, IsSelectionRequired, Selection"),
		new(PatternInterface.SelectionItem,   "IsSelected, SelectionContainer"),
		new(PatternInterface.Scroll,          "HorizontalScrollPercent, VerticalScrollPercent, ViewSize, Scrollable"),
		new(PatternInterface.Grid,            "RowCount, ColumnCount"),
		new(PatternInterface.GridItem,        "Row, Column, RowSpan, ColumnSpan, ContainingGrid"),
		new(PatternInterface.MultipleView,    "CurrentView, SupportedViews, ViewName"),
		new(PatternInterface.Window,          "CanMaximize, CanMinimize, IsModal, IsTopmost"),
		new(PatternInterface.Dock,            "DockPosition"),
		new(PatternInterface.Table,           "RowOrColumnMajor, RowHeaders, ColumnHeaders"),
		new(PatternInterface.TableItem,       "RowHeaderItems, ColumnHeaderItems"),
		new(PatternInterface.Transform,       "CanMove, CanResize, CanRotate"),
		new(PatternInterface.Text,            "DocumentRange, Selection, ActivePosition"),
		new(PatternInterface.Annotation,      "Type, TypeName, Author, Date, Target"),
		new(PatternInterface.Drag,            "IsGrabbed, DropEffects, GrabbedItems"),
		new(PatternInterface.DropTarget,      "DropTargetEffects"),
		new(PatternInterface.SpreadsheetItem, "Formula, Annotations"),
		new(PatternInterface.Styles,          "StyleId, StyleName, FillColor, Shape"),
		new(PatternInterface.Transform2,      "CanZoom, ZoomLevel, MinZoom, MaxZoom"),
	];

	#endregion

	#region T070: Matrix completeness and coverage

	[TestMethod]
	public void T070_PatternInterface_Table_Covers_All_34_Enum_Values()
	{
		var allValues = Enum.GetValues<PatternInterface>();
		Assert.AreEqual(34, allValues.Length,
			"PatternInterface enum has grown; add the new value(s) to s_patternRows and update this count.");
		var covered = s_patternRows.Select(r => r.Pattern).ToHashSet();
		var missing = allValues.Except(covered).ToList();
		Assert.AreEqual(0, missing.Count,
			$"PatternInterface values absent from matrix table: {string.Join(", ", missing)}");
	}

	[TestMethod]
	public void T070_PatternInterface_Table_Has_No_Duplicates()
	{
		var dups = s_patternRows.GroupBy(r => r.Pattern).Where(g => g.Count() > 1).ToList();
		Assert.AreEqual(0, dups.Count,
			$"Duplicate pattern rows: {string.Join(", ", dups.Select(g => g.Key))}");
	}

	[TestMethod]
	public void T070_AutomationEvents_Table_Covers_All_30_Enum_Values()
	{
		var allValues = Enum.GetValues<AutomationEvents>();
		Assert.AreEqual(30, allValues.Length,
			"AutomationEvents enum has grown; add the new value(s) to s_eventRows and update this count.");
		var covered = s_eventRows.Select(r => r.Event).ToHashSet();
		var missing = allValues.Except(covered).ToList();
		Assert.AreEqual(0, missing.Count,
			$"AutomationEvents values absent from matrix table: {string.Join(", ", missing)}");
	}

	[TestMethod]
	public void T070_AutomationEvents_Table_Has_No_Duplicates()
	{
		var dups = s_eventRows.GroupBy(r => r.Event).Where(g => g.Count() > 1).ToList();
		Assert.AreEqual(0, dups.Count,
			$"Duplicate event rows: {string.Join(", ", dups.Select(g => g.Key))}");
	}

	[TestMethod]
	public void T070_Properties_Table_Has_39_Rows_And_No_Duplicates()
	{
		Assert.AreEqual(39, s_propertyRows.Length,
			"Property table must have exactly 39 rows (one per AutomationElementIdentifiers property).");
		// AutomationProperty uses reference equality; each property is a static singleton.
		var seen = new HashSet<AutomationProperty>(ReferenceEqualityComparer.Instance);
		var dups = s_propertyRows.Where(r => !seen.Add(r.Property)).Select(r => r.Label).ToList();
		Assert.AreEqual(0, dups.Count,
			$"Duplicate property rows: {string.Join(", ", dups)}");

		var declaredProperties = typeof(AutomationElementIdentifiers)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(property => property.PropertyType == typeof(AutomationProperty))
			.Select(property => (AutomationProperty)property.GetValue(null)!)
			.ToHashSet(ReferenceEqualityComparer.Instance);
		var coveredProperties = s_propertyRows
			.Select(row => row.Property)
			.ToHashSet(ReferenceEqualityComparer.Instance);

		Assert.AreEqual(
			0,
			declaredProperties.Except(coveredProperties).Count(),
			"Every AutomationElementIdentifiers property must have a capability row.");
		Assert.AreEqual(
			0,
			coveredProperties.Except(declaredProperties).Count(),
			"The capability table must not contain properties outside AutomationElementIdentifiers.");
	}

	[TestMethod]
	public void T070_Properties_Table_Covers_Representative_AutomationElementIdentifiers()
	{
		var tableSet = s_propertyRows
			.Select(r => r.Property)
			.ToHashSet(ReferenceEqualityComparer.Instance);

		// One property from each classification bucket, plus all edge-case rows.
		AutomationProperty[] required =
		[
			AutomationElementIdentifiers.BoundingRectangleProperty,  // Direct
			AutomationElementIdentifiers.NameProperty,               // Direct
			AutomationElementIdentifiers.IsEnabledProperty,          // Direct
			AutomationElementIdentifiers.IsPasswordProperty,         // Direct -- redaction contract
			AutomationElementIdentifiers.AutomationIdProperty,       // Direct -- never spoken name
			AutomationElementIdentifiers.CultureProperty,            // Direct
			AutomationElementIdentifiers.HasKeyboardFocusProperty,   // DirectDerived
			AutomationElementIdentifiers.HeadingLevelProperty,       // DirectDerived -- exact level unsupported
			AutomationElementIdentifiers.IsDialogProperty,           // DirectDerived
			AutomationElementIdentifiers.IsDataValidForFormProperty, // DirectDerived
			AutomationElementIdentifiers.PositionInSetProperty,      // DirectDerived
			AutomationElementIdentifiers.SizeOfSetProperty,          // DirectDerived
			AutomationElementIdentifiers.IsContentElementProperty,   // Derived
			AutomationElementIdentifiers.ControlTypeProperty,        // Derived
			AutomationElementIdentifiers.AnnotationsProperty,        // DerivedCustom
			AutomationElementIdentifiers.AcceleratorKeyProperty,     // InternalCustom
			AutomationElementIdentifiers.AccessKeyProperty,          // InternalCustom
			AutomationElementIdentifiers.ControlledPeersProperty,    // InternalCustom
			AutomationElementIdentifiers.IsPeripheralProperty,       // Internal
		];

		foreach (var prop in required)
		{
			Assert.IsTrue(tableSet.Contains(prop),
				"A required AutomationElementIdentifiers property is missing from the property table.");
		}
	}

	[TestMethod]
	public void T070_Pattern_State_Groups_Cover_22_Rows()
	{
		Assert.AreEqual(22, s_stateGroupRows.Length,
			"State-group table must list all 22 patterns with documented state in matrix section 3.");
		var patternSet = s_stateGroupRows.Select(r => r.Pattern).ToHashSet();
		Assert.AreEqual(22, patternSet.Count,
			"State-group table must not contain duplicate patterns.");
		foreach (var sg in s_stateGroupRows)
		{
			Assert.IsTrue(s_patternRows.Any(r => r.Pattern == sg.Pattern),
				$"State-group pattern {sg.Pattern} must also appear in the main pattern table.");
		}
	}

	[TestMethod]
	public void T070_Relations_Table_Has_Five_Rows()
	{
		Assert.AreEqual(5, s_relationRows.Length,
			"Matrix section 4 defines exactly 5 relations.");
		var names = s_relationRows.Select(r => r.Name).Distinct().ToList();
		Assert.AreEqual(5, names.Count,
			"Each relation row must have a distinct name.");
	}

	[TestMethod]
	public void T070_Notification_Routing_Is_Separate_From_AutomationEvents_Enum()
	{
		// RaiseNotificationEvent is a distinct peer API; it must not appear as an AutomationEvents value.
		var eventNames = Enum.GetNames<AutomationEvents>();
		Assert.IsFalse(
			eventNames.Any(n => n.Contains("Notification", StringComparison.OrdinalIgnoreCase)),
			"Notification routing must remain a separate API from AutomationEvents.");
	}

	[TestMethod]
	public void T070_Every_Supported_Pattern_Has_Observable_Coverage_Or_Fallback()
	{
		// Unsupported/Internal patterns must claim Internal coverage (no snapshot field, no action).
		// All other patterns must have a non-Internal coverage (real or fallback channel).
		foreach (var row in s_patternRows)
		{
			var isExplicitlyBlocked = row.Class is MatrixClass.Unsupported or MatrixClass.Internal;
			if (isExplicitlyBlocked)
			{
				Assert.AreEqual(PatternCoverage.Internal, row.Coverage,
					$"{row.Pattern} is {row.Class} but claims coverage '{row.Coverage}'; must be Internal.");
			}
			else
			{
				Assert.AreNotEqual(PatternCoverage.Internal, row.Coverage,
					$"{row.Pattern} is {row.Class} but coverage is Internal; provide a real or fallback coverage.");
			}
		}
	}

	// Mobile-gated: heading boolean is the direct path; exact level has no native spoken equivalent.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task T070_Mobile_HeadingLevel_Direct_Boolean_Observed_In_Snapshot()
	{
		var text = new TextBlock { Text = "Section heading" };
		AutomationProperties.SetHeadingLevel(text, AutomationHeadingLevel.Level2);

		await UITestHelper.Load(text);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(text);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsTrue(snapshot.Heading,
			"Heading boolean must be true when HeadingLevel is Level2.");
	}

	// Mobile-gated: IsEnabled -> snapshot.Enabled is a direct native field.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task T070_Mobile_IsEnabled_Direct_Snapshot_Field_Reflects_Disabled_State()
	{
		var button = new Button { Content = "Probe", IsEnabled = false };

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(button);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsFalse(snapshot.Enabled,
			"IsEnabled=false must project directly to snapshot.Enabled=false.");
	}

	#endregion

	#region T071: Explicit fallback and unsupported diagnostics

	[TestMethod]
	public void T071_ObjectModel_Pattern_Is_Unsupported()
	{
		var row = s_patternRows.Single(r => r.Pattern == PatternInterface.ObjectModel);
		Assert.AreEqual(MatrixClass.Unsupported, row.Class,
			"ObjectModel has no native accessibility transport on Android or iOS; must remain Unsupported.");
		Assert.AreEqual(PatternCoverage.Internal, row.Coverage,
			"ObjectModel must not claim any snapshot field or action coverage.");
	}

	[TestMethod]
	public void T071_SynchronizedInput_Pattern_Is_Unsupported()
	{
		var row = s_patternRows.Single(r => r.Pattern == PatternInterface.SynchronizedInput);
		Assert.AreEqual(MatrixClass.Unsupported, row.Class,
			"SynchronizedInput has no native accessibility transport on Android or iOS; must remain Unsupported.");
		Assert.AreEqual(PatternCoverage.Internal, row.Coverage,
			"SynchronizedInput must not claim any snapshot field or action coverage.");
	}

	[TestMethod]
	public void T071_Exactly_Two_Patterns_Are_Unsupported()
	{
		var unsupported = s_patternRows.Where(r => r.Class == MatrixClass.Unsupported).ToList();
		Assert.AreEqual(2, unsupported.Count,
			$"Expected exactly ObjectModel and SynchronizedInput as Unsupported; got: "
			+ string.Join(", ", unsupported.Select(r => r.Pattern)));
		Assert.IsTrue(unsupported.Any(r => r.Pattern == PatternInterface.ObjectModel));
		Assert.IsTrue(unsupported.Any(r => r.Pattern == PatternInterface.SynchronizedInput));
	}

	[TestMethod]
	public void T071_Input_Diagnostic_Events_Are_Unsupported()
	{
		AutomationEvents[] diagnosticOnly =
		[
			AutomationEvents.InputReachedTarget,
			AutomationEvents.InputReachedOtherElement,
			AutomationEvents.InputDiscarded,
		];
		foreach (var ev in diagnosticOnly)
		{
			var row = s_eventRows.Single(r => r.Event == ev);
			Assert.AreEqual(MatrixClass.Unsupported, row.Class,
				$"{ev} is diagnostic-only and must remain Unsupported in the event table.");
		}
	}

	[TestMethod]
	public void T071_HeadingLevel_Is_DirectDerived_Not_Direct()
	{
		// HeadingLevel is DirectDerived: the heading boolean is native-direct, but the exact
		// numeric level has no reliable spoken equivalent on Android or iOS.
		var row = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.HeadingLevelProperty));
		Assert.AreEqual(MatrixClass.DirectDerived, row.Class,
			"HeadingLevel must be DirectDerived: heading=true is direct, exact spoken level is unsupported.");
		Assert.AreNotEqual(MatrixClass.Direct, row.Class,
			"HeadingLevel must NOT be reclassified to Direct while exact spoken level remains unsupported.");
	}

	[TestMethod]
	public void T071_TextRange_Is_Derived_Not_Direct()
	{
		// Full UIA TextRange object identity is not copied to the native platform.
		var row = s_patternRows.Single(r => r.Pattern == PatternInterface.TextRange);
		Assert.AreEqual(MatrixClass.Derived, row.Class,
			"UIA TextRange object identity is not transported natively; must be Derived.");
		Assert.AreNotEqual(MatrixClass.Direct, row.Class,
			"TextRange must not be promoted to Direct while UIA range identity is untranslated.");
	}

	[TestMethod]
	public void T071_Drag_And_DropTarget_Do_Not_Advertise_Dead_Actions()
	{
		foreach (var pattern in new[] { PatternInterface.Drag, PatternInterface.DropTarget })
		{
			var row = s_patternRows.Single(candidate => candidate.Pattern == pattern);
			Assert.AreEqual(MatrixClass.InternalCustom, row.Class);
			Assert.AreEqual(PatternCoverage.FallbackContent, row.Coverage);
		}
	}

	[TestMethod]
	public void T071_LabeledBy_Relation_Enforces_No_Dangling_Target()
	{
		var row = s_relationRows.Single(r => r.Name == "LabeledBy");
		Assert.IsTrue(row.NoDanglingTarget,
			"LabeledBy must enforce the no-dangling-target invariant per matrix section 4.");
	}

	[TestMethod]
	public void T071_FlowsTo_FlowsFrom_Relation_Enforces_No_Dangling_Target()
	{
		var row = s_relationRows.Single(r => r.Name == "FlowsTo/FlowsFrom");
		Assert.IsTrue(row.NoDanglingTarget,
			"FlowsTo/FlowsFrom must omit stale or cross-window targets (no dangling).");
	}

	[TestMethod]
	public void T071_IsPassword_Property_Is_Direct()
	{
		var row = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.IsPasswordProperty));
		Assert.AreEqual(MatrixClass.Direct, row.Class,
			"IsPassword must be Direct: both Android and iOS have a native password/secure-field concept.");
	}

	[TestMethod]
	public void T071_AutomationId_Is_Direct_And_Label_Does_Not_Claim_Spoken_Output()
	{
		var row = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.AutomationIdProperty));
		Assert.AreEqual(MatrixClass.Direct, row.Class,
			"AutomationId must be Direct: maps to resource-ID / AccessibilityIdentifier, not spoken name.");
		// The row label must be the machine identifier, not "Name".
		Assert.AreNotEqual(
			s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.NameProperty)).Label,
			row.Label,
			"AutomationId and Name must have distinct labels; AutomationId must never alias the spoken name.");
	}

	[TestMethod]
	public void T071_IsPeripheral_Is_Internal_Only()
	{
		var row = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.IsPeripheralProperty));
		Assert.AreEqual(MatrixClass.Internal, row.Class,
			"IsPeripheral has no native AT equivalent on Android or iOS; must remain Internal.");
	}

	[TestMethod]
	public void T071_AcceleratorKey_And_AccessKey_Are_InternalCustom()
	{
		AutomationProperty[] noMobileKeyboardContract =
		[
			AutomationElementIdentifiers.AcceleratorKeyProperty,
			AutomationElementIdentifiers.AccessKeyProperty,
		];
		foreach (var prop in noMobileKeyboardContract)
		{
			var row = s_propertyRows.Single(r => ReferenceEquals(r.Property, prop));
			Assert.AreEqual(MatrixClass.InternalCustom, row.Class,
				$"{row.Label} has no mobile keyboard contract; must be InternalCustom.");
		}
	}

	[TestMethod]
	public void T071_Unsupported_And_Internal_Patterns_Have_No_Native_Dispatch_Coverage()
	{
		// Fails when an Unsupported/Internal pattern is accidentally reclassified to a coverage
		// type that implies a native snapshot field or action dispatch.
		var bad = s_patternRows
			.Where(r => r.Class is MatrixClass.Unsupported or MatrixClass.Internal)
			.Where(r => r.Coverage != PatternCoverage.Internal)
			.ToList();
		Assert.AreEqual(0, bad.Count,
			"Unsupported/Internal patterns must not claim native dispatch coverage: "
			+ string.Join(", ", bad.Select(r => $"{r.Pattern}({r.Coverage})")));
	}

	[TestMethod]
	public void T071_Password_And_AutomationId_Redaction_Invariants_In_Table()
	{
		// IsPassword is Direct: password elements have a dedicated native field, not just a hint.
		var pwRow = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.IsPasswordProperty));
		Assert.AreEqual(MatrixClass.Direct, pwRow.Class,
			"Password redaction requires a direct native mechanism; classification must be Direct.");

		// AutomationId is Direct: maps to the machine identifier, never borrowed for spoken output.
		var idRow = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.AutomationIdProperty));
		Assert.AreEqual(MatrixClass.Direct, idRow.Class,
			"AutomationId redaction (never spoken name) requires a direct native mechanism.");

		// Name is also Direct, but must be a distinct row from AutomationId.
		var nameRow = s_propertyRows.Single(r => ReferenceEquals(r.Property, AutomationElementIdentifiers.NameProperty));
		Assert.AreNotSame(idRow, nameRow,
			"AutomationId and Name must be distinct rows in the property table.");
	}

	// Mobile-gated: snapshot.Password must be true for a PasswordBox.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task T071_Mobile_PasswordBox_Snapshot_Password_Field_Is_True()
	{
		var box = new PasswordBox();

		await UITestHelper.Load(box);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(box);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsTrue(snapshot.Password,
			"PasswordBox snapshot.Password must be true (direct native redaction path).");
	}

	// Mobile-gated: AutomationId must never appear in the spoken Name field.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task T071_Mobile_AutomationId_Does_Not_Bleed_Into_Snapshot_Name()
	{
		const string machineId = "t071_id_redaction_probe";
		var button = new Button();
		AutomationProperties.SetAutomationId(button, machineId);

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(button);
		Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
		Assert.IsFalse(
			snapshot.Name?.Contains(machineId, StringComparison.Ordinal) is true,
			"AutomationId must never appear in the spoken Name; it is a machine identifier only.");
		Assert.IsTrue(
			snapshot.AutomationId?.EndsWith(machineId, StringComparison.Ordinal) is true,
			$"AutomationId must be preserved in the snapshot.AutomationId field; got '{snapshot.AutomationId}'.");
	}

	#endregion
}
