using Microsoft.Build.Tasks;

namespace Uno.HotReload.Tracking;

/// <summary>
/// Provides an implementation of the <see cref="IReporter"/> interface that writes messages to the console output.
/// </summary>
/// <remarks>This class formats messages with a severity label and writes them to the standard console output. It
/// is suitable for use in command-line applications, mcp or scenarios where immediate feedback to the user is
/// required.</remarks>
public class ConsoleReporter : IReporter
{
	public void Error(string message) => System.Console.WriteLine($"[Error] {message}");
	public void Output(string message) => System.Console.WriteLine($"[Output] {message}");
	public void Verbose(string message) => System.Console.WriteLine($"[Verbose] {message}");
	public void Warn(string message) => System.Console.WriteLine($"[Warn] {message}");
}
