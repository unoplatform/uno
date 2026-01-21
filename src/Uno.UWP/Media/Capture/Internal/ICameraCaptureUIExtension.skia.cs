using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.Extensions.Media.Capture;

internal interface ICameraCaptureUIExtension
{
	void Customize(global::Windows.Media.Capture.CameraCaptureUI picker, global::Windows.Media.Capture.CameraCaptureUIMode mode);

	Task<StorageFile?> CaptureFileAsync(CancellationToken token);
}
