#nullable enable

using System;
using System.Diagnostics;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching;

internal class FrameRateLogger
{
	private long _lastFrameShow = Stopwatch.GetTimestamp();
	private int _frameCounter;
	private Type _owner;
	private string _name;

	public FrameRateLogger(Type owner, string name)
	{
		_owner = owner;
		_name = name;
	}

	public void ReportFrame()
	{
		var now = Stopwatch.GetTimestamp();
		var elapsed = Stopwatch.GetElapsedTime(_lastFrameShow).TotalSeconds;
		_frameCounter++;

		if (Stopwatch.GetElapsedTime(_lastFrameShow).TotalSeconds >= 1)
		{
			var fps = Math.Round(_frameCounter / elapsed, 2);

			if (_owner.Log().IsDebugEnabled())
			{
				_owner.Log().Debug($"{_name}/s = {fps}");
			}

			Console.WriteLine($"{_name}/s = {fps}");

			_frameCounter = 0;
			_lastFrameShow = now;
		}

	}
}
