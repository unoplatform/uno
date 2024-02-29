using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using UIKit;
using Uno.UI.StartScreen.Extensions;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private void Init() => LoadItems();

		public static bool IsSupported() => UIDevice.CurrentDevice.CheckSystemVersion(9, 0);

		private void LoadItems()
		{
			var shortcuts = UIApplication.SharedApplication.ShortcutItems!
				.Where(s => s.IsUnoShortcut())
				.ToArray();
			Items.Clear();
			foreach (var shortcut in shortcuts)
			{
				var jumpListItem = shortcut.ToJumpListItem();
				Items.Add(jumpListItem);
			}
		}

		private IAsyncAction InternalSaveAsync()
		{
			var nonUnoShortcuts = UIApplication.SharedApplication.ShortcutItems!.Where(s => !s.IsUnoShortcut()).ToArray();
			var convertedItems = Items
				.Select(item => item.ToShortcutItem())
				.ToArray();
			UIApplication.SharedApplication.ShortcutItems = nonUnoShortcuts.Union(convertedItems).ToArray();
			return Task.CompletedTask.AsAsyncAction();
		}
	}
}
