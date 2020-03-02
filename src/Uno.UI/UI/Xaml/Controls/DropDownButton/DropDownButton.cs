using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class DropDownButton : Button
	{
		public DropDownButton()
		{
			DefaultStyleKey = typeof(PersonPicture);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new DropDownButtonAutomationPeer(this);
		}

		//  - Missing
		//	m_flyoutPropertyChangedRevoker = RegisterPropertyChanged(*this, winrt::Button::FlyoutProperty(), { this, &DropDownButton::OnFlyoutPropertyChanged });

		//   RegisterFlyoutEvents();
		//}

		//void DropDownButton::RegisterFlyoutEvents()
		//{
		//	m_flyoutOpenedRevoker.revoke();
		//	m_flyoutClosedRevoker.revoke();

		//	if (Flyout())
		//	{
		//		m_flyoutOpenedRevoker = Flyout().Opened(winrt::auto_revoke, { this, &DropDownButton::OnFlyoutOpened });
		//		m_flyoutClosedRevoker = Flyout().Closed(winrt::auto_revoke, { this, &DropDownButton::OnFlyoutClosed });
		//	}
		//}

		//bool DropDownButton::IsFlyoutOpen()
		//{
		//	return m_isFlyoutOpen;
		//};

		//void DropDownButton::OpenFlyout()
		//{
		//	if (auto flyout = Flyout())
		//   {
		//		flyout.ShowAt(*this);
		//	}
		//}

		//void DropDownButton::CloseFlyout()
		//{
		//	if (auto flyout = Flyout())
		//   {
		//		flyout.Hide();
		//	}
		//}

		//void DropDownButton::OnFlyoutPropertyChanged(const winrt::DependencyObject& sender, const winrt::DependencyProperty& args)
		//{
		//	RegisterFlyoutEvents();
		//}

		//void DropDownButton::OnFlyoutOpened(const winrt::IInspectable& sender, const winrt::IInspectable& args)
		//{
		//	m_isFlyoutOpen = true;
		//	SharedHelpers::RaiseAutomationPropertyChangedEvent(*this, winrt::ExpandCollapseState::Collapsed, winrt::ExpandCollapseState::Expanded);
		//}

		//void DropDownButton::OnFlyoutClosed(const winrt::IInspectable& sender, const winrt::IInspectable& args)
		//{
		//	m_isFlyoutOpen = false;
		//	SharedHelpers::RaiseAutomationPropertyChangedEvent(*this, winrt::ExpandCollapseState::Expanded, winrt::ExpandCollapseState::Collapsed);
		//}
	}
}
