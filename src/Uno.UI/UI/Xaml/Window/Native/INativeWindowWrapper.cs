#nullable enable

using System;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper
{
	bool Visible { get; }

	event EventHandler<Size>? SizeChanged;

	event EventHandler<CoreWindowActivationState>? ActivationChanged;

	event EventHandler<bool>? VisibilityChanged;

	event EventHandler? Closed;

	void Activate();

	void Show();
}
