﻿using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.IDE;

public interface IUnoDevelopmentEnvironmentIndicator
{
	/*
	* WARNING WARNING WARNING WARNING WARNING WARNING WARNING
	*
	* This interface is shared between Uno's VS extension and the Uno.RC package, make sure to keep in sync.
	* In order to avoid versioning issues, avoid modifications and **DO NOT** remove any member from this interface.
	*
	*/

	ValueTask NotifyAsync(DevelopmentEnvironmentStatusIdeMessage message, CancellationToken ct);
}
