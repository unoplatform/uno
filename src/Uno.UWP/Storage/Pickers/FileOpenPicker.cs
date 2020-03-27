#if __ANDROID__
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a UI element that lets the user choose and open files.
	/// </summary>
	public partial class FileOpenPicker
    {
		/// <summary>
		/// Creates a new instance of a FileOpenPicker.
		/// </summary>
		public FileOpenPicker() => Init();

		/// <summary>
		/// Gets or sets the initial location where the file open picker looks for files to present to the user.
		/// </summary>
		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.DocumentsLibrary;

		/// <summary>
		/// Gets or sets the settings identifier associated with the state of the file open picker.
		/// </summary>
		public string SettingsIdentifier { get; set; } = "";

		/// <summary>
		/// Gets the collection of file types that the file open picker displays.
		/// Examples - ".png", ".jpg". Use "*" for any.
		/// </summary>
		public IList<string> FileTypeFilter { get; } = new NonNullList<string>();

		partial void Init();

		/// <summary>
		/// Shows the file picker so that the user can pick one file.
		/// </summary>
		/// <returns>When the call to this method completes successfully,
		/// it returns a StorageFile object that represents the file that the user picked.</returns>
		public IAsyncOperation<StorageFile> PickSingleFileAsync()
		{
			ValidatePicker();
			return PickSingleFileAsyncTask().AsAsyncOperation();
		}

		/// <summary>
		/// Shows the file picker so that the user can pick one file.
		/// </summary>
		/// <param name="pickerOperationId">This argument is ignored and has no effect.</param>
		/// <returns>When the call to this method completes successfully,
		/// it returns a StorageFile object that represents the file that the user picked.</returns>
		public IAsyncOperation<StorageFile> PickSingleFileAsync(string pickerOperationId) => PickSingleFileAsync();

		/// <summary>
		/// Shows the file picker so that the user can pick multiple files. (UWP app).
		/// </summary>
		/// <returns>When the call to this method completes successfully, it returns
		/// a FilePickerSelectedFilesArray object that contains all the files that were
		/// picked by the user. Picked files in this array are represented by StorageFile objects.</returns>
		public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync()
		{
			ValidatePicker();
			return PickMultipleFilesAsyncTask().AsAsyncOperation();
		}

		private void ValidatePicker()
		{
			if (FileTypeFilter.Count == 0)
			{
				throw new InvalidOperationException(
					$"At least one file type or wildcard (*) must be specified in {nameof(FileTypeFilter)}.");
			}
		}
	}
}
#endif
