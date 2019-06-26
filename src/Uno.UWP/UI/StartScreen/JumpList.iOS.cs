#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		public static bool IsSupported() => UIDevice.CurrentDevice.CheckSystemVersion(9, 0);

		private async Task LoadShortcutsAsync()
		{
			var shortcuts = UIApplication.SharedApplication.ShortcutItems.ToArray();
			Items.Clear();
			foreach (var shortcut in shortcuts)
			{
				var jumplistItem = ShortcutToJumpListItem(shortcut);
				Items.Add(jumplistItem);				
			}
		}

		private JumpListItem ShortcutToJumpListItem(UIApplicationShortcutItem shortcut)
		{
			var item = JumpListItem.CreateWithArguments(shortcut.UserInfo[]?.ToString() ?? string.Empty, shortcut.LocalizedTitle);
			item.Description = shortcut.LocalizedSubtitle;
			return item;			
		}

		private UIApplicationShortcutItem JumpListItemToShortcut(JumpListItem item)
		{
			var shortcut = new UIApplicationShortcutItem(GenerateId(), item.DisplayName, item.Description, UIApplicationShortcutIcon.)
		}

		private string GenerateId(JumpListItem item)
		{
			throw new NotImplementedException();
		}
	}
}
#endif
