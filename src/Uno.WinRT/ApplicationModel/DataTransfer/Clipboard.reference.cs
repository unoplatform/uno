using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		internal static bool IsTextAvailable() => false;

		private static void StartContentChanged()
		{
		}

		private static void StopContentChanged()
		{
		}
	}
}
