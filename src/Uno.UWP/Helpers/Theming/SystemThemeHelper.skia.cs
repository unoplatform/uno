#nullable enable

using Uno.Foundation.Extensibility;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static ISystemThemeHelperExtension? _systemThemeHelperExtension;

		private static SystemTheme GetSystemTheme()
		{
			if (_systemThemeHelperExtension == null)
			{
				ApiExtensibility.CreateInstance(typeof(SystemThemeHelper), out _systemThemeHelperExtension);
			}

			return _systemThemeHelperExtension?.GetSystemTheme() ?? SystemTheme.Light;
		}
	}
}
