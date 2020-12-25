#if !__WASM__ && !__IOS__

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
    {
		private DataTransferManager()
		{
		}

		public static bool IsSupported() => false;

		public static DataTransferManager GetForCurrentView() => throw new NotSupportedException("DataTransferManager is not yet supported on this platform.");
    }
}
#endif
