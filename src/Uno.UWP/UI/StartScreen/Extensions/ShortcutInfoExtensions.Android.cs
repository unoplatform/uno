#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content.PM;

namespace Windows.UI.StartScreen.Extensions
{
	internal static class ShortcutInfoExtensions
	{
		internal static bool IsUnoShortcut(this ShortcutInfo shortcut)
		{
			if (shortcut == null)
			{
				throw new ArgumentNullException(nameof(shortcut));
			}
			return shortcut.Extras?.ContainsKey(JumpListItem.UnoShortcutKey) == true;
		}

		internal static JumpListItem ToJumpListItem(this ShortcutInfo shortcut)
		{
			if (!shortcut.IsUnoShortcut())
			{
				throw new ArgumentException(
					"Only Uno shortcuts can be converted to JumpListItem",
					nameof(shortcut));
			}

			var jumpListItem = JumpListItem.CreateWithArguments(shortcut.Id, shortcut.ShortLabel);
			jumpListItem.Description = shortcut.LongLabel;
			if (shortcut.Extras.ContainsKey(JumpListItem.ImagePathKey))
			{
				var imagePath = shortcut.Extras.GetString(JumpListItem.ImagePathKey);
				jumpListItem.Logo = new Uri(imagePath);
			}
			return jumpListItem;
		}
	}
}
#endif
