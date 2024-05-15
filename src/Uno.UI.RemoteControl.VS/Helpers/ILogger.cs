using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.VS.Helpers;

internal interface ILogger
{
	void Info(string message);

	void Debug(string message);

	void Warn(string message);

	void Error(string message);
	void Verbose(string message);
}
