using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation;

public partial class AutomationProperties
{
	public static DependencyProperty AcceleratorKeyProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AcceleratorKey", typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty AccessKeyProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AccessKey", typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty AccessibilityViewProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AccessibilityView",
			typeof(AccessibilityView),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(AccessibilityView.Content));

	public static DependencyProperty AnnotationsProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Annotations",
			typeof(IList<AutomationAnnotation>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<AutomationAnnotation>)));

	public static DependencyProperty AutomationIdProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AutomationId",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(
				"",
				OnAutomationIdChanged));

	public static DependencyProperty ControlledPeersProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ControlledPeers",
			typeof(IList<UIElement>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<UIElement>)));

	public static DependencyProperty CultureProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Culture",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(int)));

	public static DependencyProperty DescribedByProperty { get; } =
		DependencyProperty.RegisterAttached(
			"DescribedBy",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	public static DependencyProperty FlowsFromProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FlowsFrom",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	public static DependencyProperty FlowsToProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FlowsTo",
			typeof(IList<DependencyObject>),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));

	public static DependencyProperty FullDescriptionProperty { get; } =
		DependencyProperty.RegisterAttached(
			"FullDescription",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty HeadingLevelProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HeadingLevel", typeof(AutomationHeadingLevel),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationHeadingLevel)));

	public static DependencyProperty HelpTextProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HelpText", typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty IsDataValidForFormProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsDataValidForForm", typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	public static DependencyProperty IsDialogProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsDialog",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	public static DependencyProperty IsPeripheralProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsPeripheral",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	public static DependencyProperty IsRequiredForFormProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsRequiredForForm",
			typeof(bool),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(bool)));

	public static DependencyProperty ItemStatusProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ItemStatus",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty ItemTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ItemType",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty LabeledByProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LabeledBy",
			typeof(UIElement),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(UIElement))
		);

	public static DependencyProperty LandmarkTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LandmarkType",
			typeof(AutomationLandmarkType),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(AutomationLandmarkType)));

	public static DependencyProperty LevelProperty { get; } =
	DependencyProperty.RegisterAttached(
		"Level", typeof(int),
		typeof(AutomationProperties),
		new FrameworkPropertyMetadata(default(int)));



	public static DependencyProperty LiveSettingProperty { get; } =
	DependencyProperty.RegisterAttached(
		"LiveSetting", typeof(AutomationLiveSetting),
		typeof(AutomationProperties),
		new FrameworkPropertyMetadata(default(AutomationLiveSetting)));

	public static DependencyProperty LocalizedControlTypeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"LocalizedControlType",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));


	public static DependencyProperty LocalizedLandmarkTypeProperty { get; } =
	DependencyProperty.RegisterAttached(
		"LocalizedLandmarkType", typeof(string),
		typeof(AutomationProperties),
		new FrameworkPropertyMetadata(default(string)));

	public static DependencyProperty NameProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Name",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(string.Empty, OnNamePropertyChanged)
		);

	public static DependencyProperty PositionInSetProperty { get; } =
		DependencyProperty.RegisterAttached(
			"PositionInSet",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(-1)); //TODO:MZ: Validate this default value



	public static DependencyProperty SizeOfSetProperty { get; } =
		DependencyProperty.RegisterAttached(
			"SizeOfSet",
			typeof(int),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(-1)); //TODO:MZ: Validate this default value


	public static DependencyProperty AutomationControlTypeProperty { get; } =
	DependencyProperty.RegisterAttached(
		"AutomationControlType", typeof(AutomationControlType),
		typeof(AutomationProperties),
		new FrameworkPropertyMetadata(default(AutomationControlType)));

	// Forced skipping of method AutomationProperties.AutomationControlTypeProperty.get


	public static Peers.AutomationControlType GetAutomationControlType(UIElement element)
	{
		return (Peers.AutomationControlType)element.GetValue(AutomationControlTypeProperty);
	}



	public static void SetAutomationControlType(UIElement element, Peers.AutomationControlType value)
	{
		element.SetValue(AutomationControlTypeProperty, value);
	}

	// Forced skipping of method AutomationProperties.AcceleratorKeyProperty.get


	public static string GetAcceleratorKey(DependencyObject element)
	{
		return (string)element.GetValue(AcceleratorKeyProperty);
	}



	public static void SetAcceleratorKey(DependencyObject element, string value)
	{
		element.SetValue(AcceleratorKeyProperty, value);
	}

	// Forced skipping of method AutomationProperties.AccessKeyProperty.get


	public static string GetAccessKey(DependencyObject element)
	{
		return (string)element.GetValue(AccessKeyProperty);
	}



	public static void SetAccessKey(DependencyObject element, string value)
	{
		element.SetValue(AccessKeyProperty, value);
	}


	private static void OnAutomationIdChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __APPLE_UIKIT__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is UIKit.UIView view)
			{
				view.AccessibilityIdentifier = (string)args.NewValue;
			}
#elif __ANDROID__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is AView view)
			{
				view.ContentDescription = (string)args.NewValue;
			}
#elif __WASM__
			if (dependencyObject is UIElement uiElement)
			{
				if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
				{
					uiElement.SetAttribute("xamlautomationid", (string)args.NewValue);
				}

				var role = FindHtmlRole(uiElement);
				if (role != null)
				{
					uiElement.SetAttribute(
						("aria-label", (string)args.NewValue),
						("role", role));
				}
			}
#endif
	}

	public static string GetAutomationId(DependencyObject element)
		=> (string)element.GetValue(AutomationIdProperty);

	public static void SetAutomationId(DependencyObject element, string value)
		=> element.SetValue(AutomationIdProperty, value);


	public static string GetHelpText(DependencyObject element)
	{
		return (string)element.GetValue(HelpTextProperty);
	}



	public static void SetHelpText(DependencyObject element, string value)
	{
		element.SetValue(HelpTextProperty, value);
	}

	// Forced skipping of method AutomationProperties.IsRequiredForFormProperty.get


	public static bool GetIsRequiredForForm(DependencyObject element)
	{
		return (bool)element.GetValue(IsRequiredForFormProperty);
	}



	public static void SetIsRequiredForForm(DependencyObject element, bool value)
	{
		element.SetValue(IsRequiredForFormProperty, value);
	}

	// Forced skipping of method AutomationProperties.ItemStatusProperty.get


	public static string GetItemStatus(DependencyObject element)
	{
		return (string)element.GetValue(ItemStatusProperty);
	}



	public static void SetItemStatus(DependencyObject element, string value)
	{
		element.SetValue(ItemStatusProperty, value);
	}

	// Forced skipping of method AutomationProperties.ItemTypeProperty.get


	public static string GetItemType(DependencyObject element)
	{
		return (string)element.GetValue(ItemTypeProperty);
	}



	public static void SetItemType(DependencyObject element, string value)
	{
		element.SetValue(ItemTypeProperty, value);
	}

	public static UIElement GetLabeledBy(DependencyObject element)
	{
		return (UIElement)element.GetValue(LabeledByProperty);
	}

	public static void SetLabeledBy(DependencyObject element, UIElement value)
	{
		element.SetValue(LabeledByProperty, value);
	}


	public static void SetName(DependencyObject element, string value)
	{
		element.SetValue(NameProperty, value);
	}

	public static string GetName(DependencyObject element)
	{
		return (string)element.GetValue(NameProperty);
	}


	public static Peers.AutomationLiveSetting GetLiveSetting(DependencyObject element)
	{
		return (Peers.AutomationLiveSetting)element.GetValue(LiveSettingProperty);
	}



	public static void SetLiveSetting(DependencyObject element, Peers.AutomationLiveSetting value)
	{
		element.SetValue(LiveSettingProperty, value);
	}


	public static AccessibilityView GetAccessibilityView(DependencyObject element)
	{
		return (AccessibilityView)element.GetValue(AccessibilityViewProperty);
	}

	public static void SetAccessibilityView(DependencyObject element, AccessibilityView value)
	{
		element.SetValue(AccessibilityViewProperty, value);
	}

	public static IList<UIElement> GetControlledPeers(DependencyObject element)
	{
		return (IList<UIElement>)element.GetValue(ControlledPeersProperty);
	}

	public static int GetPositionInSet(global::Microsoft.UI.Xaml.DependencyObject element) => (int)element.GetValue(PositionInSetProperty);

	public static void SetPositionInSet(DependencyObject element, int value) => element.SetValue(PositionInSetProperty, value);


	public static int GetSizeOfSet(DependencyObject element) => (int)element.GetValue(SizeOfSetProperty);

	public static void SetSizeOfSet(DependencyObject element, int value) => element.SetValue(SizeOfSetProperty, value);


	public static int GetLevel(DependencyObject element)
	{
		return (int)element.GetValue(LevelProperty);
	}



	public static void SetLevel(DependencyObject element, int value)
	{
		element.SetValue(LevelProperty, value);
	}

	// Forced skipping of method AutomationProperties.AnnotationsProperty.get


	public static IList<AutomationAnnotation> GetAnnotations(DependencyObject element)
	{
		return (IList<AutomationAnnotation>)element.GetValue(AnnotationsProperty);
	}

	public static AutomationLandmarkType GetLandmarkType(DependencyObject element) => (AutomationLandmarkType)element.GetValue(LandmarkTypeProperty);

	public static void SetLandmarkType(DependencyObject element, AutomationLandmarkType value) => element.SetValue(LandmarkTypeProperty, value);

	public static string GetLocalizedLandmarkType(DependencyObject element)
	{
		return (string)element.GetValue(LocalizedLandmarkTypeProperty);
	}



	public static void SetLocalizedLandmarkType(DependencyObject element, string value)
	{
		element.SetValue(LocalizedLandmarkTypeProperty, value);
	}

	// Forced skipping of method AutomationProperties.IsPeripheralProperty.get


	public static bool GetIsPeripheral(DependencyObject element)
	{
		return (bool)element.GetValue(IsPeripheralProperty);
	}



	public static void SetIsPeripheral(DependencyObject element, bool value)
	{
		element.SetValue(IsPeripheralProperty, value);
	}

	// Forced skipping of method AutomationProperties.IsDataValidForFormProperty.get


	public static bool GetIsDataValidForForm(DependencyObject element)
	{
		return (bool)element.GetValue(IsDataValidForFormProperty);
	}



	public static void SetIsDataValidForForm(DependencyObject element, bool value)
	{
		element.SetValue(IsDataValidForFormProperty, value);
	}

	// Forced skipping of method AutomationProperties.FullDescriptionProperty.get


	public static string GetFullDescription(DependencyObject element)
	{
		return (string)element.GetValue(FullDescriptionProperty);
	}



	public static void SetFullDescription(DependencyObject element, string value)
	{
		element.SetValue(FullDescriptionProperty, value);
	}

	public static string GetLocalizedControlType(DependencyObject element)
	{
		return (string)element.GetValue(LocalizedControlTypeProperty);
	}

	public static void SetLocalizedControlType(DependencyObject element, string value)
	{
		element.SetValue(LocalizedControlTypeProperty, value);
	}

	public static IList<DependencyObject> GetDescribedBy(DependencyObject element)
	{
		return (IList<DependencyObject>)element.GetValue(DescribedByProperty);
	}

	// Forced skipping of method AutomationProperties.FlowsToProperty.get


	public static IList<DependencyObject> GetFlowsTo(DependencyObject element)
	{
		return (IList<DependencyObject>)element.GetValue(FlowsToProperty);
	}

	// Forced skipping of method AutomationProperties.FlowsFromProperty.get


	public static IList<DependencyObject> GetFlowsFrom(DependencyObject element)
	{
		return (IList<DependencyObject>)element.GetValue(FlowsFromProperty);
	}

	// Forced skipping of method AutomationProperties.CultureProperty.get


	public static int GetCulture(DependencyObject element)
	{
		return (int)element.GetValue(CultureProperty);
	}



	public static void SetCulture(DependencyObject element, int value)
	{
		element.SetValue(CultureProperty, value);
	}

	// Forced skipping of method AutomationProperties.HeadingLevelProperty.get


	public static Peers.AutomationHeadingLevel GetHeadingLevel(DependencyObject element)
	{
		return (Peers.AutomationHeadingLevel)element.GetValue(HeadingLevelProperty);
	}



	public static void SetHeadingLevel(DependencyObject element, Peers.AutomationHeadingLevel value)
	{
		element.SetValue(HeadingLevelProperty, value);
	}

	// Forced skipping of method AutomationProperties.IsDialogProperty.get


	public static bool GetIsDialog(DependencyObject element)
	{
		return (bool)element.GetValue(IsDialogProperty);
	}



	public static void SetIsDialog(DependencyObject element, bool value)
	{
		element.SetValue(IsDialogProperty, value);
	}

}
