#if !__ANDROID__ && !__IOS__
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.Lights
{
	public partial class Lamp
	{
		private Lamp()
		{

		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static IAsyncOperation<Lamp?> GetDefaultAsync() => Task.FromResult<Lamp?>(null).AsAsyncOperation();
	}
}
#endif
