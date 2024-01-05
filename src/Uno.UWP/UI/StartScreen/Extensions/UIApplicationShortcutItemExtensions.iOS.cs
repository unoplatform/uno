using Foundation;
using System;
using UIKit;
using Windows.UI.StartScreen;

namespace Uno.UI.StartScreen.Extensions
{
	internal static class UIApplicationShortcutItemExtensions
	{
		internal static bool IsUnoShortcut(this UIApplicationShortcutItem shortcut)
		{
			if (shortcut == null)
			{
				throw new ArgumentNullException(nameof(shortcut));
			}
			return shortcut.UserInfo?[JumpListItem.UnoShortcutKey] != null;
		}

		internal static JumpListItem ToJumpListItem(this UIApplicationShortcutItem shortcut)
		{
			if (!shortcut.IsUnoShortcut())
			{
				throw new ArgumentException(
					"Only Uno shortcut items can be converted to JumpListItem",
					nameof(shortcut));
			}

			var jumpListItem = JumpListItem.CreateWithArguments(shortcut.Type, shortcut.LocalizedTitle);
			jumpListItem.Description = shortcut.LocalizedSubtitle!;
			if (shortcut.UserInfo!.ContainsKey(new NSString(JumpListItem.ImagePathKey)))
			{
				var imagePath = shortcut.UserInfo[JumpListItem.ImagePathKey].ToString();
				jumpListItem.Logo = new Uri(imagePath);
			}
			return jumpListItem;
		}
	}
}
