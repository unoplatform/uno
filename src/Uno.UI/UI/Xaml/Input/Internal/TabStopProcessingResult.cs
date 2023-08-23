#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input;

internal struct TabStopProcessingResult
{
	public TabStopProcessingResult(bool isOverriden, DependencyObject? newTabStop)
	{
		IsOverriden = isOverriden;
		NewTabStop = newTabStop;
	}

	public bool IsOverriden { get; set; }

	public DependencyObject? NewTabStop { get; set; }
}
