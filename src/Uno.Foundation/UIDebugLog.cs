using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI;

internal static class UIDebugLog
{
	internal static void Log(string message)
	{
		MessageReceived?.Invoke(null, message);
	}

	internal static event EventHandler<string> MessageReceived;
}
