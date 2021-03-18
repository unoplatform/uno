// MUX reference PagerControlAutomationPeer.cpp, commit a08f765

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class PagerControlAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{
		public bool CanSelectMultiple => false;

		public bool IsSelectionRequired => true;

		public PagerControlAutomationPeer(PagerControl owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Selection)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(PagerControl);
		}

		protected override string GetNameCore()
		{
			string name = base.GetNameCore();

			if (string.IsNullOrEmpty(name))
			{
				if (Owner is PagerControl pagerControl)
				{
					name = SharedHelpers.TryGetStringRepresentationFromObject(pagerControl.GetValue(AutomationProperties.NameProperty));
				}
			}

			return name;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Menu;
		}

		// TODO: Uno specific - workaround for #4259
		protected override AutomationLandmarkType GetLandmarkTypeCore()
		{
			return AutomationLandmarkType.Navigation;
		}

		private PagerControl GetImpl()
		{
			PagerControl impl = null;

			if (Owner is PagerControl pagerControl)
			{
				impl = pagerControl;
			}

			return impl;
		}

		IRawElementProviderSimple[] ISelectionProvider.GetSelection()
		{
			// TODO: Uno specific - does not match the WinUI version,
			// which returns plain object
			return Array.Empty<IRawElementProviderSimple>();
		}

		internal void RaiseSelectionChanged(double oldIndex, double newIndex)
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty,
					oldIndex,
					newIndex);
			}
		}
	}
}
