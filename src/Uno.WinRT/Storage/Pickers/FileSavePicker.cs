#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a file picker that lets the user choose the file name, extension, and storage location for a file.
	/// </summary>
	public partial class FileSavePicker
	{
		private string _suggestedFileName = string.Empty;
		private string _settingsIdentifier = string.Empty;
		private string _commitButtonText = string.Empty;
		private string _defaultFileExtension = string.Empty;

		/// <summary>
		/// Gets the collection of valid file types that the user can choose to assign to a file.
		/// </summary>
		public IDictionary<string, IList<string>> FileTypeChoices { get; } = new FilePickerFileTypesOrderedMap();

		/// <summary>
		/// Gets or sets the location that the file save picker suggests to the user as the location to save a file.
		/// </summary>
		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.DocumentsLibrary;

		/// <summary>
		/// Gets or sets the file name that the file save picker suggests to the user.
		/// </summary>
		public string SuggestedFileName
		{
			get => _suggestedFileName;
			set => _suggestedFileName = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets or sets the settings identifier associated with the current FileSavePicker instance.
		/// </summary>
		public string SettingsIdentifier
		{
			get => _settingsIdentifier;
			set => _settingsIdentifier = value ?? throw new ArgumentNullException(nameof(value));
		}

#if __SKIA__ || __NETSTD_REFERENCE__
		/// <summary>
		/// Gets or sets the default file name extension that the fileSavePicker gives to files to be saved.
		/// </summary>
		/// <remarks>Use format starting with a dot, e.g. ".xyz"</remarks>
		public string DefaultFileExtension
		{
			get => _defaultFileExtension;
			set => _defaultFileExtension = value ?? throw new ArgumentNullException(nameof(value));
		}
#endif

		/// <summary>
		/// Gets or sets the storageFile that the file picker suggests to the user for saving a file.
		/// </summary>
		public StorageFile? SuggestedSaveFile { get; set; }

		/// <summary>
		/// Gets or sets the label text of the commit button in the file picker UI.
		/// </summary>
		public string CommitButtonText
		{
			get => _commitButtonText;
			set => _commitButtonText = value ?? throw new ArgumentNullException(nameof(value));
		}

#if __SKIA__ || __WASM__ || __ANDROID__ || __IOS__
		public FileSavePicker()
		{
			InitializePlatform();
		}

		partial void InitializePlatform();

		/// <summary>
		/// Shows the file picker so that the user can save a file and set the file name, extension, and location of the file to be saved.
		/// </summary>
		/// <returns>
		/// When the call to this method completes successfully, it returns a <see cref="StorageFile"/> object that was created to represent the saved file.
		/// The file name, extension, and location of this <see cref="StorageFile"/> match those specified by the user, but the file has no content.
		/// To save the content of the file, your app must write the content to this <see cref="StorageFile"/>.
		/// </returns>
		public IAsyncOperation<StorageFile?> PickSaveFileAsync() =>
			AsyncOperation.FromTask(cancellationToken => PickSaveFileTaskAsync(cancellationToken));
#endif
	}
}
