using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer : DependencyObject
	{
		private AutomationPeer _parent;

		[Uno.NotImplemented]
		public static bool ListenerExists(Windows.UI.Xaml.Automation.Peers.AutomationEvents eventId) => false;

		#region Public

		public AutomationPeer EventsSource { get; set; } // TODO Uno: Implement properly.

		public object GetPattern(Windows.UI.Xaml.Automation.Peers.PatternInterface patternInterface) => GetPatternCore(patternInterface);

		public void SetParent(global::Windows.UI.Xaml.Automation.Peers.AutomationPeer peer) => _parent = peer;

		public global::Windows.UI.Xaml.Automation.Peers.AutomationPeer GetParent() => _parent;

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

		public object Navigate(Windows.UI.Xaml.Automation.Peers.AutomationNavigationDirection direction) => NavigateCore(direction);

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

		#endregion

		#region Overrides

		protected virtual object GetPatternCore(Windows.UI.Xaml.Automation.Peers.PatternInterface patternInterface) => null;

		protected virtual string GetAcceleratorKeyCore() => string.Empty;

		protected virtual string GetAccessKeyCore() => string.Empty;

		protected virtual string GetAutomationIdCore() => string.Empty;

		protected virtual Rect GetBoundingRectangleCore() => default;

		protected virtual IList<AutomationPeer> GetChildrenCore() => null;

		protected virtual Point GetClickablePointCore() => default;

		protected virtual string GetHelpTextCore() => string.Empty;

		protected virtual string GetItemStatusCore() => string.Empty;

		protected virtual string GetItemTypeCore() => string.Empty;

		protected virtual AutomationOrientation GetOrientationCore() => AutomationOrientation.None;

		protected virtual bool HasKeyboardFocusCore() => false;

		protected virtual bool IsKeyboardFocusableCore() => false;

		protected virtual bool IsOffscreenCore() => false;

		protected virtual bool IsRequiredForFormCore() => false;

		protected virtual AutomationPeer GetPeerFromPointCore(Point point) => this;

		protected virtual AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Off;

		protected virtual void ShowContextMenuCore()
		{
		}

		protected virtual object NavigateCore(Windows.UI.Xaml.Automation.Peers.AutomationNavigationDirection direction) => null;

		protected virtual IReadOnlyList<AutomationPeer> GetControlledPeersCore() => null;

		protected virtual object GetElementFromPointCore(Point pointInWindowCoordinates) => this;

		protected virtual object GetFocusedElementCore() => this;

		protected virtual IList<AutomationPeerAnnotation> GetAnnotationsCore() => null;

		protected virtual int GetPositionInSetCore() => -1;

		protected virtual int GetSizeOfSetCore() => -1;

		protected virtual int GetLevelCore() => -1;

		protected virtual AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.None;

		protected virtual string GetLocalizedLandmarkTypeCore() => string.Empty;

		protected virtual bool IsPeripheralCore() => false;

		protected virtual bool IsDataValidForFormCore() => true;

		protected virtual string GetFullDescriptionCore() => string.Empty;

		protected virtual AutomationHeadingLevel GetHeadingLevelCore() => AutomationHeadingLevel.None;

		protected virtual bool IsDialogCore() => false;

		protected virtual bool IsContentElementCore() => false;

		protected virtual bool IsControlElementCore() => false;

		protected virtual bool IsEnabledCore() => true;

		protected virtual string GetClassNameCore() => "";

		protected virtual string GetNameCore() => "";

		protected virtual string GetLocalizedControlTypeCore() => LocalizeControlType(GetAutomationControlType());

		protected virtual AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

		protected virtual bool IsPasswordCore() => false;

		protected virtual void SetFocusCore()
		{
		}

		protected virtual AutomationPeer GetLabeledByCore() => null;

		protected virtual IEnumerable<AutomationPeer> GetDescribedByCore() => null;

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
			Enum.GetName(typeof(AutomationControlType), controlType).ToLowerInvariant();

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

			return false;
		}

		internal static void RaiseEventIfListener(DependencyObject target, AutomationEvents eventId) => ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "RaiseEventIfListener");

		#endregion

		#region NotImplemented

		[Uno.NotImplemented]
		public static bool ListenerfExists(AutomationEvents eventId)
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "bool AutomationPeer.ListenerExists");
			return false;
		}

		[Uno.NotImplemented]
		public void InvalidatePeer()
		{
		}

		// This is here to make the method internal!
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected internal global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple ProviderFromPeer(global::Windows.UI.Xaml.Automation.Peers.AutomationPeer peer)
		{
			// Uno TODO: Properly implement this.
			return new();
		}

		[global::Uno.NotImplemented]
		public void RaiseAutomationEvent(global::Windows.UI.Xaml.Automation.Peers.AutomationEvents eventId)
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaiseAutomationEvent(AutomationEvents eventId)", LogLevel.Warning);
		}

		[global::Uno.NotImplemented]
		public void RaisePropertyChangedEvent(global::Windows.UI.Xaml.Automation.AutomationProperty automationProperty, object oldValue, object newValue)
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaisePropertyChangedEvent(AutomationProperty automationProperty, object oldValue, object newValue)", LogLevel.Warning);
		}
		#endregion
	}
}
