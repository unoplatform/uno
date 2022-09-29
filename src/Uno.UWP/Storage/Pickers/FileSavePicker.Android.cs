using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.UI;
using Uno.Helpers.Activities;



namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			if (!(ContextHelper.Current is Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}

			var action = Intent.ActionCreateDocument;

			var intent = new Intent(action);
			intent.SetType("*/*");
			intent.AddCategory(Intent.CategoryOpenable);
			var mimeTypes = GetMimeTypes();

			this.ParseMimeTypeForXml(mimeTypes);

            foreach (var mime in mimeTypes)
            {
				intent.PutExtra(Intent.ExtraMimeTypes, mime);
			}

			if (!string.IsNullOrEmpty(SuggestedFileName))
			{
				intent.PutExtra(Intent.ExtraTitle, SuggestedFileName);
			}

			var activity = await AwaitableResultActivity.StartAsync();
			var result = await activity.StartActivityForResultAsync(intent, cacellationToken: token);
			var resultIntent = result?.Intent;			

			if (resultIntent?.Data != null)
			{
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

		private void ParseMimeTypeForXml(string[]? mimes)
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
			return FileTypeChoices
				.SelectMany(choice => choice.Value)
				.Select(extension => MimeTypeService.GetFromExtension(extension))
				.Distinct()
				.ToArray();
		}

		private const string XmlCorrectMimeToActionCreateDocument = "application/xhtml+xml";
		private const string XmlIncorrectMimeToActionCreateDocument = "application/xml";

	}
}
