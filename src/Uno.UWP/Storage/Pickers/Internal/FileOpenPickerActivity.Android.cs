#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Content;
using AndroidUri = Android.Net.Uri;

namespace Uno.Storage.Pickers.Internal
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	internal class FileOpenPickerActivity : Activity
	{
		internal static event EventHandler<List<AndroidUri>> FilePicked;

		private bool _multiple;
		
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var caller = base.Intent.Extras;
			_multiple = caller.GetBoolean(Intent.ExtraAllowMultiple, false);

			var intent = CreateIntent(_multiple,
				caller.GetStringArray(Intent.ExtraMimeTypes).ToList(),
				caller.GetString(DocumentsContract.ExtraInitialUri, "")
				);

			intent.SetType("*/*");
			intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

			// StartActivityForResult(Android.Content.Intent.CreateChooser(intent, "Select file"),10);
			StartActivityForResult(intent, 10);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			var pickedFiles = new List<AndroidUri>();

			if (resultCode != Result.Canceled)
			{
				if (_multiple)
				{
					// multiple files - ClipData
					if (data?.ClipData != null)
					{
						for (int i = 0; i < data.ClipData.ItemCount; i++)
						{
							var item = data.ClipData.GetItemAt(i);
							pickedFiles.Add(item.Uri);
						}
					}
				}
				else
				{
					// single file - Data
					if (data?.Data != null)
					{
						pickedFiles.Add(data.Data);
					}

				}
			}

			FilePicked?.Invoke(null, pickedFiles);
			Finish();
		}

		private Intent CreateIntent(bool multiple, List<string> mimeTypes, string initialDir)
		{
			var intent = new Intent(Intent.ActionOpenDocument);

			// file ext / mimetypes
			if (mimeTypes.Count < 1)
			{  // if none is given, then use all types
				intent.SetType("*/*");  
			}
			else
			{
				if (mimeTypes.Count < 2)
				{
					intent.SetType(mimeTypes.ElementAt(0));
				}
				else
				{
					intent.SetType("*/*");
					intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes.ToArray());
				}
			}

			// multiple selection
			if (multiple)
			{
				intent.PutExtra(Intent.ExtraAllowMultiple, true);
			}

			// initial dir
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O && !string.IsNullOrEmpty(initialDir))
			{
				intent.PutExtra(DocumentsContract.ExtraInitialUri, initialDir);
			}

			// maybe also
			// intent.AddCategory(Android.Content.Intent.CategoryOpenable);
			// "openable" means: ContentResolver#openFileDescriptor(Uri, String) // string: rwt (t:can truncate)

			return intent;
		}
	}
}

#endif
