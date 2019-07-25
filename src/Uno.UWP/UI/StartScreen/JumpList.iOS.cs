#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using UIKit;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private void Init() => LoadItems();

		public static bool IsSupported() => UIDevice.CurrentDevice.CheckSystemVersion(9, 0);

		private void LoadItems()
		{
			var shortcuts = UIApplication.SharedApplication.ShortcutItems.ToArray();
			Items.Clear();
			foreach (var shortcut in shortcuts)
			{
				var jumplistItem = ShortcutToJumpListItem(shortcut);
				Items.Add(jumplistItem);
			}
		}

		private IAsyncAction InternalSaveAsync()
		{
			var convertedItems = new List<UIApplicationShortcutItem>();
			foreach (var item in Items)
			{
				convertedItems.Add(JumpListItemToShortcut(item));
			}
			UIApplication.SharedApplication.ShortcutItems = convertedItems.ToArray();
			return Task.CompletedTask.AsAsyncAction();
		}

		private JumpListItem ShortcutToJumpListItem(UIApplicationShortcutItem shortcut)
		{
			var item = JumpListItem.CreateWithArguments(shortcut.Type, shortcut.LocalizedTitle);
			item.Description = shortcut.LocalizedSubtitle;
			return item;
		}

		private UIApplicationShortcutItem JumpListItemToShortcut(JumpListItem item)
		{
			var shortcut =
				new UIApplicationShortcutItem(
					item.Arguments,
					item.DisplayName,
					item.Description,
					UIApplicationShortcutIcon.FromType(UIApplicationShortcutIconType.Add),
					null);
			return shortcut;
		}
	}
}
#endif
