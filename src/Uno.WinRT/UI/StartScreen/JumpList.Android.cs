using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Windows.Foundation;
using System.Collections.Generic;
using Uno.UI.StartScreen.Extensions;
using Uno.Extensions;
using AndroidX.Core.Content.PM;
using Uno.UI;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private void Init()
		{
			LoadItems();
		}

		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;

		private void LoadItems()
		{
			var shortcuts = ShortcutManagerCompat.GetDynamicShortcuts(ContextHelper.Current).ToArray();
			Items.Clear();
			foreach (var shortcut in shortcuts)
			{
				Items.Add(shortcut.ToJumpListItem());
			}
		}

		private IAsyncAction InternalSaveAsync()
		{
			return AsyncAction.FromTask(async ct =>
			{
				var nonUnoShortcuts = ShortcutManagerCompat.GetDynamicShortcuts(ContextHelper.Current).Where(s => !s.IsUnoShortcut()).ToArray();
				var convertedItems = new List<ShortcutInfoCompat>();
				foreach (var item in Items)
				{
					convertedItems.Add(await item.ToShortcutInfoCompatAsync());
				}

				ShortcutManagerCompat.SetDynamicShortcuts(ContextHelper.Current, nonUnoShortcuts.Union(convertedItems).ToArray());
			});
		}
	}
}
