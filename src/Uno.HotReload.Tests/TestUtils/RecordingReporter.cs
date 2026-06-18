using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// Captures every message routed through the tracker's reporter so tests can
/// assert on warnings/errors emitted by the hot-reload pipeline.
/// </summary>
internal sealed class RecordingReporter : IReporter
{
	private readonly List<string> _verbose = [];
	private readonly List<string> _output = [];
	private readonly List<string> _warnings = [];
	private readonly List<string> _errors = [];
	private readonly Lock _gate = new();

	public IReadOnlyList<string> Warnings
	{
		get
		{
			lock (_gate)
			{
				return [.. _warnings];
			}
		}
	}

	public IReadOnlyList<string> Errors
	{
		get
		{
			lock (_gate)
			{
				return [.. _errors];
			}
		}
	}

	public void Verbose(string message)
	{
		lock (_gate)
		{
			_verbose.Add(message);
		}
	}

	public void Output(string message)
	{
		lock (_gate)
		{
			_output.Add(message);
		}
	}

	public void Warn(string message)
	{
		lock (_gate)
		{
			_warnings.Add(message);
		}
	}

	public void Error(string message)
	{
		lock (_gate)
		{
			_errors.Add(message);
		}
	}
}
