#if __ANDROID__ || __IOS__
using System;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsView : Control
{
	public ItemsView()
	{
#if DEBUG
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("ItemsView is not supported on this platform (iOS, Android).");
		}
#endif
		throw new NotSupportedException("ItemsView is not yet supported on this platform. For more information, visit https://aka.platform.uno/notimplemented#m=ItemsView");
	}
}
#endif
