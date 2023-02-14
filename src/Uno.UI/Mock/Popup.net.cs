using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class Popup
	{
		partial void InitializePartial()
		{
			PopupPanel = new PopupPanel(this);
		}

		partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen)
		{
			if (newIsOpen)
			{
				PopupPanel.Visibility = Visibility.Visible;
			}
			else
			{
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		partial void OnChildChangedPartialNative(UIElement oldChild, UIElement newChild)
		{
			PopupPanel.Children.Remove(oldChild);
			PopupPanel.Children.Add(newChild);
		}

		partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
		{
			previousPanel?.Children.Clear();

			if (PopupPanel != null)
			{
				if (Child != null)
				{
					PopupPanel.Children.Add(Child);
				}
			}
		}
	}
}
