using System.Collections.Generic;
namespace Windows.Storage.Pickers;

internal interface IFilePicker
{
	string CommitButtonText { get; }
	IList<string> FileTypeFilter { get; }
	PickerLocationId SuggestedStartLocation { get; }
}
