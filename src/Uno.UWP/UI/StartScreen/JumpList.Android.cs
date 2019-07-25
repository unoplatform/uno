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
using Java.Lang;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		public const string JumpListItemExtra = "JumpListItem.Arguments";

		private ShortcutManager _manager;

		private void Init()
		{
			_manager = (ShortcutManager)Application.Context.GetSystemService(Context.ShortcutService);
			LoadItems();
		}

		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;

		private void LoadItems()
		{
			var shortcuts = _manager.DynamicShortcuts.ToArray();
			Items.Clear();
			foreach (var shortcut in shortcuts)
			{
				var jumplistItem = ShortcutToJumpListItem(shortcut);
				Items.Add(jumplistItem);
			}
		}

		private JumpListItem ShortcutToJumpListItem(ShortcutInfo shortcut)
		{
			var item = JumpListItem.CreateWithArguments(shortcut.Id, shortcut.ShortLabel);
			item.Description = shortcut.LongLabel;
			return item;
		}

		private ShortcutInfo JumpListItemToShortcut(JumpListItem item)
		{
			var builder = new ShortcutInfo.Builder(Application.Context, item.Arguments);
			var pm = Application.Context.PackageManager;
			var intent = pm.GetLaunchIntentForPackage(Application.Context.PackageName);
			//Intent startIntent = new Intent(Intent.ActionView);
			//startIntent.AddFlags(ActivityFlags.NewTask);
			//startIntent.SetPackage(Application.Context.PackageName);
			intent.PutExtra(JumpListItemExtra, item.Arguments);
			builder.SetIntent(intent);
			builder.SetShortLabel(item.DisplayName);
			builder.SetLongLabel(item.Description);
			return builder.Build();
		}

		private IAsyncAction InternalSaveAsync()
		{
			var convertedItems = new List<ShortcutInfo>();
			foreach (var item in Items)
			{
				convertedItems.Add(JumpListItemToShortcut(item));
			}
			_manager.SetDynamicShortcuts(convertedItems);
			return Task.CompletedTask.AsAsyncAction();
		}
	}
}
#endif
