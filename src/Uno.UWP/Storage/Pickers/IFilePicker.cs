using System.Collections.Generic;
namespace Windows.Storage.Pickers;

internal interface IFilePicker
{
	string CommitButtonTextInternal { get; }
	IList<string> FileTypeFilterInternal { get; }
	PickerLocationId SuggestedStartLocationInternal { get; }
}
