#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides methods and properties for interacting with the status bar on a window (app view).
	/// </summary>
	public sealed partial class StatusBar
	{
		private static StatusBar _statusBar;

		public event TypedEventHandler<StatusBar, object> Hiding;
		public event TypedEventHandler<StatusBar, object> Showing;

		private StatusBar() { }

		/// <summary>
		/// Gets the status bar for the current window (app view).
		/// </summary>
		/// <returns>The status bar for the current window (app view).</returns>
		public static StatusBar GetForCurrentView()
		{
			CoreDispatcher.CheckThreadAccess();
			if (_statusBar == null)
			{
				_statusBar = new StatusBar();
			}

			return _statusBar;
		}

		/// <summary>
		/// Gets the region of the core window currently occluded by the status bar.
		/// </summary>
		public Rect OccludedRect => GetOccludedRect();

		/// <summary>
		/// Gets or sets the foreground color of the status bar. The alpha channel of the color is not used.
		/// </summary>
		/// <remarks>
		/// <para>iOS and Android (API 23+) only allow their status bar foreground to be set to either Light or Dark. 
		/// The provided color will automatically be converted to the nearest supported color to preserve contrast.</para>
		/// <para>In general, you should set this property to either White or Black to avoid confusion.</para>
		/// <para>This property is only supported on Android starting from Marshmallow (API 23).</para>
		/// </remarks>
		public Color? ForegroundColor
		{
			get
			{
				var foregroundType = GetStatusBarForegroundType();
				switch (foregroundType)
				{
					case StatusBarForegroundType.Light:
						return Colors.White;
					case StatusBarForegroundType.Dark:
						return Colors.Black;
					default:
						return null;
				}
			}
			set
			{
				if (!value.HasValue)
				{
					return;
				}

				var foregroundType = ColorToForegroundType(value.Value);

				SetStatusBarForegroundType(foregroundType);
			}
		}

		private StatusBarForegroundType ColorToForegroundType(Color color)
		{
			// Source: https://en.wikipedia.org/wiki/Luma_(video)
			var y = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;

			return y < 128
				? StatusBarForegroundType.Dark
				: StatusBarForegroundType.Light;
		}

		private enum StatusBarForegroundType { Light, Dark }

		[global::Uno.NotImplemented]
		public double BackgroundOpacity
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "double StatusBar.BackgroundOpacity");
				return 0;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "double StatusBar.BackgroundOpacity");
			}
		}

		[global::Uno.NotImplemented]
		public global::Windows.UI.Color? BackgroundColor
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "Color? StatusBar.BackgroundColor");
				return null;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.StatusBar", "Color? StatusBar.BackgroundColor");
			}

		}
	}
}
#endif
