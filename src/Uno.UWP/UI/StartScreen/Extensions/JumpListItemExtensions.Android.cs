using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using AndroidX.Core.Content.PM;
using AndroidX.Core.Graphics.Drawable;
using Uno.Helpers;
using Windows.UI.StartScreen;

namespace Uno.UI.StartScreen.Extensions
{
	internal static class JumpListItemExtensions
	{
		internal static async Task<ShortcutInfoCompat> ToShortcutInfoCompatAsync(this JumpListItem jumpListItem)
		{
			var builder = new ShortcutInfoCompat.Builder(Application.Context, jumpListItem.Arguments);
			var pm = Application.Context.PackageManager;
			var intent = pm.GetLaunchIntentForPackage(Application.Context.PackageName);
			intent.PutExtra(JumpListItem.ArgumentsExtraKey, jumpListItem.Arguments);
			builder.SetIntent(intent);
			builder.SetShortLabel(
				!string.IsNullOrEmpty(jumpListItem.DisplayName) ?
					jumpListItem.DisplayName : " "); //Android requires non-empty DisplayName
			if (!string.IsNullOrEmpty(jumpListItem.Description))
			{
				builder.SetLongLabel(jumpListItem.Description);
			}

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
					builder.SetIcon(IconCompat.CreateWithBitmap(bitmap));
				}
			}

			builder.SetExtras(persistableBundle);

			return builder.Build();
		}
	}
}
