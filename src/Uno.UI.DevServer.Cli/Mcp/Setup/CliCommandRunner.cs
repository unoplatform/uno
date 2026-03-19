using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Executes agent CLI commands for MCP registration.
/// When an agent provides a CLI (e.g. <c>claude mcp add</c>), this runner
/// handles executable detection, placeholder substitution, and process execution.
/// </summary>
internal class CliCommandRunner(ILogger logger)
{
	/// <summary>
	/// Checks whether <paramref name="cli"/>'s executable is available in PATH
	/// by running the detect command (typically <c>--version</c>).
	/// </summary>
	public virtual bool IsAvailable(CliProfile cli)
	{
		try
		{
			var (exitCode, _, _) = Execute(cli.Executable, cli.Detect, new Dictionary<string, object>(), ".", TimeSpan.FromSeconds(10));
			return exitCode == 0;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			logger.LogDebug(ex, "CLI '{Executable}' not available", cli.Executable);
			return false;
		}
	}

	/// <summary>
	/// Executes <paramref name="executable"/> with the given argument template,
	/// substituting placeholders from <paramref name="placeholders"/>.
	/// </summary>
	/// <remarks>
	/// Supported placeholders:
	/// <list type="bullet">
	///   <item><c>{name}</c> — server name (string)</item>
	///   <item><c>{command}</c> — command (string)</item>
	///   <item><c>{args...}</c> — arguments array (string[]), expanded in-place</item>
	///   <item><c>{url}</c> — server URL (string)</item>
	/// </list>
	/// </remarks>
	public virtual (int ExitCode, string Stdout, string Stderr) Execute(
		string executable,
		string[] argsTemplate,
		IDictionary<string, object> placeholders,
		string workingDirectory,
		TimeSpan? timeout = null)
	{
		var args = ExpandArgs(argsTemplate, placeholders);
		var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

		logger.LogDebug("Executing: {Executable} {Args} in {WorkingDirectory}",
			executable, string.Join(' ', args), workingDirectory);

		var psi = new ProcessStartInfo
		{
			FileName = executable,
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
		};

		foreach (var arg in args)
		{
			psi.ArgumentList.Add(arg);
		}

		using var process = Process.Start(psi)
			?? throw new InvalidOperationException($"Failed to start process: {executable}");

		var stdoutTask = process.StandardOutput.ReadToEndAsync();
		var stderrTask = process.StandardError.ReadToEndAsync();

		if (!process.WaitForExit((int)effectiveTimeout.TotalMilliseconds))
		{
			try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
			throw new TimeoutException($"CLI command '{executable}' timed out after {effectiveTimeout.TotalSeconds}s.");
		}

		var stdout = stdoutTask.GetAwaiter().GetResult();
		var stderr = stderrTask.GetAwaiter().GetResult();

		logger.LogDebug("CLI exit {ExitCode}, stdout={StdoutLength}b, stderr={StderrLength}b",
			process.ExitCode, stdout.Length, stderr.Length);

		return (process.ExitCode, stdout, stderr);
	}

	/// <summary>
	/// Expands an argument template by substituting placeholders.
	/// <c>{args...}</c> is expanded in-place to multiple arguments.
	/// </summary>
	internal static IReadOnlyList<string> ExpandArgs(
		string[] template,
		IDictionary<string, object> placeholders)
	{
		var result = new List<string>(template.Length);

		foreach (var token in template)
		{
			if (token == "{args...}")
			{
				if (placeholders.TryGetValue("args", out var argsValue) && argsValue is string[] argsArray)
				{
					result.AddRange(argsArray);
				}
			}
			else if (token.StartsWith('{') && token.EndsWith('}'))
			{
				var key = token[1..^1];
				if (placeholders.TryGetValue(key, out var value))
				{
					result.Add(value?.ToString() ?? string.Empty);
				}
				else
				{
					result.Add(token); // leave unresolved placeholder as-is
				}
			}
			else
			{
				result.Add(token);
			}
		}

		return result;
	}
}
