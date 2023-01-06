#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal interface IChangingFocusEventArgs
	{
		DependencyObject? NewFocusedElement { get; set; }

		bool Cancel { get; }
	}
}
