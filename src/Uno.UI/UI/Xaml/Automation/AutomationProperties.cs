using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uno.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Automation
{
	[Bindable]
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

		public static DependencyProperty NameProperty
		{
			[DynamicDependency(nameof(GetName))]
			[DynamicDependency(nameof(SetName))]
			get;
		} = DependencyProperty.RegisterAttached(
				"Name",
				typeof(string),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(string.Empty, OnNamePropertyChanged)
			);


		private static void OnNamePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if __SKIA__
			if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
				dependencyObject is UIElement element && // TODO: Adjust when TextElement's automation peers are supported.
				element.GetOrCreateAutomationPeer() is { } peer)
			{
				AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.NameProperty, args.OldValue, args.NewValue);
			}
#endif
		}

		#endregion

		#region AccessibilityView

		public static AccessibilityView GetAccessibilityView(DependencyObject element)
		{
			return (AccessibilityView)element.GetValue(AccessibilityViewProperty);
		}

		public static void SetAccessibilityView(DependencyObject element, AccessibilityView value)
		{
			element.SetValue(AccessibilityViewProperty, value);
		}

		public static DependencyProperty AccessibilityViewProperty
		{
			[DynamicDependency(nameof(GetAccessibilityView))]
			[DynamicDependency(nameof(SetAccessibilityView))]
			get;
		} = DependencyProperty.RegisterAttached(
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

		public static DependencyProperty LabeledByProperty
		{
			[DynamicDependency(nameof(GetLabeledBy))]
			[DynamicDependency(nameof(SetLabeledBy))]
			get;
		} = DependencyProperty.RegisterAttached(
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

		public static DependencyProperty LocalizedControlTypeProperty
		{
			[DynamicDependency(nameof(GetLocalizedControlType))]
			[DynamicDependency(nameof(SetLocalizedControlType))]
			get;
		} = DependencyProperty.RegisterAttached(
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

		public static DependencyProperty DescribedByProperty
		{
			[DynamicDependency(nameof(GetDescribedBy))]
			get;
		} = DependencyProperty.RegisterAttached(
				"DescribedBy", typeof(IList<DependencyObject>),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(IList<DependencyObject>)) // TODO: Empty list?
			);

		#endregion

		#region AutomationId

		public static DependencyProperty AutomationIdProperty
		{
			[DynamicDependency(nameof(GetAutomationId))]
			[DynamicDependency(nameof(SetAutomationId))]
			get;
		} = DependencyProperty.RegisterAttached(
			name: "AutomationId",
			propertyType: typeof(string),
			ownerType: typeof(AutomationProperties),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: "",
				propertyChangedCallback: OnAutomationIdChanged)
		);

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
		#endregion

		public static int GetPositionInSet(global::Microsoft.UI.Xaml.DependencyObject element) => (int)element.GetValue(PositionInSetProperty);

		public static void SetPositionInSet(DependencyObject element, int value) => element.SetValue(PositionInSetProperty, value);

		public static DependencyProperty PositionInSetProperty
		{
			[DynamicDependency(nameof(GetPositionInSet))]
			[DynamicDependency(nameof(SetPositionInSet))]
			get;
		} = DependencyProperty.RegisterAttached(
				"PositionInSet", typeof(int),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(-1)); //TODO:MZ: Validate this default value

		public static int GetSizeOfSet(DependencyObject element) => (int)element.GetValue(SizeOfSetProperty);

		public static void SetSizeOfSet(DependencyObject element, int value) => element.SetValue(SizeOfSetProperty, value);

		public static DependencyProperty SizeOfSetProperty
		{
			[DynamicDependency(nameof(GetSizeOfSet))]
			[DynamicDependency(nameof(SetSizeOfSet))]
			get;
		} = DependencyProperty.RegisterAttached(
				"SizeOfSet", typeof(int),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(-1)); //TODO:MZ: Validate this default value

		public static AutomationLandmarkType GetLandmarkType(DependencyObject element) => (AutomationLandmarkType)element.GetValue(LandmarkTypeProperty);

		public static void SetLandmarkType(DependencyObject element, AutomationLandmarkType value) => element.SetValue(LandmarkTypeProperty, value);

		public static DependencyProperty LandmarkTypeProperty
		{
			[DynamicDependency(nameof(GetLandmarkType))]
			[DynamicDependency(nameof(SetLandmarkType))]
			get;
		} = DependencyProperty.RegisterAttached(
				"LandmarkType", typeof(AutomationLandmarkType),
				typeof(AutomationProperties),
				new FrameworkPropertyMetadata(default(AutomationLandmarkType)));

#if __WASM__ || __SKIA__
		internal static string FindHtmlRole(UIElement uIElement)
		{
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Button_Available && uIElement is Button)
			{
				return "button";
			}
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_RadioButton_Available && uIElement is RadioButton)
			{
				return "radio";
			}
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_CheckBox_Available && uIElement is CheckBox)
			{
				return "checkbox";
			}
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBlock_Available && uIElement is TextBlock)
			{
				return "label";
			}
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBox_Available && uIElement is TextBox)
			{
				return "textbox";
			}
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Slider_Available && uIElement is Slider)
			{
				return "slider";
			}

			var peer = uIElement.GetOrCreateAutomationPeer();
			if (peer?.GetAutomationControlType() is { } type)
			{
				return type switch
				{
					AutomationControlType.Button => "button",
					AutomationControlType.RadioButton => "radio",
					AutomationControlType.CheckBox => "checkbox",
					AutomationControlType.Text => "label",
					AutomationControlType.Edit => "label",
					AutomationControlType.Slider => "slider",
					_ => null,
				};
			}

			return null;
		}
#endif

	}
}
