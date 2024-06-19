using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

internal class MenuFlyoutItemBaseCollection : DependencyObjectCollection<MenuFlyoutItemBase>
{
#if HAS_UNO // Our API is a bit different from WinUI
	private protected override void OnCollectionChanged() => NotifyMenuFlyoutOfCollectionChange();
#endif

	private void NotifyMenuFlyoutOfCollectionChange()
	{
		ctl::ComPtr<DependencyObject> ownerAsDO;
		IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(static_cast<CCollection*>(GetHandle())->GetOwner(), &ownerAsDO));

		auto ownerAsMenuFlyout = ownerAsDO.AsOrNull<MenuFlyout>();

		if (ownerAsMenuFlyout)
		{
			IFC_RETURN(ownerAsMenuFlyout.Cast<MenuFlyout>()->QueueRefreshItemsSource());
		}
		else
		{
			auto ownerAsMenuFlyoutSubItem = ownerAsDO.AsOrNull<MenuFlyoutSubItem>();

			// MenuFlyoutItemBaseCollection is only used by MenuFlyout and MenuFlyoutSubItem.
			// If another type is added, this will need to change.
			IFCEXPECT_RETURN(ownerAsMenuFlyoutSubItem != nullptr);

			IFC_RETURN(ownerAsMenuFlyoutSubItem.Cast<MenuFlyoutSubItem>()->QueueRefreshItemsSource());
		}
	}
}
