using System;

namespace Uno.UI.TestComparer;

internal static class Helpers
{
	internal static void WriteLineWithTime(string text)
		=> Console.WriteLine($"{DateTime.Now.TimeOfDay}: {text}");
}
