namespace Uno.IDE;

#pragma warning disable CS1031
internal interface ICommandHandlerRegistry
{
	void Register(string name, ICommandHandler handler);

	void Unregister(ICommandHandler handler);
}
