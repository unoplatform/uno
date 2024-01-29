using System;
using System.Collections.Generic;
using Uno;
using Uno.UI;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

#if __ANDROID__
using View = Android.Views.ViewGroup;
#elif __IOS__
using View = UIKit.UIView;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using AppKit;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class FrameworkElementAutomationPeer : AutomationPeer
	{
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


		public static global::Microsoft.UI.Xaml.Automation.Peers.AutomationPeer FromElement(global::Microsoft.UI.Xaml.UIElement element)
		{
			if (element is IFrameworkElement fe)
			{
				return FromIFrameworkElement(fe);
			}

			return null;
		}

		public static global::Microsoft.UI.Xaml.Automation.Peers.AutomationPeer CreatePeerForElement(global::Microsoft.UI.Xaml.UIElement element)
		{
			if (element is IFrameworkElement fe)
			{
				return CreatePeerForIFrameworkElement(fe);
			}

			return null;
		}

		private static AutomationPeer CreatePeerForIFrameworkElement(IFrameworkElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			return element.GetAutomationPeer();
		}

		private static AutomationPeer FromIFrameworkElement(IFrameworkElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			return element.GetAutomationPeer();
		}

		protected override string GetLocalizedControlTypeCore()
		{
			if (AutomationProperties.GetLocalizedControlType(Owner) is string localizedControlType && !string.IsNullOrEmpty(localizedControlType))
			{
				return localizedControlType;
			}

			return base.GetLocalizedControlTypeCore();
		}

		protected override AutomationPeer GetLabeledByCore()
		{
			if (AutomationProperties.GetLabeledBy(Owner) is IFrameworkElement label) // TODO: UIElement
			{
				return label.GetAutomationPeer();
			}

			return base.GetLabeledByCore();
		}

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

		/// <inheritdoc />
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			return Owner.GetChildren().OfType<UIElement>().Select(CreatePeerForElement).ToList();
		}

		private string GetSimpleAccessibilityName()
		{
			if (FeatureConfiguration.AutomationPeer.UseSimpleAccessibility
			&& Owner is View view
			&& AutomationProperties.GetAccessibilityView(Owner) != AccessibilityView.Raw)
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

		protected override bool IsEnabledCore()
		{
			if (Owner is Control control)
			{
				return control.IsEnabled;
			}

			return true;
		}

		protected override void SetFocusCore()
		{
			if (Owner is Control control)
			{
				control.Focus(FocusState.Programmatic);
			};
		}

		protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationProperties.GetLandmarkType(Owner);

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
			bool isPopupOpen = true;

			var childIsAcceptable = element != null;
			if (element != null)
			{
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
	}
}
