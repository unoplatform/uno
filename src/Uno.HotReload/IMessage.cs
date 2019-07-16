namespace Uno.UI.HotReload
{
	public interface IMessage
	{
		string Scope { get; }

		string Name { get; }
	}
}
