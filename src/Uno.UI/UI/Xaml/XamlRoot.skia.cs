#nullable enable

using System;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Microsoft.UI.Xaml;

public partial class XamlRoot
{
	internal IDisposable OpenPopup(Controls.Primitives.Popup popup)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Creating popup");
		}

		if (VisualTree.PopupRoot == null)
		{
			throw new InvalidOperationException("PopupRoot is not initialized yet.");
		}

		var popupPanel = popup.PopupPanel;
		VisualTree.PopupRoot.Children.Add(popupPanel);

		return Disposable.Create(() =>
		{

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Closing popup");
			}

			VisualTree.PopupRoot.Children.Remove(popupPanel);
		});
	}
}
