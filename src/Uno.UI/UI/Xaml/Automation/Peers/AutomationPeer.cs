using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Provider;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class AutomationPeer : DependencyObject
	{
		[global::Uno.NotImplemented]
		public static bool ListenerExists(global::Windows.UI.Xaml.Automation.Peers.AutomationEvents eventId)
		{
			return false;
		}

		#region Public

		public bool IsContentElement()
		{
			return IsContentElementCore();
		}

		public bool IsControlElement()
		{
			return IsControlElementCore();
		}

		public bool IsEnabled()
		{
			return IsEnabledCore();
		}
		
		public bool IsPassword()
		{
			return IsPasswordCore();
		}
		
		public void SetFocus()
		{
			SetFocusCore();
		}
				
		public string GetClassName()
		{
			return GetClassNameCore();
		}

		public AutomationControlType GetAutomationControlType()
		{
			return GetAutomationControlTypeCore();
		}

		public string GetLocalizedControlType()
		{
			return GetLocalizedControlTypeCore();
		}

		public string GetName()
		{
			return GetNameCore();
		}
		
		public AutomationPeer GetLabeledBy()
		{
			return GetLabeledByCore();
		}
		
		#endregion

		#region Overrides

		protected virtual bool IsContentElementCore()
		{
			return false;
		}

		protected virtual bool IsControlElementCore()
		{
			return false;
		}

		protected virtual bool IsEnabledCore()
		{
			return true;
		}

		protected virtual string GetClassNameCore()
		{
			return "";
		}

		protected virtual string GetNameCore()
		{
			return "";
		}

		protected virtual string GetLocalizedControlTypeCore()
		{
			return LocalizeControlType(GetAutomationControlType());
		}

		protected virtual AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Custom;
		}
		
		protected virtual bool IsPasswordCore()
		{
			return false;
		}
		
		protected virtual void SetFocusCore()
		{
		}

		protected virtual AutomationPeer GetLabeledByCore()
		{
			return null;
		}
		
		protected virtual IEnumerable<AutomationPeer> GetDescribedByCore()
		{
			return null;
		}

		#endregion

		#region Private

		private static string LocalizeControlType(AutomationControlType controlType)
		{
			// TODO: Humanize ("AppBarButton" -> "app bar button")
			// TODO: Localize
			return Enum.GetName(typeof(AutomationControlType), controlType);
		}

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

		internal static void RaiseEventIfListener(DependencyObject target, AutomationEvents eventId)
		{
			Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "RaiseEventIfListener");
		}

		#endregion

		#region NotImplemented

		[Uno.NotImplemented]
		public static bool ListenerfExists(AutomationEvents eventId)
		{
			Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "bool AutomationPeer.ListenerExists");
			return false;
		}

		[Uno.NotImplemented]
		public void InvalidatePeer()
		{
		}

		#endregion
	}
}
