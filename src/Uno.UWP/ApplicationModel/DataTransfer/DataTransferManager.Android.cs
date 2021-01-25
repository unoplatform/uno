#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		public static bool IsSupported() => true;

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var context = ContextHelper.Current;
			if (context == null)
			{
				if (_instance.Value.Log().IsEnabled(LogLevel.Error))
				{
					_instance.Value.Log().LogError("The Share API was called too early in the application lifecycle");
				}
				return false;
			}

			var dataPackageView = dataPackage.GetView();
			var items = new List<string>();

			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				items.Add(text);
			}

			var uri = await GetSharedUriAsync(dataPackageView);
			if (uri != null)
			{
				items.Add(uri.OriginalString);
			}

			var intent = new Intent(Intent.ActionSend);
			intent.SetType("text/plain");
			intent.PutExtra(Intent.ExtraText, string.Join(Environment.NewLine, items));

			var title = dataPackage.Properties.Title;
			if (!string.IsNullOrWhiteSpace(title))
			{
				intent.PutExtra(Intent.ExtraSubject, title);
			}

			var chooserIntent = Intent.CreateChooser(intent, title ?? string.Empty);
			var flags = ActivityFlags.ClearTop | ActivityFlags.NewTask;
			chooserIntent?.SetFlags(flags);
			ContextHelper.Current.StartActivity(chooserIntent);

			return true;
		}
	}
}
