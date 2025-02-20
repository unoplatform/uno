using System.Runtime.InteropServices;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Extensions.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.Gtk.Extensions.Helpers.Theming;
using Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Gtk.System.Profile;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Extensions.System;
#pragma warning disable CS0649
namespace Uno.UI.Runtime.Skia.Gtk.Extensions;

internal static class GtkExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension());
		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoKeyboardInputSource), o => new GtkKeyboardInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.IUnoCorePointerInputSource), o => new GtkCorePointerInputSource(o));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new GtkNativeElementHostingExtension(o));
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new GtkApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new GtkSystemThemeHelperExtension(o));
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => new GtkDisplayInformationExtension(o));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new TextBoxViewExtension(o));
		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
		ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
		ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
		ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
		ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => new SystemNavigationManagerPreviewExtension());

		ApiExtensibility.Register(typeof(ILauncherExtension), o =>
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new WindowsLauncherExtension(o);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return new LinuxLauncherExtension(o);
			}

			return null;
		});
	}
}
