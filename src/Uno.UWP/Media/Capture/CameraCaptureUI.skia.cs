#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Media.Capture;
using Uno.Foundation.Extensibility;
using Windows.Storage;

namespace Windows.Media.Capture
{
	public partial class CameraCaptureUI
	{
		private ICameraCaptureUIExtension? _cameraCaptureUIExtension;

		partial void InitializePlatform() => ApiExtensibility.CreateInstance(this, out _cameraCaptureUIExtension);

		private async Task<StorageFile?> CaptureFile(CancellationToken ct, CameraCaptureUIMode mode)
		{
			if (_cameraCaptureUIExtension == null)
			{
				return null;
			}

			_cameraCaptureUIExtension.Customize(this, mode);
			return await _cameraCaptureUIExtension.CaptureFileAsync(ct);
		}
	}
}
