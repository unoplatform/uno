namespace Uno.UI.RemoteControl
{
	public interface IMessage
	{
		string Scope { get; }

		string Name { get; }
	}
}
