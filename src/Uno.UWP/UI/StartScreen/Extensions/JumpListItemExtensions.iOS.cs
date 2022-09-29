#nullable disable

#if __IOS__
using System.Linq;
using UIKit;
using System.Collections.Generic;
using Foundation;
using Windows.UI.StartScreen;

namespace Uno.UI.StartScreen.Extensions
{
	internal static class JumpListItemExtensions
	{
		internal static UIApplicationShortcutItem ToShortcutItem(this JumpListItem jumpListItem)
		{
			var dictionary = new Dictionary<string, string>() {
				{ JumpListItem.UnoShortcutKey, "true" }
			};
			if (jumpListItem.Logo != null)
			{
				dictionary[JumpListItem.ImagePathKey] = jumpListItem.Logo.ToString();
			}

			var displayName = jumpListItem.DisplayName;
			if (string.IsNullOrEmpty(displayName))
			{
				displayName = " "; //use single space to make sure item is displayed
			}

			var shortcut =
				new UIApplicationShortcutItem(
					jumpListItem.Arguments,
					displayName,
					jumpListItem.Description,
					jumpListItem.Logo != null ? UIApplicationShortcutIcon.FromTemplateImageName(jumpListItem.Logo.LocalPath) : null,
					NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
						dictionary.Values.Cast<object>().ToArray(),
						dictionary.Keys.Cast<object>().ToArray()));
			return shortcut;
		}
	}
}
#endif
