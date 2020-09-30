#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.UI;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		internal const int RequestCode = 6003;
		private static TaskCompletionSource<Intent?>? _currentFileOpenPickerRequest;

		internal static bool TryHandleIntent(Intent intent, Result resultCode)
		{
			if (_currentFileOpenPickerRequest == null)
			{
				return false;
			}
			if (resultCode == Result.Canceled)
			{
				_currentFileOpenPickerRequest.SetResult(null);
			}
			else
			{
				_currentFileOpenPickerRequest.SetResult(intent);
			}
			return true;
		}

		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			if (!(ContextHelper.Current is Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}

			var action = Intent.ActionCreateDocument;

			var intent = new Intent(action);
			intent.SetType("*/*");

			var mimeTypes = GetMimeTypes();
			intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);

			if (!string.IsNullOrEmpty(SuggestedFileName))
			{
				intent.PutExtra(Intent.ExtraTitle, SuggestedFileName);
			}

			_currentFileOpenPickerRequest = new TaskCompletionSource<Intent?>();

			appActivity.StartActivityForResult(intent, RequestCode);

			var resultIntent = await _currentFileOpenPickerRequest.Task;
			_currentFileOpenPickerRequest = null;

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

		private string[] GetMimeTypes()
		{
			return FileTypeChoices
				.SelectMany(choice => choice.Value)
				.Select(extension => MimeTypeService.GetFromExtension(extension))
				.Distinct()
				.ToArray();
		}

	}
}
