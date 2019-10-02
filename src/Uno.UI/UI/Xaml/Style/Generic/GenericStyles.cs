using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public static partial class GenericStyles
	{
		private static bool _initialized = false;

		public static void Initialize()
		{
			if (!_initialized)
			{
				_initialized = true;

				InitStyles();
			}
		}

		static partial void InitStyles();
	}
}
