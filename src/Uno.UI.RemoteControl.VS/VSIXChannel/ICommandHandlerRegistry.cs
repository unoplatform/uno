namespace Uno.IDE;

internal interface ICommandHandlerRegistry
{
	void Register(string name, ICommandHandler handler);

	void Unregister(ICommandHandler handler);
}
