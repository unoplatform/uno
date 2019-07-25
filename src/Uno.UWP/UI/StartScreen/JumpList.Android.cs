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

		private void Init()
		{
			_manager = (ShortcutManager)Application.Context.GetSystemService(Context.ShortcutService);
		}

		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;

		private async Task LoadShortcuts()
		{
			throw new NotImplementedException();
		}

		private JumpListItem ShortcutToJumpListItem(ShortcutInfo shortcut)
		{
			throw new NotImplementedException();
			//var item = JumpListItem.CreateWithArguments(shortcut.Intent.ToUri(IntentUriType.AllowUnsafe), shortcut.ShortLabel);
			//item.Description = shortcut.LongLabel;
			//item. = shortcut.
			//shortcut.LongLabel
		}

		private IAsyncAction InternalSaveAsync()
		{
			throw new NotImplementedException();
		}
	}
}
#endif
