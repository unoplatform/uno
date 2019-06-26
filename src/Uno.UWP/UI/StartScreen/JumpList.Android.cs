#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Windows.Foundation;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private ShortcutManager _manager;

		private JumpList()
		{
			_manager = (ShortcutManager)Application.Context.GetSystemService(Context.ShortcutService);
		}

		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;

		public static async IAsyncOperation<JumpList> LoadCurrentAsync()
		{
			
		}

		private async Task LoadShortcuts()
		{
			
		}

		private JumpListItem ShortcutToJumpListItem(ShortcutInfo shortcut)
		{
			var item = JumpListItem.CreateWithArguments(shortcut.Intent.ToUri(IntentUriType.AllowUnsafe), shortcut.ShortLabel);
			item.Description = shortcut.LongLabel;
			item. = shortcut.
			shortcut.LongLabel
		}
	}
}
#endif
