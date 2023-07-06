using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl;

public interface IRemoteControlPreProcessor
{
	Task<bool> SkipProcessingFrame(Frame frame);
}
