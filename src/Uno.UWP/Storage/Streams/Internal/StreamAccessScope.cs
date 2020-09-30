using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Uno.Storage.Streams.Internal
{
	internal class StreamAccessScope
    {
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

		public IDisposable Begin()
		{
			_semaphore.Wait();

			return Disposable.Create(() =>
			{
				_semaphore.Release();
			});
		}

		public async Task<IDisposable> BeginAsync()
		{
			await _semaphore.WaitAsync();

			return Disposable.Create(() =>
			{
				_semaphore.Release();
			});
		}
    }
}
