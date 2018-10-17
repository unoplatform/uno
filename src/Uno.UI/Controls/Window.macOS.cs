using System;
using System.Drawing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Extensions;
using System.Linq;
using Windows.UI.Core;
using Uno.Diagnostics.Eventing;
using Foundation;
using AppKit;
using CoreGraphics;
using Uno.Collections;
using Windows.UI.Xaml.Input;
using WebKit;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Uno.UI.Controls;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A UIWindow which handle the focus item.
	/// </summary>
	public partial class Window : NSWindow
	{
		/// <summary>
		/// ctor.
		/// </summary>
		public Window()
			: base()
		{
		}
	}
}
