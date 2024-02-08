using System.Collections.Generic;
namespace Windows.Storage.Pickers;

internal interface IFilePicker
{
	public string CommitButtonText { get; }
	IList<string> FileTypeFilter { get; }
	public PickerLocationId SuggestedStartLocation { get; }
}
