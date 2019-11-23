using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl
{
	internal interface IRemoteControlProcessor
	{
		string Scope { get; }

		Task Initialize();

		Task ProcessFrame(Frame frame);
	}
}
