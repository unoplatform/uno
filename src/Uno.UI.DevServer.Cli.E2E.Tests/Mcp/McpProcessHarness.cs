using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal sealed class McpProcessHarness : IAsyncDisposable
{
	private readonly Process _process;
	private readonly Channel<string> _stdoutLines = Channel.CreateUnbounded<string>();
	private Task _stdoutPump = Task.CompletedTask;
	private Task _stderrPump = Task.CompletedTask;

	private McpProcessHarness(
		Process process,
		StringBuilder standardOutput,
		StringBuilder standardError)
	{
		_process = process;
		StandardOutput = standardOutput;
		StandardError = standardError;
	}

	public StringBuilder StandardOutput { get; }

	public StringBuilder StandardError { get; }

	public static McpProcessHarness Start(
		CliCommandSpec command,
		string workingDirectory,
		bool forceRootsFallback)
	{
		var arguments = command.Arguments.ToList();
		arguments.Add("--mcp-app");
		arguments.Add("-l");
		arguments.Add("trace");
		if (forceRootsFallback)
		{
			arguments.Add("--force-roots-fallback");
		}

		var startInfo = new CliCommandSpec(command.FileName, arguments).CreateStartInfo(workingDirectory);

		var process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true,
		};

		process.Start().Should().BeTrue("the MCP bridge process must start for process-backed integration tests");

		var standardOutput = new StringBuilder();
		var standardError = new StringBuilder();
		var harness = new McpProcessHarness(process, standardOutput, standardError);

		harness._stdoutPump = Task.Run(async () =>
		{
			string? line;
			while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
			{
				standardOutput.AppendLine(line);
				await harness._stdoutLines.Writer.WriteAsync(line);
			}

			harness._stdoutLines.Writer.TryComplete();
		});

		harness._stderrPump = Task.Run(async () =>
		{
			string? line;
			while ((line = await process.StandardError.ReadLineAsync()) is not null)
			{
				standardError.AppendLine(line);
			}
		});

		return harness;
	}

	public async Task WriteLineAsync(string message, CancellationToken ct)
	{
		await _process.StandardInput.WriteLineAsync(message.AsMemory(), ct);
		await _process.StandardInput.FlushAsync();
	}

	public async Task<JsonDocument> ReadResponseAsync(int requestId, TimeSpan timeout)
	{
		using var timeoutCts = new CancellationTokenSource(timeout);

		try
		{
			while (await _stdoutLines.Reader.WaitToReadAsync(timeoutCts.Token))
			{
				while (_stdoutLines.Reader.TryRead(out var line))
				{
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}

					var json = JsonDocument.Parse(line);
					var root = json.RootElement;
					if (root.TryGetProperty("id", out var idElement)
						&& idElement.ValueKind == JsonValueKind.Number
						&& idElement.GetInt32() == requestId)
					{
						return json;
					}

					json.Dispose();
				}
			}
		}
		catch (OperationCanceledException)
		{
			throw new TimeoutException($"Timed out waiting for MCP response {requestId}.{Environment.NewLine}STDOUT:{Environment.NewLine}{StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{StandardError}");
		}

		throw new TimeoutException($"The MCP process stopped producing output before response {requestId} was received.{Environment.NewLine}STDOUT:{Environment.NewLine}{StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{StandardError}");
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			if (!_process.HasExited)
			{
				try
				{
					_process.Kill(entireProcessTree: true);
				}
				catch
				{
					// Best-effort cleanup for process-backed tests.
				}
			}

			await _process.WaitForExitAsync();
		}
		catch
		{
			// Ignore teardown failures.
		}

		try
		{
#pragma warning disable VSTHRD003
			await _stdoutPump;
			await _stderrPump;
#pragma warning restore VSTHRD003
		}
		catch
		{
			// Ignore background reader failures during teardown.
		}

		_process.Dispose();
	}
}
