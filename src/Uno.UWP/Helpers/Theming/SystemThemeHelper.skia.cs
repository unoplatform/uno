#nullable enable

using Uno.Foundation.Extensibility;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static ISystemThemeHelperExtension? _systemThemeHelperExtension;

		/// <summary>
		/// Call this method from Skia target when the system theme might have changed.
		/// SystemThemeHelper will verify if the change actually took place and raises
		/// SystemThemeChanged event in that case.
		/// </summary>
		internal static void NotifySystemChanges() => RefreshSystemTheme();

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
