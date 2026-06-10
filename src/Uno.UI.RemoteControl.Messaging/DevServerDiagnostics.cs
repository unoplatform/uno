#nullable enable
using System;
using System.Linq;
using System.Threading;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl;

public static class DevServerDiagnostics
{
	private static readonly AsyncLocal<ISink> _current = new();

	/// <summary>
	/// Gets the current (async-local) sink for dev-server diagnostics.
	/// </summary>
	public static ISink Current
	{
		get => _current.Value ?? NullSink.Instance;
		set => _current.Value = value;
	}

	/// <summary>
	/// Clears the current async-local sink, reverting to the default <see cref="NullSink"/>.
	/// Called during <see cref="RemoteControlClient"/> disposal to break the
	/// AsyncLocal → StatusSink → RemoteControlClient reference chain.
	/// </summary>
	public static void ResetCurrent()
		=> _current.Value = NullSink.Instance;

	public interface ISink
	{
		void ReportInvalidFrame<TContent>(Frame frame);
	}

	private class NullSink : ISink
	{
		public static NullSink Instance { get; } = new();

		public void ReportInvalidFrame<TContent>(Frame frame)
		{
		}
	}
}
