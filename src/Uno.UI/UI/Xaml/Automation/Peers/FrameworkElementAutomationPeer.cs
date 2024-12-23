// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FrameworkElementAutomationPeer_Partial.cpp, tag winui3/release/1.5.3

using System;
using System.Collections.Generic;
using Uno.UI;
using DirectUI;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Rect = Windows.Foundation.Rect;
using Uno.UI.Xaml.Core.Scaling;



#if __ANDROID__
using View = Android.Views.ViewGroup;
#elif __IOS__
using View = UIKit.UIView;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using AppKit;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Automation.Peers;

public partial class FrameworkElementAutomationPeer : AutomationPeer
{
	private string m_LocalizedControlType;
	private string m_ClassName;
	private ScrollItemAdapter m_spScrollItemAdapter;

	private AutomationControlType m_ControlType;

	public UIElement Owner { get; }

	public FrameworkElementAutomationPeer() { }

	public FrameworkElementAutomationPeer(object element)
	{
		Owner = element as UIElement;
	}

	public FrameworkElementAutomationPeer(FrameworkElement owner)
	{
		Owner = owner;
	}
	protected override string GetAcceleratorKeyCore()
	{
		if (Owner != null)
		{
			return AutomationProperties.GetAcceleratorKey(Owner);
		}

		return null;
	}

	protected override string GetAccessKeyCore()
	{
		// Check to see if the value is unset
		var value = (Owner as UIElement).ReadLocalValue(AutomationProperties.AccessKeyProperty);
		DependencyPropertyFactory.IsUnsetValue(value, out var isUnset);

		// If value is unset, then fallback to the AccessKey property on Framework Element
		if (isUnset)
		{
			return AccessKeyStringBuilder.GetAccessKeyMessageFromElement(Owner as FrameworkElement);
		}

		// Find the value normally
		return AutomationProperties.GetAccessKey(Owner);
	}

	protected override string GetAutomationIdCore()
	{
		var automationId = AutomationProperties.GetAutomationId(Owner);

		if (string.IsNullOrEmpty(automationId))
		{
			//UNO TODO: Implement GetAutomationIdHelper on AutomationPeer

			//Owner.GetAutomationIdHelper(out var strAutomationId);

			//return strAutomationId;
		}

		return automationId;
	}

	//UNO TODO: GetAutomationControlType should be protected override
	internal new AutomationControlType GetAutomationControlType()
	{
		// If AutomationProperties.AutomationControlType is set, we'll return that value.
		// Otherwise, we'll fall back to the GetAutomationControlTypeCore override.

		// UNO TODO: Use IsPropertyDefault instead of GetCurrentHighestValuePrecedence
		if (Owner.GetCurrentHighestValuePrecedence(AutomationProperties.AutomationControlTypeProperty) != DependencyPropertyValuePrecedences.DefaultValue)
		{
			return AutomationProperties.GetAutomationControlType(Owner);
		}
		else
		{
			return base.GetAutomationControlType();
		}
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> m_ControlType;

	protected override Rect GetBoundingRectangleCore()
	{
		Rect rect = default;

		var isOneCoreTransforms = IsEnabled();

		// In OneCoreTransforms mode, we ignore the clip on the all CScrollContentPresenters for the magnifier. This is
		// needed because Santorini's Magnifier places a RenderTransform on itself to do the magnification, which will
		// push parts of the shell (which lives underneath the Magnifier in the tree) beyond the bounds of the window.
		// Those parts still need to report bounds in order to be accessed by UIA and be scrolled back into view by the
		// shell.
		//
		// Note that ignoring the root CScrollContentPresenter clip alone does not guarantee non-zero bounds to be
		// returned. The window size could have been given to layout and could have caused layout clips to be applied
		// in the tree. This works for Magnifier because the Magnifier control uses only a RenderTransform for
		// magnification, which does not affect layout at all.
		//
		// Also note that we still respect the root CScrollContentPresenter clip for IsOffscreen (see IsOffscreenCore).
		// GetBoundingRectangle is not required to clip the bounds to the window, but IsOffscreen needs to remain accurate.
		// https://docs.microsoft.com/en-us/windows/desktop/api/uiautomationcore/nf-uiautomationcore-irawelementproviderfragment-get_boundingrectangle
		if (!IsOffscreenHelper(isOneCoreTransforms))
		{
			var bounds = Owner.GetGlobalBoundsWithOptions(
				false /* ignoreClipping */,
				isOneCoreTransforms,
				false /* useTargetInformation */);

			rect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
		}

		if (isOneCoreTransforms)
		{
			// In OneCoreTransforms mode, GetGlobalBounds returns logical pixels so we must convert to RasterizedClient
			var scale = RootScale.GetRasterizationScaleForElement(GetRootNoRef());
			return new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
		}

		return rect;
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var children = new List<AutomationPeer>();
		GetAutomationPeerChildren(Owner, children);
		return children;
	}

	private void GetAutomationPeerChildren(UIElement element, List<AutomationPeer> children)
	{
		//UNO TODO: Properly implement GetAutomationPeerChildren on FrameworkElementAutomationPeer
		//Temporarily disabled as android, ios, macos doesn't use UIElement

#if !__ANDROID__ && !__IOS__ && !__MACOS__
		var childCount = element.GetChildren().Count;
		if (childCount > 0)
		{
			var reverseOrder = element.AreAutomationPeerChildrenReversed();
			for (var nIndex = reverseOrder ? childCount - 1 : 0; reverseOrder ? nIndex >= 0 : nIndex < childCount; nIndex += reverseOrder ? -1 : 1)
			{
				var spChild = element.GetChildren()[nIndex];
				var childIsAcceptable = ChildIsAcceptable(spChild);

				if (childIsAcceptable)
				{
					var spChildAP = spChild.GetOrCreateAutomationPeer();
					if (spChildAP != null)
					{
						children.Add(spChildAP);
					}
					else
					{
						GetAutomationPeerChildren(spChild, children);
					}
				}
			}
		}
#endif
	}

	internal IList<AutomationPeer> GetAutomationPeersForChildrenOfElement(UIElement element)
	{
		var automationPeers = new List<AutomationPeer>();

		GetAutomationPeerChildren(element, automationPeers);

		return automationPeers;
	}

	protected override string GetClassNameCore() => m_ClassName;

	protected override Point GetClickablePointCore()
		=> Owner.GetClickablePointRasterizedClient();

	protected override string GetHelpTextCore()
		=> AutomationProperties.GetHelpText(Owner);

	protected override string GetItemStatusCore()
		=> AutomationProperties.GetItemStatus(Owner);

	protected override string GetItemTypeCore()
		=> AutomationProperties.GetItemType(Owner);

	protected override AutomationPeer GetLabeledByCore()
	{
		if (AutomationProperties.GetLabeledBy(Owner) is IFrameworkElement label)
		// UNO TODO: Implement GetAutomationPeer on UIElement
		{
			return label.GetAutomationPeer();
		}

		return base.GetLabeledByCore();
	}

	//UNO TODO: GetLocalizedControlType should be protected override
	internal new string GetLocalizedControlType()
	{
		//If AutomationProperties.LocalizedControlType is set, we'll return that value.
		//Otherwise, we'll fall back to the GetLocalizedControlTypeCore override.

		// UNO TODO: Use IsPropertyDefault instead of GetCurrentHighestValuePrecedence
		if (Owner.GetCurrentHighestValuePrecedence(AutomationProperties.LocalizedControlTypeProperty) != DependencyPropertyValuePrecedences.DefaultValue)
		{
			// If set, return the value from AutomationProperties
			return AutomationProperties.GetLocalizedControlType(Owner);
		}
		else
		{
			// Otherwise, fallback to the base class implementation
			return base.GetLocalizedControlType();
		}
	}

	protected override string GetLocalizedControlTypeCore()
	{
		if (AutomationProperties.GetLocalizedControlType(Owner) is string localizedControlType && !string.IsNullOrEmpty(localizedControlType))
		{
			return localizedControlType;
		}

		return base.GetLocalizedControlTypeCore();
	}

	protected override bool IsRequiredForFormCore()
		=> AutomationProperties.GetIsRequiredForForm(Owner);

	protected override string GetNameCore()
	{
		if (AutomationProperties.GetName(Owner) is string name && !string.IsNullOrEmpty(name))
		{
			return name;
		}

		if (GetLabeledBy() is AutomationPeer labelAutomationPeer && labelAutomationPeer.GetName() is string label && !string.IsNullOrEmpty(label))
		{
			return label;
		}

		if (GetSimpleAccessibilityName() is string simpleAccessibilityName && !string.IsNullOrEmpty(simpleAccessibilityName))
		{
			return simpleAccessibilityName;
		}

		if ((Owner as FrameworkElement)?.GetAccessibilityInnerText() is string innerText && !string.IsNullOrEmpty(innerText))
		{
			return innerText;
		}

		return base.GetNameCore();
	}

	internal object GetDefaultPattern(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ScrollItem)
		{
			return m_spScrollItemAdapter ??= new ScrollItemAdapter(this);
		}

		return null;
	}

	protected override AutomationLiveSetting GetLiveSettingCore()
		=> AutomationProperties.GetLiveSetting(Owner);

	protected override int GetPositionInSetCore()
		=> AutomationProperties.GetPositionInSet(Owner);

	protected override int GetSizeOfSetCore()
		=> AutomationProperties.GetSizeOfSet(Owner);

	protected override int GetLevelCore()
		=> AutomationProperties.GetLevel(Owner);

	protected override AutomationLandmarkType GetLandmarkTypeCore()
		=> AutomationProperties.GetLandmarkType(Owner);

	protected override string GetLocalizedLandmarkTypeCore()
		=> AutomationProperties.GetLocalizedLandmarkType(Owner);

	protected override bool IsContentElementCore()
		=> AutomationProperties.GetAccessibilityView(Owner) == AccessibilityView.Content;

	protected override bool IsControlElementCore()
	{
		var accessibilityView = AutomationProperties.GetAccessibilityView(Owner);

		// Content view is subset of Control View so anything which is in Content View will have ControlElement as True
		return (accessibilityView == AccessibilityView.Control || accessibilityView == AccessibilityView.Content);
	}

	protected override bool IsEnabledCore()
	{
		if (Owner is Control control)
		{
			return control.IsEnabled;
		}

		return true;
	}

	protected override bool IsKeyboardFocusableCore()
		=> IsKeyboardFocusableHelper();

	protected override bool IsOffscreenCore()
		=> IsOffscreenHelper(false /* ignoreClippingOnScrollContentPresenters */);

	protected override void SetFocusCore()
	{
		if (Owner is Control control)
		{
			control.Focus(FocusState.Programmatic);
		};
	}

	protected override void ShowContextMenuCore()
	{
		if (Owner is Control control)
		{
			control.ContextFlyout?.ShowAt(control);
		}
	}

	protected override bool IsPeripheralCore()
		=> AutomationProperties.GetIsPeripheral(Owner);

	protected override bool IsDataValidForFormCore()
		=> AutomationProperties.GetIsDataValidForForm(Owner);

	protected override string GetFullDescriptionCore()
		=> AutomationProperties.GetFullDescription(Owner);

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
		=> GetAutomationPeerCollection(AutomationProperties.DescribedByProperty);

	protected override IEnumerable<AutomationPeer> GetFlowsToCore()
		=> GetAutomationPeerCollection(AutomationProperties.FlowsToProperty);

	protected override IEnumerable<AutomationPeer> GetFlowsFromCore()
		=> GetAutomationPeerCollection(AutomationProperties.FlowsFromProperty);

	protected override AutomationHeadingLevel GetHeadingLevelCore()
		=> AutomationProperties.GetHeadingLevel(Owner);

	protected override bool IsDialogCore()
		=> AutomationProperties.GetIsDialog(Owner);

	protected override IReadOnlyList<AutomationPeer> GetControlledPeersCore()
	{
		var peers = new List<AutomationPeer>();
		// AutomationProperties deals with UIElements but the peer world wants to work in AutomationPeers.
		// Here we do the translation.

		var elements = AutomationProperties.GetControlledPeers(Owner as UIElement);

		if (elements != null)
		{
			foreach (var element in elements)
			{
				if (element != null)
				{
					var automationPeer = element.GetOrCreateAutomationPeer();
					if (automationPeer != null)
					{
						peers.Add(automationPeer);
					}
				}
			}
		}

		return peers;
	}

	internal IReadOnlyList<AutomationPeerAnnotation> GetAnnotationsCoreImpl()
	{
		var annotations = new List<AutomationPeerAnnotation>();

		// AutomationProperties deals with UIElements but the peer world wants to work in AutomationPeers.
		// Here we do the translation.

		var uiElementAnnotations = AutomationProperties.GetAnnotations(Owner);
		if (uiElementAnnotations != null)
		{
			if (uiElementAnnotations.Count > 0)
			{
				for (var i = 0; i < uiElementAnnotations.Count; ++i)
				{
					var uiElementAnnotation = uiElementAnnotations[i];
					var apAnnotation = new AutomationPeerAnnotation();

					// Retrieve information from the AutomationAnnotation
					var type = uiElementAnnotation.Type;
					var uie = uiElementAnnotation.Element;

					// Get or create AutomationPeer for the UIElement
					AutomationPeer ap = null;
					if (uie != null)
					{
						ap = uie.GetOrCreateAutomationPeer();
					}

					// Set properties of AutomationPeerAnnotation
					apAnnotation.Type = type;
					apAnnotation.Peer = ap;

					annotations.Add(apAnnotation);
				}
			}
		}

		return annotations;
	}

	internal void SetControlType(AutomationControlType controlType)
	{
		m_ControlType = controlType;
	}

	internal void SetLocalizedControlType(string localizedControlType)
	{
		m_LocalizedControlType = localizedControlType;
	}

	internal void SetClassName(string className)
	{
		m_ClassName = className;
	}

	public static AutomationPeer FromElement(UIElement element)
	{
		if (element is IFrameworkElement fe)
		{
			return FromIFrameworkElement(fe);
		}

		return null;
	}

	public static AutomationPeer CreatePeerForElement(UIElement element)
	{
		if (element is IFrameworkElement fe)
		{
			return CreatePeerForIFrameworkElement(fe);
		}

		return null;
	}

	private static AutomationPeer CreatePeerForIFrameworkElement(IFrameworkElement element)
	{
		if (element is not { })
		{
			throw new ArgumentNullException(nameof(element));
		}

		return element.GetAutomationPeer();
	}

	private static AutomationPeer FromIFrameworkElement(IFrameworkElement element)
	{
		if (element is not { })
		{
			throw new ArgumentNullException(nameof(element));
		}

		return element.GetAutomationPeer();
	}

	/// <summary>
	/// Virtual helper method which provide ability for any specific Automation peers
	/// do not allows including Automation peer of child elements' in to the Automation
	/// peer tree.
	/// </summary>
	/// <remarks>
	/// We don't accept nonUI or null elements by default.
	/// </remarks>
	/// <param name="child">
	/// Child element to be decided to include it to
	/// Automation peer's tree.
	/// </param>
	/// <returns>True if the child element is acceptable.</returns>
	private protected virtual bool ChildIsAcceptable(UIElement element)
	{
		var childIsAcceptable = element != null;

		if (element is { })
		{
			var isPopupOpen = true;

			if (element is Popup popup)
			{
				isPopupOpen = popup.IsOpen;
			}
			var value = element.Visibility;

			// this condition checks that if Control is visible and if it's popup then it must be open
			childIsAcceptable = isPopupOpen && value == Visibility.Visible;
		}

		return childIsAcceptable;
	}

	internal IReadOnlyList<AutomationPeer> GetAutomationPeerCollection(DependencyProperty eProperty)
	{
		var peers = new List<AutomationPeer>();

		if (Owner is { })
		{
			IList<DependencyObject> elements = null;

			if (eProperty == AutomationProperties.DescribedByProperty)
			{
				elements = AutomationProperties.GetDescribedBy(Owner);
			}
			else if (eProperty == AutomationProperties.FlowsToProperty)
			{
				elements = AutomationProperties.GetFlowsTo(Owner);
			}
			else if (eProperty == AutomationProperties.FlowsFromProperty)
			{
				elements = AutomationProperties.GetFlowsFrom(Owner);
			}

			if (elements.Count > 0)
			{
				for (var i = 0; i < elements.Count; ++i)
				{
					var ap = (elements[i] as UIElement).GetOrCreateAutomationPeer();
					peers.Add(ap);
				}
			}
		}

		return peers;
	}

	private string GetSimpleAccessibilityName()
	{
		if (FeatureConfiguration.AutomationPeer.UseSimpleAccessibility
		&& Owner is View view
		&& AutomationProperties.GetAccessibilityView(Owner) is not AccessibilityView.Raw)
		{
			/// We get our name by aggregating the name of all our children.
			/// See <see cref="FeatureConfiguration.AutomationPeer.UseSimpleAccessibility" /> for details.
			return string.Join(", ", view
				.EnumerateAllChildren()
				.OfType<IFrameworkElement>()
				.Where(child => child.Visibility == Visibility.Visible)
				.Select(child =>
				{
					// We set this for two reasons:
					// - We want to disable accessibility focus for elements whose names are aggregated into the name of their parent.
					// - We want to prevent these elements from enumerating their own children in GetSimpleAccessibilityName(),
					//	 which might be called as a result of calling automationPeer.GetName() below.
					AutomationProperties.SetAccessibilityView(child, AccessibilityView.Raw);
					return child;
				})
				.Select(FromIFrameworkElement)
				.Where(automationPeer => automationPeer != null)
				.Select(automationPeer => automationPeer.GetName())
				.Where(childName => !string.IsNullOrEmpty(childName))
			);
		}
		else
		{
			return null;
		}
	}
}
