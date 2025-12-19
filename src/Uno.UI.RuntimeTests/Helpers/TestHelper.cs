using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class TestHelper
	{
		public static async Task RetryAssert(Action assertion, int count = 30)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= count)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}

		public static async Task RetryAssert(Func<Task> assertion, int count = 30)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					await assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= count)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}

		public static async Task<bool> TryWaitUntilCollected(WeakReference reference)
		{
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(3))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();

				if (!reference.IsAlive)
				{
					return true;
				}

				await Task.Delay(100);
			}

			return false;
		}
	}
}
