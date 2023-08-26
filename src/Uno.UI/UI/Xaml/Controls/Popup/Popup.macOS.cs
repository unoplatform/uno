using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using AppKit;
using System.Linq;
using System.Drawing;
using Windows.UI.Xaml.Input;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private NSView _mainWindowContent;

	public NSView MainWindowContent
	{
		get
		{
			if (_mainWindowContent == null)
			{
				_mainWindowContent = NSApplication.SharedApplication.KeyWindow?.ContentView;
			}

			return _mainWindowContent;
		}
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		RegisterPopupPanel();
	}

	private void RegisterPopupPanel()
	{
		if (PopupPanel == null)
		{
			PopupPanel = new PopupPanel(this);
		}

		if (PopupPanel.Superview == null)
		{
			MainWindowContent?.AddSubview(PopupPanel);
		}
	}

	partial void OnUnloadedPartial()
	{
		PopupPanel?.RemoveFromSuperview();
	}
}
