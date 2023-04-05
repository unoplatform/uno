#if __ANDROID__ || __IOS__
using System;

namespace Windows.UI.Core;

public interface ICoreWindowEvents
{
	void RaiseKeyUp(KeyEventArgs args);
	void RaiseKeyDown(KeyEventArgs args);
}
#endif
