using System.Collections.Generic;
using Uno.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Automation
{
	public sealed partial class AutomationProperties
	{
		#region Name

		public static void SetName(DependencyObject element, string value)
		{
			element.SetValue(NameProperty, value);
		}

		public static string GetName(DependencyObject element)
		{
			return (string)element.GetValue(NameProperty);
		}

		public static DependencyProperty NameProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Name",
				typeof(string),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(string.Empty, OnNamePropertyChanged)
			);


		private static void OnNamePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}

		#endregion

		#region AccessibilityView

		public static AccessibilityView GetAccessibilityView(DependencyObject element)
		{
			return (AccessibilityView)element.GetValue(AccessibilityViewProperty);
		}

		public static void SetAccessibilityeView(DependencyObject element, AccessibilityView value)
		{
			element.SetValue(AccessibilityViewProperty, value);
		}

		public static DependencyProperty AccessibilityViewProperty { get; } =
			DependencyProperty.RegisterAttached(
				"AccessibilityView",
				typeof(AccessibilityView),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(AccessibilityView.Content)
			);

		#endregion

		#region LabeledBy

		public static UIElement GetLabeledBy(DependencyObject element)
		{
			return (UIElement)element.GetValue(LabeledByProperty);
		}

		public static void SetLabeledBy(DependencyObject element, UIElement value)
		{
			element.SetValue(LabeledByProperty, value);
		}

		public static DependencyProperty LabeledByProperty { get; } =
			DependencyProperty.RegisterAttached(
				"LabeledBy", typeof(UIElement),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(UIElement))
			);

		#endregion

		#region LocalizedControlType

		public static string GetLocalizedControlType(DependencyObject element)
		{
			return (string)element.GetValue(LocalizedControlTypeProperty);
		}

		public static void SetLocalizedControlType(DependencyObject element, string value)
		{
			element.SetValue(LocalizedControlTypeProperty, value);
		}

		public static DependencyProperty LocalizedControlTypeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"LocalizedControlType", typeof(string),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(string))
			);

		#endregion

		#region DescribedBy

		public static IList<DependencyObject> GetDescribedBy(DependencyObject element)
		{
			return (IList<DependencyObject>)element.GetValue(DescribedByProperty);
		}

		public static DependencyProperty DescribedByProperty { get; } =
			DependencyProperty.RegisterAttached(
				"DescribedBy", typeof(IList<DependencyObject>),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(IList<DependencyObject>)) // TODO: Empty list?
			);

		#endregion

		#region AutomationId

		public static DependencyProperty AutomationIdProperty { get; } =
		DependencyProperty.RegisterAttached(
			name: "AutomationId",
			propertyType: typeof(string),
			ownerType: typeof(AutomationProperties),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: "",
				propertyChangedCallback: OnAutomationIdChanged)
		);

		private static void OnAutomationIdChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if __IOS__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is UIKit.UIView view)
			{
				view.AccessibilityIdentifier = (string)args.NewValue;
			}
#elif __MACOS__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is AppKit.NSView view)
			{
				view.AccessibilityIdentifier = (string)args.NewValue;
			}
#elif __ANDROID__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is Android.Views.View view)
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
		#endregion

		public static int GetPositionInSet(global::Windows.UI.Xaml.DependencyObject element) => (int)element.GetValue(PositionInSetProperty);

		public static void SetPositionInSet(DependencyObject element, int value) => element.SetValue(PositionInSetProperty, value);

		public static DependencyProperty PositionInSetProperty { get; } =
			DependencyProperty.RegisterAttached(
				"PositionInSet", typeof(int),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(int)));

		public static int GetSizeOfSet(DependencyObject element) => (int)element.GetValue(SizeOfSetProperty);

		public static void SetSizeOfSet(DependencyObject element, int value) => element.SetValue(SizeOfSetProperty, value);

		public static DependencyProperty SizeOfSetProperty { get; } =
			DependencyProperty.RegisterAttached(
				"SizeOfSet", typeof(int),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(int)));

		public static AutomationLandmarkType GetLandmarkType(DependencyObject element) => (AutomationLandmarkType)element.GetValue(LandmarkTypeProperty);

		public static void SetLandmarkType(DependencyObject element, AutomationLandmarkType value) => element.SetValue(LandmarkTypeProperty, value);

		public static DependencyProperty LandmarkTypeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"LandmarkType", typeof(AutomationLandmarkType),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(AutomationLandmarkType)));

#if __WASM__
		private static string FindHtmlRole(UIElement uIElement)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Button_Available && uIElement is Button)
			{
				return "button";
			}
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_RadioButton_Available && uIElement is RadioButton)
			{
				return "radio";
			}
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_CheckBox_Available && uIElement is CheckBox)
			{
				return "checkbox";
			}
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_TextBlock_Available && uIElement is TextBlock)
			{
				return "label";
			}
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_TextBox_Available && uIElement is TextBox)
			{
				return "textbox";
			}
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Slider_Available && uIElement is Slider)
			{
				return "slider";
			}

			return null;
		}
#endif

	}
}
