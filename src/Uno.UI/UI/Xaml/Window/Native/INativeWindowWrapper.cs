#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper
{
	bool Visible { get; }

	event SizeChangedEventHandler? SizeChanged;

	void Activate();
}
