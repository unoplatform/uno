using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ComboBoxItemAutomationPeer : FrameworkElementAutomationPeer
{
	public ComboBoxItemAutomationPeer(ComboBoxItem owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(ComboBoxItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (TryGetFaceplateOwner(out var faceplate))
		{
			return GetAutomationPeersForChildrenOfElement(faceplate);
		}

		return base.GetChildrenCore();
	}

	protected override bool IsOffscreenCore()
	{
		if (TryGetParentComboBox(out var comboBox) && !comboBox.IsDropDownOpen)
		{
			return !OwnerComboBoxItem.IsSelected;
		}

		return base.IsOffscreenCore();
	}

	protected override Rect GetBoundingRectangleCore()
	{
		if (TryGetParentComboBox(out var comboBox) && !comboBox.IsDropDownOpen)
		{
			if (OwnerComboBoxItem.IsSelected)
			{
				var contentPresenter = comboBox.GetContentPresenterPart();
				if (contentPresenter is { })
				{
					var faceplatePeer = FrameworkElementAutomationPeer.FromElement(contentPresenter)
						?? FrameworkElementAutomationPeer.CreatePeerForElement(contentPresenter);

					if (faceplatePeer is not null)
					{
						return faceplatePeer.GetBoundingRectangle();
					}
				}

				return Rect.Empty;
			}

			return Rect.Empty;
		}

		return base.GetBoundingRectangleCore();
	}

	private bool TryGetFaceplateOwner(out UIElement faceplate)
	{
		faceplate = null;
		if (TryGetParentComboBox(out var comboBox)
			&& !comboBox.IsDropDownOpen
			&& OwnerComboBoxItem.IsSelected
			&& comboBox.GetContentPresenterPart() is UIElement presenter)
		{
			faceplate = presenter;
			return true;
		}

		return false;
	}

	private bool TryGetParentComboBox(out ComboBox comboBox)
	{
		comboBox = ItemsControl.ItemsControlFromItemContainer(OwnerComboBoxItem) as ComboBox;
		return comboBox is not null;
	}

	private ComboBoxItem OwnerComboBoxItem => (ComboBoxItem)Owner;
}
