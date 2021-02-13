#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Represents a UI element that lets the user choose and open files.
	/// </summary>
	public partial class FileOpenPicker
    {
		private string _suggestedFileName = string.Empty;
		private string _settingsIdentifier = string.Empty;
		private string _commitButtonText = string.Empty;

		/// <summary>
		/// Gets the collection of file types that the folder picker displays.
		/// </summary>
		public IList<string> FileTypeFilter => new FileExtensionVector();

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
	}
}
