using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		partial void InitializePartial()
		{
			PopupPanel = new PopupPanel(this);
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);
			if (newIsOpen)
			{
				PopupPanel.Visibility = Visibility.Visible;
			}
			else
			{
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		protected override void OnChildChanged(FrameworkElement oldChild, FrameworkElement newChild)
		{
			base.OnChildChanged(oldChild, newChild);

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
