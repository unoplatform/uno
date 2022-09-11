#if NET6_0_OR_GREATER
using Microsoft.Build.Tasks;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	internal class Reporter : IReporter
	{
		public void Error(string message) => System.Console.WriteLine($"[Error] {message}");
		public void Output(string message) => System.Console.WriteLine($"[Output] {message}");
		public void Verbose(string message) => System.Console.WriteLine($"[Verbose] {message}");
		public void Warn(string message) => System.Console.WriteLine($"[Warn] {message}");
	}
}
#endif
