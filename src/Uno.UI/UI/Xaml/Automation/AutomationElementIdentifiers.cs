namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as automation property identifiers by UI Automation providers and UI Automation clients.
/// </summary>
public partial class AutomationElementIdentifiers
{

	internal AutomationElementIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the bounding rectangle automation property. The bounding rectangle property value is returned 
	/// by the GetBoundingRectangle method.
	/// </summary>
	public static AutomationProperty BoundingRectangleProperty { get; } = new();

	/// <summary>
	/// Identifies the keyboard focus automation property. The keyboard focus state is returned by the 
	/// HasKeyboardFocus method.
	/// </summary>
	public static AutomationProperty HasKeyboardFocusProperty { get; } = new();

	/// <summary>
	/// Identifies the help text automation property. The help text property value is returned by the 
	/// GetHelpText method.
	/// </summary>
	public static AutomationProperty HelpTextProperty { get; } = new();

	/// <summary>
	/// Identifies the content element determination automation property. The content element status indicates 
	/// whether the element contains content that is valuable to the end user. The current status is returned 
	/// by the IsContentElement method.
	/// </summary>
	public static AutomationProperty IsContentElementProperty { get; } = new();

	/// <summary>
	/// Identifies the control element determination automation property. The control element status indicates 
	/// whether the element contains user interface components that can be manipulated. The current status is 
	/// returned by the IsControlElement method.
	/// </summary>
	public static AutomationProperty IsControlElementProperty { get; } = new();

	/// <summary>
	/// Identifies the enabled determination automation property. The enabled status indicates whether the item 
	/// referenced by the automation peer is enabled. The current status is returned by the IsEnabled method.
	/// </summary>
	public static AutomationProperty IsEnabledProperty { get; } = new();

	/// <summary>
	/// Identifies the keyboard-focusable determination automation property. The keyboard focusable status is 
	/// returned by the IsKeyboardFocusable method.
	/// </summary>
	public static AutomationProperty IsKeyboardFocusableProperty { get; } = new();

	/// <summary>
	/// Identifies the offscreen determination automation property. The offscreen status indicates whether the
	/// item referenced by the automation peer is off the screen. The current status is returned by the IsOffscreen method.
	/// </summary>
	public static AutomationProperty IsOffscreenProperty { get; } = new();

	/// <summary>
	/// Identifies the password determination automation property. The password status indicates whether the item referenced 
	/// by the automation peer contains a password. The current status is returned by the IsPassword method.
	/// </summary>
	public static AutomationProperty IsPasswordProperty { get; } = new();

	/// <summary>
	/// Identifies the form requirement determination automation property. The form requirement status indicates whether the 
	/// element must be completed on a form. The current status is returned by the IsRequiredForForm method.
	/// </summary>
	public static AutomationProperty IsRequiredForFormProperty { get; } = new();

	/// <summary>
	/// Identifies the item status automation property. The current item status is returned by the GetItemStatus method.
	/// </summary>
	public static AutomationProperty ItemStatusProperty { get; } = new();

	/// <summary>
	/// Identifies the item type automation property. The item type value is returned by GetItemType method.
	/// </summary>
	public static AutomationProperty ItemTypeProperty { get; } = new();

	/// <summary>
	/// Identifies the labeled-by peer automation property. The labeling relationship for an automation peer is returned 
	/// by the GetLabeledBy method.
	/// </summary>
	public static AutomationProperty LabeledByProperty { get; } = new();

	/// <summary>
	/// Identifies the live settings automation property. The live settings property value is returned by the GetLiveSetting method.
	/// </summary>
	public static AutomationProperty LiveSettingProperty { get; } = new();

	/// <summary>
	/// Identifies the localized control type automation property which provides a mechanism to alter the control type read by Narrator.
	/// </summary>
	public static AutomationProperty LocalizedControlTypeProperty { get; } = new();

	/// <summary>
	/// Identifies the class name automation property. The class name property value is returned by the GetClassName method.
	/// </summary>
	public static AutomationProperty NameProperty { get; } = new();

	/// <summary>
	/// Identifies the accelerator key automation property. The accelerator key property value is returned by the GetAcceleratorKey method.
	/// </summary>
	public static AutomationProperty AcceleratorKeyProperty { get; } = new();

	/// <summary>
	/// Identifies the access key automation property. The access key property value is returned by the GetAccessKey method.
	/// </summary>
	public static AutomationProperty AccessKeyProperty { get; } = new();

	/// <summary>
	/// Identifies the automation element identifier automation property. The automation element identifier value is returned 
	/// by the GetAutomationId method.
	/// </summary>
	public static AutomationProperty AutomationIdProperty { get; } = new();

	/// <summary>
	/// Identifies the orientation automation property. The current orientation value is returned by the GetOrientation method.
	/// </summary>
	public static AutomationProperty OrientationProperty { get; } = new();

	/// <summary>
	/// Identifies the class name automation property. The class name property value is returned by the GetClassName method.
	/// </summary>
	public static AutomationProperty ClassNameProperty { get; } = new();

	/// <summary>
	/// Identifies the clickable point automation property. A valid clickable point property value is returned by the 
	/// GetClickablePoint method.
	/// </summary>
	public static AutomationProperty ClickablePointProperty { get; } = new();

	/// <summary>
	/// Identifies the control type automation property. The control type property value is returned by the 
	/// GetAutomationControlType method.
	/// </summary>
	public static AutomationProperty ControlTypeProperty { get; } = new();

	/// <summary>
	/// Identifies the controlled peers automation property. A list of controlled peers is returned by the 
	/// GetControlledPeers method.
	/// </summary>
	public static AutomationProperty ControlledPeersProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the annotations automation property.
	/// </summary>
	public static AutomationProperty AnnotationsProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the level automation property.
	/// </summary>
	public static AutomationProperty LevelProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the position in set automation property.
	/// </summary>
	public static AutomationProperty PositionInSetProperty { get; } = new();

	/// <summary>
	/// Gets the identification of the size of set automation property.
	/// </summary>
	public static AutomationProperty SizeOfSetProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the landmark type automation property.
	/// </summary>
	public static AutomationProperty LandmarkTypeProperty { get; } = new();

	/// <summary>
	/// Gets the identifier for the localized landmark type automation property.
	/// </summary>
	public static AutomationProperty LocalizedLandmarkTypeProperty { get; } = new();

	/// <summary>
	/// Identifies the described by automation property.
	/// </summary>
	public static AutomationProperty DescribedByProperty { get; } = new();

	/// <summary>
	/// Identifies the "flows from" automation property. The "flows from" property value is returned by the GetFlowsFrom method.
	/// </summary>
	public static AutomationProperty FlowsFromProperty { get; } = new();

	/// <summary>
	/// Identifies the "flows to" automation property. The "flows to" property value is returned by the GetFlowsTo method.
	/// </summary>
	public static AutomationProperty FlowsToProperty { get; } = new();

	/// <summary>
	/// Identifies the full description automation property.
	/// </summary>
	public static AutomationProperty FullDescriptionProperty { get; } = new();

	/// <summary>
	/// Identifies the Boolean automation property that indicates if the data is valid for the form.
	/// </summary>
	public static AutomationProperty IsDataValidForFormProperty { get; } = new();

	/// <summary>
	/// Identifies the Boolean automation property that indicates if the automation element represents peripheral UI.
	/// </summary>
	public static AutomationProperty IsPeripheralProperty { get; } = new();

	/// <summary>
	/// Identifies the Culture property, which contains a locale identifier for the automation element 
	/// (for example, 0x0409 for "en-US" or English (United States)).
	/// </summary>
	public static AutomationProperty CultureProperty { get; } = new();

	/// <summary>
	/// Identifies the heading level automation property. The heading level property value is returned 
	/// by the GetHeadingLevel method.
	/// </summary>
	public static AutomationProperty HeadingLevelProperty { get; } = new();

	/// <summary>
	/// Identifies the Boolean AutomationProperties.IsDialogProperty that indicates whether the 
	/// automation element is a dialog window.
	/// </summary>
	public static AutomationProperty IsDialogProperty { get; } = new();
}
