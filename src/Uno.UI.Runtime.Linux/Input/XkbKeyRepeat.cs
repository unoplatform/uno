#nullable enable

using System;
using System.Threading;

namespace Uno.UI.Runtime.Linux.Input;

/// <summary>
/// Repeats the last pressed key, as Linux hosts do themselves: neither Wayland compositors nor libinput send repeats.
/// The callback runs on a timer thread with the key being repeated.
/// </summary>
internal sealed class XkbKeyRepeat(Action<uint> repeat) : IDisposable
{
	private readonly object _gate = new();
	private Timer? _timer;
	private int _generation;

	/// <summary>Repeats per second; zero disables repeating.</summary>
	public int Rate { get; set; } = 25;

	/// <summary>Milliseconds before the first repeat.</summary>
	public int Delay { get; set; } = 600;

	public uint? Key { get; private set; }

	internal void Start(uint key)
	{
		lock (_gate)
		{
			Stop();
			if (Rate <= 0)
			{
				return;
			}
			Key = key;
			var generation = _generation;
			_timer = new Timer(_ =>
			{
				lock (_gate)
				{
					if (generation != _generation)
					{
						return;
					}
				}
				repeat(key);
			}, null, Delay, Math.Max(1, 1000 / Rate));
		}
	}

	internal void Stop()
	{
		lock (_gate)
		{
			_generation++;
			_timer?.Dispose();
			_timer = null;
			Key = null;
		}
	}

	public void Dispose() => Stop();
}
