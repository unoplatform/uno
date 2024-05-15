using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host;

internal class ParentProcessObserver
{
	internal static IDisposable? Observe(Microsoft.AspNetCore.Hosting.IWebHost host, int ppid)
	{
		if (ppid != 0)
		{
			return new Timer(ParentProcessWatchDog, ppid, 10_000, 10_000);
		}

		return null;
	}

	static void ParentProcessWatchDog(object? state)
	{
		var ppid = (int)state!;
		try
		{
			if (ppid != 0)
			{
				Process.GetProcessById(ppid);
			}
		}
		catch (ArgumentException)
		{
			Console.Error.WriteLine($"Parent process {ppid} not found, exiting...");
			Environment.Exit(4);
		}
	}
}
