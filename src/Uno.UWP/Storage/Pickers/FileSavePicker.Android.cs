#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Java.Nio.FileNio.Attributes;
using Uno.Helpers.Activities;
using Uno.UI;
using static Android.Graphics.BitmapFactory;

namespace Windows.Storage.Pickers;

public partial class FileSavePicker
{
	private const string XmlCorrectMimeToActionCreateDocument = "application/xhtml+xml";
	private const string XmlIncorrectMimeToActionCreateDocument = "application/xml";
	private Action<Intent> _intentAction;

	private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
	{
		if (!(ContextHelper.Current is Activity appActivity))
		{
			throw new InvalidOperationException("Application activity is not yet set, API called too early.");
		}

		var action = Intent.ActionCreateDocument;

		var intent = new Intent(action);
		intent.AddCategory(Intent.CategoryOpenable);

		var mimeTypes = GetMimeTypes();
		this.AdjustInvalidMimeTypes(mimeTypes);

		if (mimeTypes.Length == 0)
		{
			intent.SetType("*/*");
		}
		else
		{
			intent.SetType(mimeTypes[0]);
		}

		if (mimeTypes.Length > 1)
		{
			// Add list of accepted mime types as options.
			intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);
		}

		if (!string.IsNullOrEmpty(SuggestedFileName))
		{
			var defaultFileName = SuggestedFileName;
			if (FileTypeChoices.Count > 0 &&
				(Path.GetExtension(SuggestedFileName) is not { } extension ||
				!FileTypeChoices.Values.Any(extensions => extensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))))
			{
				// Add the default extension if not already present.
				defaultFileName += FileTypeChoices.Values.First().First();
			}
			intent.PutExtra(Intent.ExtraTitle, defaultFileName);
		}

		_intentAction?.Invoke(intent);

		var activity = await AwaitableResultActivity.StartAsync();
		var result = await activity.StartActivityForResultAsync(intent, cacellationToken: token);
		var resultIntent = result?.Intent;

		if (resultIntent?.Data != null)
		{
			ContextHelper.Current.GrantUriPermission(ContextHelper.Current.PackageName, resultIntent.Data, ActivityFlags.GrantWriteUriPermission);
			return StorageFile.GetFromSafUri(resultIntent.Data);
		}
		else if (resultIntent?.ClipData != null && resultIntent.ClipData.ItemCount > 0)
		{
			for (int itemIndex = 0; itemIndex < resultIntent.ClipData.ItemCount; itemIndex++)
			{
				var uri = resultIntent.ClipData.GetItemAt(itemIndex)?.Uri;
				if (uri != null)
				{
					return StorageFile.GetFromSafUri(uri);
				}
			}
		}

		return null;
	}

	private void AdjustInvalidMimeTypes(string[]? mimes)
	{
		if (mimes is not null)
		{
			for (int i = 0; i < mimes.Length; i++)
			{
				if (mimes[i] == XmlIncorrectMimeToActionCreateDocument)
				{
					mimes[i] = XmlCorrectMimeToActionCreateDocument;
				}
			}
		}
	}

	private string[] GetMimeTypes()
	{
		var mimes = FileTypeChoices
			.SelectMany(choice => choice.Value)
			.Select(extension => MimeTypeService.GetFromExtension(extension))
			.Distinct()
			.ToArray();
		AdjustInvalidMimeTypes(mimes);
		return mimes ?? Array.Empty<string>();
	}

	internal void RegisterOnBeforeStartActivity(Action<Intent> intentAction)
		=> _intentAction = intentAction;
}
