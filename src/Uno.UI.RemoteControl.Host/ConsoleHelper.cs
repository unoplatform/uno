using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host;

public static class ConsoleHelper
{
	public static CancellationTokenSource CreateCancellationToken()
	{
		// Set up graceful shutdown handling
		var cancellationTokenSource = new CancellationTokenSource();
		var shutdownRequested = false;

		Console.CancelKeyPress += (sender, e) =>
		{
			if (!cancellationTokenSource.IsCancellationRequested)
			{
				shutdownRequested = true;
				e.Cancel = true; // Prevent immediate termination
				Console.WriteLine("Graceful shutdown requested...");
				cancellationTokenSource.Cancel();
			}
		};

		// Monitor stdin for CTRL-C character (ASCII 3) for graceful shutdown
		_ = Task.Run(async () =>
			{
				try
				{
					if (!Console.IsInputRedirected)
						return;

					using var reader = new StreamReader(Console.OpenStandardInput());

					var buffer = new char[1];
					while (!cancellationTokenSource.Token.IsCancellationRequested)
					{
						var read = await reader.ReadAsync(buffer, 0, 1);
						if (read == 0)
						{
							break; // EOF
						}

						if (buffer[0] != '\x03') // CTRL-C (ASCII 3)
						{
							continue;
						}

						if (!shutdownRequested)
						{
							shutdownRequested = true;
							Console.WriteLine("Graceful shutdown requested via stdin...");
							await cancellationTokenSource.CancelAsync();
						}

						break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error monitoring stdin: {ex.Message}");
				}
			},
			cancellationTokenSource.Token);
		return cancellationTokenSource;
	}
}
