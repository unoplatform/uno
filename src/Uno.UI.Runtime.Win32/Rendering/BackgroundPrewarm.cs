#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// Creates an expensive resource on a background thread while startup continues, and hands it to a single consumer.
/// Any failure (creation, or the consumer finding it unusable) falls back to the consumer's synchronous path, so
/// the warm-up can only make startup faster, never change its outcome. Single-use: call either Claim or Discard.
/// </summary>
internal sealed class BackgroundPrewarm<T> where T : class, IDisposable
{
	private readonly Task<T> _task;

	public BackgroundPrewarm(Func<T> create)
		=> _task = Task.Factory.StartNew(create, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

	/// <summary>
	/// Waits for the warmed resource and lets <paramref name="tryComplete"/> finish it for its target. When creation
	/// failed, or completion returns false or throws, the resource is disposed and <paramref name="createFallback"/> runs.
	/// </summary>
	public T Claim(Func<T, bool> tryComplete, Func<T> createFallback)
	{
		T? warmed = null;
		try
		{
			var waitStart = Stopwatch.GetTimestamp();
			warmed = _task.GetAwaiter().GetResult();
			var waited = Stopwatch.GetElapsedTime(waitStart);
			if (tryComplete(warmed))
			{
				this.LogDebug()?.Debug($"Using the prewarmed {typeof(T).Name} (waited {waited.TotalMilliseconds:F0} ms for it).");
				return warmed;
			}

			this.LogInfo()?.Info($"The prewarmed {typeof(T).Name} is not usable for this target; creating it synchronously.");
		}
		catch (Exception e)
		{
			this.LogInfo()?.Info($"The prewarmed {typeof(T).Name} failed ({e.Message}); creating it synchronously.");
		}

		warmed?.Dispose();
		return createFallback();
	}

	/// <summary>Disposes the resource once created, for when no consumer will claim it.</summary>
	public void Discard()
		=> _task.ContinueWith(
			static t =>
			{
				if (t.IsCompletedSuccessfully)
				{
					t.Result.Dispose();
				}
				else
				{
					_ = t.Exception;
				}
			},
			CancellationToken.None,
			TaskContinuationOptions.None,
			TaskScheduler.Default);
}
