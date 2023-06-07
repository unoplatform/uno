using System;
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
	}
}
