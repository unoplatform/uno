
using Android.Graphics;
using Uno.Extensions;
using Uno.UI;
using Uno.Helpers;
#if __ANDROID__
using Android.Graphics.Drawables;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Uno;

namespace Windows.UI.StartScreen.Extensions
{
	internal static class JumpListItemExtensions
	{
		internal static async Task<ShortcutInfo> ToShortcutInfoAsync(this JumpListItem jumpListItem)
		{
			var builder = new ShortcutInfo.Builder(Application.Context, jumpListItem.Arguments);
			var pm = Application.Context.PackageManager;
			var intent = pm.GetLaunchIntentForPackage(Application.Context.PackageName);
			intent.PutExtra(JumpListItem.ArgumentsExtraKey, jumpListItem.Arguments);
			builder.SetIntent(intent);

			builder.SetShortLabel(jumpListItem.DisplayName);
			builder.SetLongLabel(jumpListItem.Description);

			var persistableBundle = new PersistableBundle();
			persistableBundle.PutString(JumpListItem.UnoShortcutKey, "true");

			if (jumpListItem.Logo != null)
			{
				persistableBundle.PutString(JumpListItem.ImagePathKey, jumpListItem.Logo.ToString());
				var imageResourceId = DrawableHelper.FindResourceId(jumpListItem.Logo.LocalPath);
				if (imageResourceId != null)
				{
					var bitmap = await BitmapFactory.DecodeResourceAsync(
						ContextHelper.Current.Resources,
						imageResourceId.Value,
						new BitmapFactory.Options());
					builder.SetIcon(Icon.CreateWithBitmap(bitmap));
				}
			}

			builder.SetExtras(persistableBundle);

			return builder.Build();
		}
	}
}
#endif
