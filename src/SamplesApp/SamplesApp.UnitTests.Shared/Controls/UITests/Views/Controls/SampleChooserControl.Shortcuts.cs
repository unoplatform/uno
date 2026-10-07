#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Controls;

partial class SampleChooserControl
{
	internal async Task FocusSearchAsync()
	{
		if (!SplitView.IsPaneOpen)
		{
			SplitView.IsPaneOpen = true;
			await Task.Yield();
		}

		SearchBox.Focus(FocusState.Keyboard);
	}

	internal void ShowSampleInfo() => InfoButton.Flyout?.ShowAt(InfoButton);
}
