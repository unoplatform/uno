#if !__WASM__

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
    {
		public static bool IsSupported() => false;

		public static DataTransferManager GetForCurrentView() => throw new NotSupportedException("DataTransferManager is not yet supported on this platform.");
    }
}
#endif
