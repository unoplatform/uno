#if __IOS__
using System.Linq;
using UIKit;
using System.Collections.Generic;
using Foundation;

namespace Windows.UI.StartScreen.Extensions
{
	internal static class JumpListItemExtensions
	{
		internal static UIApplicationShortcutItem ToShortcutItem(this JumpListItem jumpListItem)
		{
			var dictionary = new Dictionary<string, string>() { { JumpListItem.UnoShortcutKey, "true" } };
			if (jumpListItem.Logo != null)
			{
				dictionary[JumpListItem.ImagePathKey] = jumpListItem.Logo.ToString();
			}

			var shortcut =
				new UIApplicationShortcutItem(
					jumpListItem.Arguments,
					jumpListItem.DisplayName,
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
