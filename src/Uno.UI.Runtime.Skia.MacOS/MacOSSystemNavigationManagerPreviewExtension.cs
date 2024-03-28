using Windows.UI.Xaml;

using Uno.Foundation.Extensibility;
using Uno.UI.Core.Preview;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSSystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
{
	private static readonly MacOSSystemNavigationManagerPreviewExtension _instance = new();

	public static void Register()
	{
		ApiExtensibility.Register(typeof(ISystemNavigationManagerPreviewExtension), _ => _instance);
	}

	private MacOSSystemNavigationManagerPreviewExtension()
	{
	}

	public void RequestNativeAppClose() => Window.Current?.Close();
}
