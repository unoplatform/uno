namespace Uno.ApplicationModel.Core;

internal interface ICoreApplicationExtension
{
	bool CanExit { get; }

	void Exit();
}
