#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Threading;

namespace Windows.Storage.Helpers
{
	internal partial class ConcurrentEntryManager
	{
		private readonly Dictionary<string, DownloadEntry> _assetsGate = new Dictionary<string, DownloadEntry>();

		public async Task<IDisposable> LockForAsset(CancellationToken ct, string updatedPath)
		{
			DownloadEntry GetEntry()
			{
				lock (_assetsGate)
				{
					if (!_assetsGate.TryGetValue(updatedPath, out var entry))
					{
						_assetsGate[updatedPath] = entry = new DownloadEntry();
					}

					entry.ReferenceCount++;

					return entry;
				}
			}

			var entry = GetEntry();

			var disposable = await entry.Gate.LockAsync(ct);

			void ReleaseEntry()
			{
				lock (_assetsGate)
				{
					disposable.Dispose();

					if(--entry.ReferenceCount == 0)
					{
						_assetsGate.Remove(updatedPath);
					}
				}
			}

			return Disposable.Create(ReleaseEntry);
		}

		private class DownloadEntry
		{
			public int ReferenceCount;
			public AsyncLock Gate { get; } = new AsyncLock();
		}
	}
}
