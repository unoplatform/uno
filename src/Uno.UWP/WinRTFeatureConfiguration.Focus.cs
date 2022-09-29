#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class Focus
		{
#if __IOS__ || __ANDROID__
			public static bool EnableExperimentalKeyboardFocus { get; set; }
#endif
		}
	}
}
