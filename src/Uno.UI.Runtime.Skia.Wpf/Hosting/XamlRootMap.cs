using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Hosting
{
	internal static class XamlRootMap
    {
		private static Dictionary<XamlRoot, IWpfHost> _map = new Dictionary<XamlRoot, IWpfHost>();

		public static void Register(XamlRoot xamlRoot, IWpfHost host)
		{
			_map[xamlRoot] = host;
		}

		public static void Unregister(XamlRoot xamlRoot)
		{
			_map.Remove(xamlRoot);
		}

		public static IWpfHost GetHostForRoot(XamlRoot xamlRoot)
		{
			return _map[xamlRoot];
		}
	}
}
