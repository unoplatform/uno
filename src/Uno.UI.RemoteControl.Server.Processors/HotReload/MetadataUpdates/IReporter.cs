#if NET6_0_OR_GREATER
namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	/// <summary>
	/// This API supports infrastructure and is not intended to be used
	/// directly from your code. This API may change or be removed in future releases.
	/// </summary>
	public interface IReporter
	{
		void Verbose(string message);
		void Output(string message);
		void Warn(string message);
		void Error(string message);
	}
}
#endif
