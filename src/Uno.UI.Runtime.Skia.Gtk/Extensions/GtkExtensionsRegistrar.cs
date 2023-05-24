using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.ApplicationModel.Core;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.GTK.Extensions.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming;
using Uno.UI.Runtime.Skia.GTK.Extensions.System;
using Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.GTK.System.Profile;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.GTK.Extensions;

internal static class GtkExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(Uno.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
		ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new GtkCoreWindowExtension(o));
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new GtkApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new GtkSystemThemeHelperExtension(o));
		ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new GtkDisplayInformationExtension(o, _window));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new TextBoxViewExtension(o));
		ApiExtensibility.Register(typeof(ILauncherExtension), o => new LauncherExtension(o));
		ApiExtensibility.Register<FileOpenPicker>(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
		ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
		ApiExtensibility.Register<FileSavePicker>(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
		ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
		ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), o => new SystemNavigationManagerPreviewExtension(_window));
	}
}
