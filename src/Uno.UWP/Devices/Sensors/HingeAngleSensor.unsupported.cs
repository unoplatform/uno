#if __IOS__ || __MACOS__ || __WASM__ || NET461
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class HingeAngleSensor
	{
		private HingeAngleSensor()
		{
		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static IAsyncOperation<HingeAngleSensor> GetDefaultAsync() => Task.FromResult<HingeAngleSensor>(null).AsAsyncOperation();
	}
}
#endif
