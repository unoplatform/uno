using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer : DependencyObject
	{
		private AutomationPeer _parent;

		[Uno.NotImplemented]
		public static bool ListenerExists(Microsoft.UI.Xaml.Automation.Peers.AutomationEvents eventId) => false;

		#region Public

		public AutomationPeer EventsSource { get; set; } // TODO Uno: Implement properly.

		public object GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface patternInterface) => GetPatternCore(patternInterface);

		public void SetParent(global::Microsoft.UI.Xaml.Automation.Peers.AutomationPeer peer) => _parent = peer;

		public global::Microsoft.UI.Xaml.Automation.Peers.AutomationPeer GetParent() => _parent;

		public string GetAcceleratorKey() => GetAcceleratorKeyCore();

		public string GetAccessKey() => GetAcceleratorKeyCore();

		public string GetAutomationId() => GetAutomationIdCore();

		public Rect GetBoundingRectangle() => GetBoundingRectangleCore();

		public IList<AutomationPeer> GetChildren() => GetChildrenCore();

		public Point GetClickablePoint() => GetClickablePointCore();

		public string GetHelpText() => GetHelpTextCore();

		public string GetItemStatus() => GetItemStatusCore();

		public string GetItemType() => GetItemTypeCore();

		public AutomationOrientation GetOrientation() => GetOrientationCore();

		public bool HasKeyboardFocus() => HasKeyboardFocusCore();

		public bool IsKeyboardFocusable() => IsKeyboardFocusableCore();

		public bool IsOffscreen() => IsOffscreenCore();

		public bool IsRequiredForForm() => IsRequiredForFormCore();

		public AutomationPeer GetPeerFromPoint(Point point) => GetPeerFromPointCore(point);

		public AutomationLiveSetting GetLiveSetting() => GetLiveSettingCore();

		public object Navigate(Microsoft.UI.Xaml.Automation.Peers.AutomationNavigationDirection direction) => NavigateCore(direction);

		public object GetElementFromPoint(Point pointInWindowCoordinates) => GetElementFromPointCore(pointInWindowCoordinates);

		public object GetFocusedElement() => GetFocusedElementCore();

		public void ShowContextMenu() => ShowContextMenuCore();

		public IReadOnlyList<AutomationPeer> GetControlledPeers() => GetControlledPeersCore();

		public IList<AutomationPeerAnnotation> GetAnnotations() => GetAnnotationsCore();

		public int GetPositionInSet() => GetPositionInSetCore();

		public int GetSizeOfSet() => GetSizeOfSetCore();

		public int GetLevel() => GetLevelCore();

		public AutomationLandmarkType GetLandmarkType() => GetLandmarkTypeCore();

		public string GetLocalizedLandmarkType() => GetLocalizedLandmarkTypeCore();

		public bool IsPeripheral() => IsPeripheralCore();

		public bool IsDataValidForForm() => IsDataValidForFormCore();

		public string GetFullDescription() => GetFullDescriptionCore();

		public AutomationHeadingLevel GetHeadingLevel() => GetHeadingLevelCore();

		public bool IsDialog() => IsDialogCore();

		public bool IsContentElement() => IsContentElementCore();

		public bool IsControlElement() => IsControlElementCore();

		public bool IsEnabled() => IsEnabledCore();

		public bool IsPassword() => IsPasswordCore();

		public void SetFocus() => SetFocusCore();

		public string GetClassName() => GetClassNameCore();

		public AutomationControlType GetAutomationControlType() => GetAutomationControlTypeCore();

		public string GetLocalizedControlType() => GetLocalizedControlTypeCore();

		public string GetName() => GetNameCore();

		public AutomationPeer GetLabeledBy() => GetLabeledByCore();

		protected internal IRawElementProviderSimple ProviderFromPeer(AutomationPeer peer) => new IRawElementProviderSimple(peer);

		#endregion

		#region Overrides





		

		


		protected virtual AutomationPeer GetPeerFromPointCore(Point point) => this;

		

		

		protected virtual object NavigateCore(Microsoft.UI.Xaml.Automation.Peers.AutomationNavigationDirection direction) => null;

		

		protected virtual object GetElementFromPointCore(Point pointInWindowCoordinates) => this;

		

		

		

		

		

		

		protected virtual bool IsDataValidForFormCore() => true;

		

		protected virtual AutomationHeadingLevel GetHeadingLevelCore() => AutomationHeadingLevel.None;

		


		

		

		protected virtual string GetLocalizedControlTypeCore() => LocalizeControlType(GetAutomationControlType());


		
		protected virtual void SetFocusCore()
		{
		}


		#endregion

		#region Private

		//UNO TODO: Implement GetRootNoRef on AutomationPeer
		internal DependencyObject GetRootNoRef()
		{
			return null;
		}

		//UNO TODO: Check the implementations of IsKeyboardFocusableHelper and IsOffscreenHelper
		internal bool IsKeyboardFocusableHelper()
			=> false;

		internal bool IsOffscreenHelper(bool ignoreClippingOnScrollContentPresenters)
			=> false;

		private static string LocalizeControlType(AutomationControlType controlType) =>
			// TODO: Humanize ("AppBarButton" -> "app bar button")
			// TODO: Localize
			Enum.GetName<AutomationControlType>(controlType).ToLowerInvariant();

		internal bool InvokeAutomationPeer()
		{
			// TODO: Add support for ComboBox, Slider, CheckBox, ToggleButton, RadioButton, ToggleSwitch, Selector, etc.
			if (this is IInvokeProvider invokeProvider)
			{
				invokeProvider.Invoke();
				return true;
			}
			else if (this is IToggleProvider toggleProvider)
			{
				toggleProvider.Toggle();
				return true;
			}
			else if (this is ISelectionItemProvider selectionItemProvider)
			{
				selectionItemProvider.Select();
				return true;
			}

			return false;
		}

		internal static void RaiseEventIfListener(DependencyObject target, AutomationEvents eventId) => ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "RaiseEventIfListener");

		#endregion

		#region NotImplemented

		[Uno.NotImplemented]
		public static bool ListenerfExists(AutomationEvents eventId)
		{
			ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "bool AutomationPeer.ListenerExists");
			return false;
		}

		[Uno.NotImplemented]
		public void InvalidatePeer()
		{
		}

		[global::Uno.NotImplemented]
		public void RaiseAutomationEvent(global::Microsoft.UI.Xaml.Automation.Peers.AutomationEvents eventId)
		{
			ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaiseAutomationEvent(AutomationEvents eventId)", LogLevel.Warning);
		}

		[global::Uno.NotImplemented]
		public void RaiseNotificationEvent(global::Microsoft.UI.Xaml.Automation.Peers.AutomationNotificationKind notificationKind, global::Microsoft.UI.Xaml.Automation.Peers.AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
		{
			ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)", LogLevel.Warning);
		}

#if !__SKIA__
		[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
		public void RaisePropertyChangedEvent(global::Microsoft.UI.Xaml.Automation.AutomationProperty automationProperty, object oldValue, object newValue)
		{
			ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaisePropertyChangedEvent(AutomationProperty automationProperty, object oldValue, object newValue)", LogLevel.Warning);
		}
#endif
		#endregion
	}
}
