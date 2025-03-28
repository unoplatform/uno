using System;
using Uno;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes RadioButton types to Microsoft UI Automation.
	/// </summary>
	public partial class RadioButtonAutomationPeer : ToggleButtonAutomationPeer, ISelectionItemProvider
	{
		/// <summary>
		/// Initializes a new instance of the RadioButtonAutomationPeer class.
		/// </summary>
		/// <param name="owner"></param>
		public RadioButtonAutomationPeer(RadioButton owner) : base(owner)
		{
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.SelectionItem)
			{
				return this;
			}

			return null;
		}

		protected override string GetClassNameCore() => "RadioButton";

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;

		/// <summary>
		/// Clears any existing selection and then selects the current element.
		/// </summary>
		public void Select()
		{
			var isEnabled = IsEnabled();
			if (!isEnabled)
			{
				return;
			}

			(Owner as RadioButton)?.AutomationRadioButtonOnToggle();
		}

		/// <summary>
		/// Adds the current element to the collection of selected items.
		/// </summary>
		public void AddToSelection()
		{
			var owner = (RadioButton)Owner;
			if (owner.IsChecked == null || !owner.IsChecked.Value)
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Removes the current element from the collection of selected items.
		/// </summary>
		public void RemoveFromSelection()
		{
			var owner = (RadioButton)Owner;
			if (owner.IsChecked == true)
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Gets a value that indicates whether an item is selected.
		/// </summary>
		public bool IsSelected => (Owner as RadioButton)?.IsChecked == true;

		/// <summary>
		/// Gets the UI automation provider that implements ISelectionProvider and acts as the container for the calling object.
		/// </summary>
		public IRawElementProviderSimple SelectionContainer => null;

		// IsSelected Property Changed Event to UIAutomation Clients
		internal void RaiseIsSelectedPropertyChangedEvent(bool bOldValue, bool bNewValue)
		{
			if (bOldValue != bNewValue)
			{
				RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, bOldValue, bNewValue);
			}
		}
	}
}
