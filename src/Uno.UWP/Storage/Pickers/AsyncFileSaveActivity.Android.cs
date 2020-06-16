using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Uno;
using Uno.UI;

namespace Windows.Storage.Pickers
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	class AsyncFileSaveActivity : AsyncActivity
	{
		internal async Task<Uri> GetFileUri()
		{
			var activityResult = await Start();
			var fileUri = activityResult.Intent.Data;
			return new Uri(fileUri.ToString());
		}

		public static Intent CreateIntent(IDictionary<string, IList<string>> fileTypeChoices, string suggestedStartLocation, string suggestedFileName)
		{
			{
				var intent = new Intent(Intent.ActionCreateDocument);
				var fileTypes = GetMimeTypes(fileTypeChoices);
				intent.SetType(fileTypes.Count switch
				{
					1 => fileTypes.FirstOrDefault(),
					_ => "*/*"
				});

				if (fileTypes.Count > 1)
				{
					intent.PutStringArrayListExtra(Intent.ExtraMimeTypes, fileTypes);
				}

				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && !string.IsNullOrEmpty(suggestedStartLocation))
				{
					intent.PutExtra(DocumentsContract.ExtraInitialUri, suggestedStartLocation);
				}

				intent.AddCategory(Intent.CategoryOpenable);
				intent.PutExtra(Intent.ExtraTitle, suggestedFileName);
				return intent;
			}
		}

			private static List<string> GetMimeTypes(IDictionary<string, IList<string>> fileTypeChoices)
		{
			var result = new HashSet<string>();

			foreach (var filetype in fileTypeChoices)
			{
				foreach (var extension in filetype.Value)
				{
					var androidExtension = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension.Substring(1));
					result.Add(androidExtension);
				}
			}
			return result.ToList();
		}
	}
}
