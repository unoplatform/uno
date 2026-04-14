using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class Flyout
{
	protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
		if (Content is not null)
		{
			Content.TryInvokeKeyboardAccelerator(args);
		}
	}
}
