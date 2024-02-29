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
using System.Diagnostics.CodeAnalysis;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private ShortcutManager _manager;

		[MemberNotNull(nameof(_manager))]
		private void Init()
		{
			_manager = (ShortcutManager)Application.Context.GetSystemService(Context.ShortcutService)!;
			LoadItems();
		}

		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;

		private void LoadItems()
		{
			var shortcuts = _manager.DynamicShortcuts.ToArray();
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
				var nonUnoShortcuts = _manager.DynamicShortcuts.Where(s => !s.IsUnoShortcut()).ToArray();
				var convertedItems = new List<ShortcutInfo>();
				foreach (var item in Items)
				{
					convertedItems.Add(await item.ToShortcutInfoAsync());
				}

				_manager.SetDynamicShortcuts(nonUnoShortcuts.Union(convertedItems).ToArray());
			});
		}
	}
}
