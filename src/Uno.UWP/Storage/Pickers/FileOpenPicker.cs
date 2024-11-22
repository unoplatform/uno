#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a UI element that lets the user choose and open files.
	/// </summary>
	public partial class FileOpenPicker : IFilePicker
	{
		private string _settingsIdentifier = string.Empty;
		private string _commitButtonText = string.Empty;

		/// <summary>
		/// Gets the collection of file types that the folder picker displays.
		/// </summary>
		public IList<string> FileTypeFilter { get; } = new FileExtensionVector();

		/// <summary>
		/// Gets or sets the view mode that the folder picker uses to display items.
		/// </summary>
		public PickerViewMode ViewMode { get; set; } = PickerViewMode.List;

		/// <summary>
		/// Gets or sets the location that the file save picker suggests to the user as the location to save a file.
		/// </summary>
		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.DocumentsLibrary;

		/// <summary>
		/// Gets or sets the settings identifier associated with the current FileSavePicker instance.
		/// </summary>
		public string SettingsIdentifier
		{
			get => _settingsIdentifier;
			set => _settingsIdentifier = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets or sets the label text of the commit button in the file picker UI.
		/// </summary>
		public string CommitButtonText
		{
			get => _commitButtonText;
			set => _commitButtonText = value ?? throw new ArgumentNullException(nameof(value));
		}

		string IFilePicker.CommitButtonTextInternal => CommitButtonText;
		PickerLocationId IFilePicker.SuggestedStartLocationInternal => PickerLocationId.DocumentsLibrary;
		IList<string> IFilePicker.FileTypeFilterInternal => new FileExtensionVector();

#if __SKIA__ || __WASM__ || __IOS__ || __ANDROID__ || __MACOS__
		public FileOpenPicker()
		{
			InitializePlatform();
		}

		partial void InitializePlatform();

		/// <summary>
		/// Shows the file picker so that the user can pick one file.
		/// </summary>
		/// <param name="pickerOperationId">This argument is ignored and has no effect.</param>
		/// <returns>When the call to this method completes successfully, it returns a <see cref="StorageFile"/>
		/// object that represents the file that the user picked.</returns>
		public IAsyncOperation<StorageFile?> PickSingleFileAsync(string pickerOperationId) => PickSingleFileAsync();

		/// <summary>
		/// Shows the file picker so that the user can pick one file.
		/// </summary>
		/// <returns>When the call to this method completes successfully, it returns a <see cref="StorageFile"/>
		/// object that represents the file that the user picked.</returns>
		public IAsyncOperation<StorageFile?> PickSingleFileAsync()
		{
			ValidateConfiguration();

			return AsyncOperation.FromTask(cancellationToken => PickSingleFileTaskAsync(cancellationToken));
		}

		/// <summary>
		/// Shows the file picker so that the user can pick multiple files.
		/// </summary>
		/// <returns>When the call to this method completes successfully, it returns a <see cref="FilePickerSelectedFilesArray"/> object that contains
		/// all the files that were picked by the user. Picked files in this array are represented by <see cref="StorageFile"/> objects.</returns>
		public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync()
		{
			ValidateConfiguration();

			return AsyncOperation.FromTask(cancellationToken => PickMultipleFilesTaskAsync(cancellationToken));
		}

		private void ValidateConfiguration()
		{
			if (FileTypeFilter.Count == 0)
			{
				throw new InvalidOperationException("You must provide at least a general file type filter ('*')");
			}
		}
#endif
	}
}
