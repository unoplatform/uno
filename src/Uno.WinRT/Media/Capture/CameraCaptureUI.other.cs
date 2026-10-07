#if !__IOS__ && !__ANDROID__ && !__SKIA__
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Windows.Media.Capture
{
	public partial class CameraCaptureUI
	{
		private Task<StorageFile> CaptureFile(CancellationToken arg, CameraCaptureUIMode mode)
		{
			return Task.FromResult<StorageFile>(null);
		}
	}
}
#endif
