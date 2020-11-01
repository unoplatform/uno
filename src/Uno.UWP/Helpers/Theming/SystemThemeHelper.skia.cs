using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static SystemTheme GetSystemTheme()
		{
		}
	}

	internal interface ISystemThemeHelperExtension
	{
		SystemTheme GetDefaultSystemTheme();
	}
}
