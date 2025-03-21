using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI;

internal class UIDebugLog
{
	internal void Log(string message)
	{

	}

	internal event EventHandler<string> MessageReceived;
}
