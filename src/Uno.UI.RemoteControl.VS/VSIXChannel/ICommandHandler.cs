using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.IDE;

internal interface ICommandHandler
{
	/*
	* WARNING WARNING WARNING WARNING WARNING WARNING WARNING
	*
	* This interface is shared between Uno's VS extension and the Uno.RC package, make sure to keep in sync.
	* In order to avoid versioning issues, avoid modifications and **DO NOT** remove any member from this interface.
	*
	*/

	void Execute(Command command);
}
