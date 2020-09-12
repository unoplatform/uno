using System.Collections.Generic;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		public string SuggestedFileName { get; set; }

		public string SettingsIdentifier { get; set; }

		public StorageFile SuggestedSaveFile { get; set; }

		public string CommitButtonText { get; set; }

		public IDictionary<string, IList<string>> FileTypeChoices { get; set; }
	}
}
