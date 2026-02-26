using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Provides attached properties that expose UI Automation metadata for elements.
/// Based on Microsoft Learn: AutomationProperties (Microsoft.UI.Xaml.Automation).
/// </summary>
public partial class AutomationProperties
{
	/// <summary>
	/// Identifies the AcceleratorKey attached property, which describes the accelerator (shortcut) key combination for an element.
	/// </summary>
	public static DependencyProperty AcceleratorKeyProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AcceleratorKey",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the AccessKey attached property, which specifies the access key (mnemonic) for an element.
	/// </summary>
	public static DependencyProperty AccessKeyProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AccessKey",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the AccessibilityView attached property, which controls whether and how the element appears in the UI Automation tree.
	/// </summary>
	public static DependencyProperty AccessibilityViewProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AccessibilityView",
			typeof(AccessibilityView),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(AccessibilityView.Content));

	/// <summary>
	/// Identifies the Annotations attached property, which provides a collection of annotations associated with the element.
	/// </summary>
	public static DependencyProperty AnnotationsProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Annotations",
			typeof(IList<AutomationAnnotation>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<AutomationAnnotation>)));

	/// <summary>
	/// Identifies the AutomationId attached property, which sets a developer-supplied identifier used by UI Automation.
	/// </summary>
	public static DependencyProperty AutomationIdProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AutomationId",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(
				"",
				OnAutomationIdChanged));

	/// <summary>
	/// Identifies the ControlledPeers attached property, which lists peers that this element directly controls.
	/// </summary>
	public static DependencyProperty ControlledPeersProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ControlledPeers",
			typeof(IList<UIElement>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<UIElement>)));

	/// <summary>
	/// Identifies the Culture attached property, which reports the default input language or content locale for the element.
	/// </summary>
	public static DependencyProperty CultureProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Culture",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(int)));

	/// <summary>
	/// Identifies the DescribedBy attached property, which points to elements that provide extended descriptive text.
	/// </summary>
	public static DependencyProperty DescribedByProperty { get; } =
		DependencyProperty.RegisterAttached(
			"DescribedBy",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	/// <summary>
	/// Identifies the FlowsFrom attached property, which indicates the reading order origin for the element.
	/// </summary>
	public static DependencyProperty FlowsFromProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FlowsFrom",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	/// <summary>
	/// Identifies the FlowsTo attached property, which indicates the next elements in the reading order.
	/// </summary>
	public static DependencyProperty FlowsToProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FlowsTo",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	/// <summary>
	/// Identifies the FullDescription attached property, which provides a complete description of the element for assistive technologies.
	/// </summary>
	public static DependencyProperty FullDescriptionProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FullDescription",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the HeadingLevel attached property, which indicates the heading level for structured content.
	/// </summary>
	public static DependencyProperty HeadingLevelProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HeadingLevel",
			typeof(AutomationHeadingLevel),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationHeadingLevel)));

	/// <summary>
	/// Identifies the HelpText attached property, which supplies helpful context or instructions for the element.
	/// </summary>
	public static DependencyProperty HelpTextProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HelpText",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the IsDataValidForForm attached property, which indicates whether the element’s value is valid for form submission.
	/// </summary>
	public static DependencyProperty IsDataValidForFormProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsDataValidForForm",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Identifies the IsDialog attached property, which indicates whether the element represents a dialog.
	/// </summary>
	public static DependencyProperty IsDialogProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsDialog",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Identifies the IsPeripheral attached property, which indicates whether the element is peripheral to the main UI experience.
	/// </summary>
	public static DependencyProperty IsPeripheralProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsPeripheral",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Identifies the IsRequiredForForm attached property, which indicates whether the element requires user input before form submission.
	/// </summary>
	public static DependencyProperty IsRequiredForFormProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsRequiredForForm",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Identifies the ItemStatus attached property, which conveys status information about an element (for example, “New” or “Busy”).
	/// </summary>
	public static DependencyProperty ItemStatusProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ItemStatus",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the ItemType attached property, which describes the type of item represented by the element.
	/// </summary>
	public static DependencyProperty ItemTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ItemType",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the LabeledBy attached property, which references an element that provides the accessible label.
	/// </summary>
	public static DependencyProperty LabeledByProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LabeledBy",
			typeof(UIElement),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(UIElement))
		);

	/// <summary>
	/// Identifies the LandmarkType attached property, which indicates the landmark role of a region for navigation.
	/// </summary>
	public static DependencyProperty LandmarkTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LandmarkType",
			typeof(AutomationLandmarkType),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationLandmarkType)));

	/// <summary>
	/// Identifies the Level attached property, which specifies the hierarchical level of the element within a set or outline.
	/// </summary>
	public static DependencyProperty LevelProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Level",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(int)));

	/// <summary>
	/// Identifies the LiveSetting attached property, which indicates how changes to the element are announced to assistive technologies.
	/// </summary>
	public static DependencyProperty LiveSettingProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LiveSetting",
			typeof(AutomationLiveSetting),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationLiveSetting)));

	/// <summary>
	/// Identifies the LocalizedControlType attached property, which supplies a localized string describing the control type.
	/// </summary>
	public static DependencyProperty LocalizedControlTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LocalizedControlType",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the LocalizedLandmarkType attached property, which supplies a localized string describing the landmark type.
	/// </summary>
	public static DependencyProperty LocalizedLandmarkTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LocalizedLandmarkType",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the Name attached property, which provides the accessible name for an element.
	/// </summary>
	public static DependencyProperty NameProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Name",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(string.Empty, OnNamePropertyChanged)
		);

	/// <summary>
	/// Identifies the PositionInSet attached property, which indicates the 1-based position of the element within a set.
	/// </summary>
	public static DependencyProperty PositionInSetProperty { get; } =
		DependencyProperty.RegisterAttached(
			"PositionInSet",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(-1));

	/// <summary>
	/// Identifies the SizeOfSet attached property, which indicates the total number of items in the set that contains the element.
	/// </summary>
	public static DependencyProperty SizeOfSetProperty { get; } =
		DependencyProperty.RegisterAttached(
			"SizeOfSet",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(-1));

	/// <summary>
	/// Identifies the AutomationControlType attached property, which specifies the UI Automation control type of an element.
	/// </summary>
	public static DependencyProperty AutomationControlTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AutomationControlType",
			typeof(AutomationControlType),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationControlType)));

	/// <summary>
	/// Gets the UI Automation control type for the specified element.
	/// </summary>
	public static AutomationControlType GetAutomationControlType(UIElement element) => (AutomationControlType)element.GetValue(AutomationControlTypeProperty);

	/// <summary>
	/// Sets the UI Automation control type for the specified element.
	/// </summary>
	public static void SetAutomationControlType(UIElement element, AutomationControlType value) => element.SetValue(AutomationControlTypeProperty, value);

	/// <summary>
	/// Gets the accelerator key string for the specified element.
	/// </summary>
	public static string GetAcceleratorKey(DependencyObject element) => (string)element.GetValue(AcceleratorKeyProperty);

	/// <summary>
	/// Sets the accelerator key string for the specified element.
	/// </summary>
	public static void SetAcceleratorKey(DependencyObject element, string value) => element.SetValue(AcceleratorKeyProperty, value);

	/// <summary>
	/// Gets the access key (mnemonic) for the specified element.
	/// </summary>
	public static string GetAccessKey(DependencyObject element) => (string)element.GetValue(AccessKeyProperty);

	/// <summary>
	/// Sets the access key (mnemonic) for the specified element.
	/// </summary>
	public static void SetAccessKey(DependencyObject element, string value) => element.SetValue(AccessKeyProperty, value);

	/// <summary>
	/// Gets the AutomationId for the specified element.
	/// </summary>
	public static string GetAutomationId(DependencyObject element)
		=> (string)element.GetValue(AutomationIdProperty);

	/// <summary>
	/// Sets the AutomationId for the specified element.
	/// </summary>
	public static void SetAutomationId(DependencyObject element, string value)
		=> element.SetValue(AutomationIdProperty, value);

	/// <summary>
	/// Gets the help text associated with the specified element.
	/// </summary>
	public static string GetHelpText(DependencyObject element) => (string)element.GetValue(HelpTextProperty);

	/// <summary>
	/// Sets the help text associated with the specified element.
	/// </summary>
	public static void SetHelpText(DependencyObject element, string value) => element.SetValue(HelpTextProperty, value);

	/// <summary>
	/// Gets whether the element is required for form submission.
	/// </summary>
	public static bool GetIsRequiredForForm(DependencyObject element) => (bool)element.GetValue(IsRequiredForFormProperty);

	/// <summary>
	/// Sets whether the element is required for form submission.
	/// </summary>
	public static void SetIsRequiredForForm(DependencyObject element, bool value) => element.SetValue(IsRequiredForFormProperty, value);

	/// <summary>
	/// Gets the status text associated with the specified element.
	/// </summary>
	public static string GetItemStatus(DependencyObject element) => (string)element.GetValue(ItemStatusProperty);

	/// <summary>
	/// Sets the status text associated with the specified element.
	/// </summary>
	public static void SetItemStatus(DependencyObject element, string value) => element.SetValue(ItemStatusProperty, value);

	/// <summary>
	/// Gets the item type description for the specified element.
	/// </summary>
	public static string GetItemType(DependencyObject element) => (string)element.GetValue(ItemTypeProperty);

	/// <summary>
	/// Sets the item type description for the specified element.
	/// </summary>
	public static void SetItemType(DependencyObject element, string value) => element.SetValue(ItemTypeProperty, value);

	/// <summary>
	/// Gets the element that provides the accessible label for the specified element.
	/// </summary>
	public static UIElement GetLabeledBy(DependencyObject element) => (UIElement)element.GetValue(LabeledByProperty);

	/// <summary>
	/// Sets the element that provides the accessible label for the specified element.
	/// </summary>
	public static void SetLabeledBy(DependencyObject element, UIElement value) => element.SetValue(LabeledByProperty, value);

	/// <summary>
	/// Sets the accessible name for the specified element.
	/// </summary>
	public static void SetName(DependencyObject element, string value) => element.SetValue(NameProperty, value);

	/// <summary>
	/// Gets the accessible name for the specified element.
	/// </summary>
	public static string GetName(DependencyObject element) => (string)element.GetValue(NameProperty);

	/// <summary>
	/// Gets the live region setting for the specified element.
	/// </summary>
	public static AutomationLiveSetting GetLiveSetting(DependencyObject element) => (AutomationLiveSetting)element.GetValue(LiveSettingProperty);

	/// <summary>
	/// Sets the live region setting for the specified element.
	/// </summary>
	public static void SetLiveSetting(DependencyObject element, AutomationLiveSetting value) => element.SetValue(LiveSettingProperty, value);

	/// <summary>
	/// Gets the accessibility view for the specified element.
	/// </summary>
	public static AccessibilityView GetAccessibilityView(DependencyObject element) => (AccessibilityView)element.GetValue(AccessibilityViewProperty);

	/// <summary>
	/// Sets the accessibility view for the specified element.
	/// </summary>
	public static void SetAccessibilityView(DependencyObject element, AccessibilityView value) => element.SetValue(AccessibilityViewProperty, value);

	/// <summary>
	/// Gets the list of controlled peers for the specified element.
	/// </summary>
	public static IList<UIElement> GetControlledPeers(DependencyObject element) => (IList<UIElement>)element.GetValue(ControlledPeersProperty);

	/// <summary>
	/// Gets the 1-based position of the element within its set.
	/// </summary>
	public static int GetPositionInSet(DependencyObject element) => (int)element.GetValue(PositionInSetProperty);

	/// <summary>
	/// Sets the 1-based position of the element within its set.
	/// </summary>
	public static void SetPositionInSet(DependencyObject element, int value) => element.SetValue(PositionInSetProperty, value);

	/// <summary>
	/// Gets the total size of the set that contains the element.
	/// </summary>
	public static int GetSizeOfSet(DependencyObject element) => (int)element.GetValue(SizeOfSetProperty);

	/// <summary>
	/// Sets the total size of the set that contains the element.
	/// </summary>
	public static void SetSizeOfSet(DependencyObject element, int value) => element.SetValue(SizeOfSetProperty, value);

	/// <summary>
	/// Gets the hierarchical level of the element.
	/// </summary>
	public static int GetLevel(DependencyObject element) => (int)element.GetValue(LevelProperty);

	/// <summary>
	/// Sets the hierarchical level of the element.
	/// </summary>
	public static void SetLevel(DependencyObject element, int value) => element.SetValue(LevelProperty, value);

	/// <summary>
	/// Gets the annotations associated with the specified element.
	/// </summary>
	public static IList<AutomationAnnotation> GetAnnotations(DependencyObject element) => (IList<AutomationAnnotation>)element.GetValue(AnnotationsProperty);

	/// <summary>
	/// Gets the landmark type for the specified element.
	/// </summary>
	public static AutomationLandmarkType GetLandmarkType(DependencyObject element) => (AutomationLandmarkType)element.GetValue(LandmarkTypeProperty);

	/// <summary>
	/// Sets the landmark type for the specified element.
	/// </summary>
	public static void SetLandmarkType(DependencyObject element, AutomationLandmarkType value) => element.SetValue(LandmarkTypeProperty, value);

	/// <summary>
	/// Gets the localized landmark type string for the specified element.
	/// </summary>
	public static string GetLocalizedLandmarkType(DependencyObject element) => (string)element.GetValue(LocalizedLandmarkTypeProperty);

	/// <summary>
	/// Sets the localized landmark type string for the specified element.
	/// </summary>
	public static void SetLocalizedLandmarkType(DependencyObject element, string value) => element.SetValue(LocalizedLandmarkTypeProperty, value);

	/// <summary>
	/// Gets whether the element is peripheral to the main UI experience.
	/// </summary>
	public static bool GetIsPeripheral(DependencyObject element) => (bool)element.GetValue(IsPeripheralProperty);

	/// <summary>
	/// Sets whether the element is peripheral to the main UI experience.
	/// </summary>
	public static void SetIsPeripheral(DependencyObject element, bool value) => element.SetValue(IsPeripheralProperty, value);

	/// <summary>
	/// Gets whether the element’s data is valid for form submission.
	/// </summary>
	public static bool GetIsDataValidForForm(DependencyObject element) => (bool)element.GetValue(IsDataValidForFormProperty);

	/// <summary>
	/// Sets whether the element’s data is valid for form submission.
	/// </summary>
	public static void SetIsDataValidForForm(DependencyObject element, bool value) => element.SetValue(IsDataValidForFormProperty, value);

	/// <summary>
	/// Gets the full description text for the specified element.
	/// </summary>
	public static string GetFullDescription(DependencyObject element) => (string)element.GetValue(FullDescriptionProperty);

	/// <summary>
	/// Sets the full description text for the specified element.
	/// </summary>
	public static void SetFullDescription(DependencyObject element, string value) => element.SetValue(FullDescriptionProperty, value);

	/// <summary>
	/// Gets the localized control type string for the specified element.
	/// </summary>
	public static string GetLocalizedControlType(DependencyObject element) => (string)element.GetValue(LocalizedControlTypeProperty);

	/// <summary>
	/// Sets the localized control type string for the specified element.
	/// </summary>
	public static void SetLocalizedControlType(DependencyObject element, string value) => element.SetValue(LocalizedControlTypeProperty, value);

	/// <summary>
	/// Gets the collection of elements that describe the specified element.
	/// </summary>
	public static IList<DependencyObject> GetDescribedBy(DependencyObject element) => (IList<DependencyObject>)element.GetValue(DescribedByProperty);

	/// <summary>
	/// Gets the collection of elements that are next in reading order from the specified element.
	/// </summary>
	public static IList<DependencyObject> GetFlowsTo(DependencyObject element) => (IList<DependencyObject>)element.GetValue(FlowsToProperty);

	/// <summary>
	/// Gets the collection of elements that precede the specified element in reading order.
	/// </summary>
	public static IList<DependencyObject> GetFlowsFrom(DependencyObject element) => (IList<DependencyObject>)element.GetValue(FlowsFromProperty);

	/// <summary>
	/// Gets the culture (locale) identifier for the specified element.
	/// </summary>
	public static int GetCulture(DependencyObject element) => (int)element.GetValue(CultureProperty);

	/// <summary>
	/// Sets the culture (locale) identifier for the specified element.
	/// </summary>
	public static void SetCulture(DependencyObject element, int value) => element.SetValue(CultureProperty, value);

	/// <summary>
	/// Gets the heading level for the specified element.
	/// </summary>
	public static AutomationHeadingLevel GetHeadingLevel(DependencyObject element) => (AutomationHeadingLevel)element.GetValue(HeadingLevelProperty);

	/// <summary>
	/// Sets the heading level for the specified element.
	/// </summary>
	public static void SetHeadingLevel(DependencyObject element, AutomationHeadingLevel value) => element.SetValue(HeadingLevelProperty, value);

	/// <summary>
	/// Gets whether the element represents a dialog.
	/// </summary>
	public static bool GetIsDialog(DependencyObject element) => (bool)element.GetValue(IsDialogProperty);

	/// <summary>
	/// Sets whether the element represents a dialog.
	/// </summary>
	public static void SetIsDialog(DependencyObject element, bool value) => element.SetValue(IsDialogProperty, value);
}
