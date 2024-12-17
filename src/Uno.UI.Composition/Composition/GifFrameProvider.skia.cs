#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SkiaSharp;

namespace Windows.UI.Composition;

internal sealed class GifFrameProvider : IFrameProvider
{
	private readonly SKImage[] _images;
	private readonly SKCodecFrameInfo[]? _frameInfos;
	private readonly Timer? _timer;
	private readonly Stopwatch? _stopwatch;
	private readonly long _totalDuration;
	private readonly WeakReference<Action> _onFrameChanged;

	private int _currentFrame;
	private bool _disposed;

	// Note: The Timer will keep holding onto the ImageFrameProvider until stopped (it's a static root).
	// But we only stop the timer when we dispose ImageFrameProvider from SkiaCompositionSurface finalizer.
	// The onFrameChanged Action is also holding onto SkiaCompositionSurface.
	// So, if ImageFrameProvider holds onto onFrameChanged, the SkiaCompositionSurface is never GC'ed.
	// That's why we make it a WeakReference.
	// Note that SkiaCompositionSurface keeps an unused private field storing onFrameChanged so that it's not GC'ed early.
	internal GifFrameProvider(SKImage[] images, SKCodecFrameInfo[] frameInfos, long totalDuration, Action onFrameChanged)
	{
		_images = images;
		_frameInfos = frameInfos;
		_totalDuration = totalDuration;
		_onFrameChanged = new WeakReference<Action>(onFrameChanged);
		Debug.Assert(images.Length > 1);
		Debug.Assert(frameInfos is not null);
		Debug.Assert(totalDuration != 0);
		Debug.Assert(onFrameChanged is not null);

		if (_images.Length < 2)
		{
			throw new ArgumentException("GifFrameProvider should only be used when there is at least two frames");
		}

		_stopwatch = Stopwatch.StartNew();
		_timer = new Timer(OnTimerCallback, null, dueTime: _frameInfos![0].Duration, period: Timeout.Infinite);
	}

	public SKImage? CurrentImage => _images[_currentFrame];

	private int GetCurrentFrameIndex()
	{
		var currentTimestampInMilliseconds = _stopwatch!.ElapsedMilliseconds % _totalDuration;
		for (int i = 0; i < _frameInfos!.Length; i++)
		{
			if (currentTimestampInMilliseconds < _frameInfos[i].Duration)
			{
				return i;
			}

			currentTimestampInMilliseconds -= _frameInfos[i].Duration;
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
			nextFrameTimeStamp += _frameInfos![i].Duration;
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
			_timer?.Dispose();
			_disposed = true;
		}
	}
}
