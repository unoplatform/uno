#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal sealed class AnimatedImageFrameProvider : IFrameProvider
{
	private readonly SKImage[] _images;
	private readonly int[] _durations;
	private readonly Timer? _timer;
	private readonly Stopwatch? _stopwatch;
	private readonly long _totalDuration;
	private readonly long _memoryPressure;
	private readonly WeakReference<Action> _onFrameChanged;

	private int _currentFrame;
	private bool _disposed;

	// Note: The Timer will keep holding onto the AnimatedImageFrameProvider until stopped (it's a static root).
	// But we only stop the timer when we dispose AnimatedImageFrameProvider from SkiaCompositionSurface finalizer.
	// The onFrameChanged Action is also holding onto SkiaCompositionSurface.
	// So, if AnimatedImageFrameProvider holds onto onFrameChanged, the SkiaCompositionSurface is never GC'ed.
	// That's why we make it a WeakReference.
	// Note that SkiaCompositionSurface keeps an unused private field storing onFrameChanged so that it's not GC'ed early.
	internal AnimatedImageFrameProvider(SKImage[] images, int[] durations, long totalDuration, Action onFrameChanged)
	{
		_images = images;
		_durations = durations;
		_totalDuration = totalDuration;
		_onFrameChanged = new WeakReference<Action>(onFrameChanged);
		Debug.Assert(images.Length > 1);
		Debug.Assert(durations is not null);
		Debug.Assert(durations.Length == images.Length);
		Debug.Assert(totalDuration != 0);
		Debug.Assert(onFrameChanged is not null);

		if (_images.Length < 2)
		{
			throw new ArgumentException("AnimatedImageFrameProvider should only be used when there is at least two frames");
		}

		long pressure = 0;
		for (int i = 0; i < _images.Length; i++)
		{
			pressure += _images[i].Info.BytesSize;
		}

		_memoryPressure = pressure;
		GC.AddMemoryPressure(_memoryPressure);

		_stopwatch = Stopwatch.StartNew();
		_timer = new Timer(OnTimerCallback, null, dueTime: _durations[0], period: Timeout.Infinite);
	}

	public SKImage? CurrentImage => _images[_currentFrame];

	private int GetCurrentFrameIndex()
	{
		var currentTimestampInMilliseconds = _stopwatch!.ElapsedMilliseconds % _totalDuration;
		for (int i = 0; i < _durations.Length; i++)
		{
			if (currentTimestampInMilliseconds < _durations[i])
			{
				return i;
			}

			currentTimestampInMilliseconds -= _durations[i];
		}

		throw new InvalidOperationException("This shouldn't be reachable. A timestamp in total duration range should map to a frame");
	}

	private void SetCurrentFrame()
	{
		var frameIndex = GetCurrentFrameIndex();
		if (_currentFrame != frameIndex)
		{
			_currentFrame = frameIndex;
			Debug.Assert(_onFrameChanged is not null);
			if (_onFrameChanged.TryGetTarget(out var onFrameChanged))
			{
				onFrameChanged();
			}
		}
	}

	private void OnTimerCallback(object? state)
	{
		SetCurrentFrame();

		var timestamp = _stopwatch!.ElapsedMilliseconds % _totalDuration;
		var nextFrameTimeStamp = 0;
		for (int i = 0; i <= _currentFrame; i++)
		{
			nextFrameTimeStamp += _durations[i];
		}

		var dueTime = nextFrameTimeStamp - timestamp;
		if (dueTime < 0)
		{
			// Defensive check. When pausing the program for debugging, the calculations can go wrong.
			dueTime = 16;
		}

		try
		{
			_timer!.Change(dueTime, period: Timeout.Infinite);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_timer?.Dispose();

			for (int i = 0; i < _images.Length; i++)
			{
				_images[i].Dispose();
			}

			GC.RemoveMemoryPressure(_memoryPressure);
		}
	}
}
