#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal interface IChangingFocusEventArgs
	{
		DependencyObject? NewFocusedElement { get; set; }

		bool Cancel { get; }
	}
}
