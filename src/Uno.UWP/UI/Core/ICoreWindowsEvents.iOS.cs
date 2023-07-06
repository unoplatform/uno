using System;

namespace Windows.UI.Core;

internal interface ICoreWindowEvents
{
	void RaiseKeyUp(KeyEventArgs args);
	void RaiseKeyDown(KeyEventArgs args);
}

