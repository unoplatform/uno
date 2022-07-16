#nullable enable

using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.UI.Input;
using MouseDevice = System.Windows.Input.MouseDevice;
using System.Reflection;
using Windows.System;
using Uno.UI.Skia.Platform.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Constants;
using Uno.UI.Runtime.Skia.Wpf.Input;

namespace Uno.UI.Skia.Platform
{
	internal partial class WpfCoreWindowExtension : ICoreWindowExtension
	{
		private readonly ICoreWindowEvents _ownerEvents;
		private readonly WpfHost? _host;

		public CoreCursor PointerCursor
		{
			get => Mouse.OverrideCursor.ToCoreCursor();
			set => Mouse.OverrideCursor = value.ToCursor();
		}

		public WpfCoreWindowExtension(object owner)
		{
			_ownerEvents = (ICoreWindowEvents)owner;

			_host = WpfHost.Current;
			if (_host is null)
			{
				return;
			}			

			// Hook for native events
			_host.Loaded += HookNative;

			void HookNative(object sender, RoutedEventArgs e)
			{
				_host.Loaded -= HookNative;

				var win = Window.GetWindow(_host);

				win.AddHandler(UIElement.KeyUpEvent, (KeyEventHandler)HostOnKeyUp, true);
				win.AddHandler(UIElement.KeyDownEvent, (KeyEventHandler)HostOnKeyDown, true);
			}
		}

		public void SetPointerCapture(PointerIdentifier pointer)
			=> WpfHost.Current?.CaptureMouse();

		public void ReleasePointerCapture(PointerIdentifier pointer)
			=> WpfHost.Current?.ReleaseMouseCapture();
	}
}
