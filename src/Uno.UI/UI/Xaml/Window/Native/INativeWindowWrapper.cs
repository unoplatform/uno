#nullable enable

using System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper
{
	bool Visible { get; }

	event SizeChangedEventHandler? SizeChanged;

	event EventHandler<CoreWindowActivationState>? ActivationChanged;

	event EventHandler<bool>? VisibilityChanged;

	event EventHandler? Closed;

	void Activate();
}
