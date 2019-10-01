using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using Photos;
using UIKit;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;

namespace Windows.Media.Capture
{
	public partial class CameraCaptureUI
	{
		private async Task<StorageFile> CaptureFile(CancellationToken ct, CameraCaptureUIMode mode)
		{
			UIImagePickerController picker = null;
			var cameraDelegate = new CameraDelegate();

			if (UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera))
			{
				picker = new UIImagePickerController();
				picker.Delegate = cameraDelegate;
				picker.AllowsEditing = PhotoSettings.AllowCropping;

				picker.SourceType = UIImagePickerControllerSourceType.Camera;

				switch (mode)
				{
					case CameraCaptureUIMode.Photo:
					case CameraCaptureUIMode.PhotoOrVideo:
						picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
						break;

					case CameraCaptureUIMode.Video:
						picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
						break;
				}
			}
			else
			{
				// Probably running in the simulator
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().WarnFormat("'{0}' not available - picking from albums", UIImagePickerControllerSourceType.Camera);
				}

				picker = new LockedOrientationUIImagePickerController(DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask())
				{
					// Use the camera roll instead of crashing
					SourceType = UIImagePickerControllerSourceType.PhotoLibrary
				};

				await ValidatePhotoLibraryAccess();
			}

			picker.Delegate = cameraDelegate;
			UIKit.UIApplication.SharedApplication.KeyWindow?.RootViewController.PresentModalViewController(picker, true);

			using (ct.Register(() => picker.DismissViewController(true, () => { }), useSynchronizationContext: true))
			{
				var result = await cameraDelegate.Task;

				if (result != null)
				{
					var image = result.ValueForKey(new NSString("UIImagePickerControllerOriginalImage")) as UIImage;
					var metadata = result.ValueForKey(new NSString("UIImagePickerControllerOriginalImage")) as UIImage;

					var correctedImage = await FixOrientation(ct, image);

					(Stream data, string extension) GetImageStream()
					{
						switch (PhotoSettings.Format)
						{
							case CameraCaptureUIPhotoFormat.Jpeg:
								return (image.AsJPEG().AsStream(), ".jpg");

							case CameraCaptureUIPhotoFormat.Png:
								return (image.AsPNG().AsStream(), ".png");

							default:
								throw new NotSupportedException($"{PhotoSettings.Format} is not supported");
						}
					};

					var (data, extension) = GetImageStream();
					return await CreateTempImage(data, extension);
				}
				else
				{
					return null;
				}
			}
		}


		// As of iOS 10, usage description keys are required for many more permissions
		private static bool IsUsageKeyDefined(string usageKey)
		{
			return UIDevice.CurrentDevice.CheckSystemVersion(10, 0)
				? NSBundle.MainBundle.ObjectForInfoDictionary(usageKey) != null
				: true;
		}

		private async Task ValidateCameraAccess()
		{
			if (!IsUsageKeyDefined("NSCameraUsageDescription"))
			{
				throw new InvalidOperationException("Info.plist must define NSCameraUsageDescription");
			}

			var isAllowed = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
			if (!isAllowed)
			{
				throw new UnauthorizedAccessException();
			}
		}

		private async Task ValidatePhotoLibraryAccess()
		{
			if (!IsUsageKeyDefined("NSPhotoLibraryUsageDescription"))
			{
				throw new InvalidOperationException("Info.plist must define NSPhotoLibraryUsageDescription");
			}

			var isAllowed = (await PHPhotoLibrary.RequestAuthorizationAsync()) == PHAuthorizationStatus.Authorized;
			if (!isAllowed)
			{
				throw new UnauthorizedAccessException();
			}
		}

		private class LockedOrientationUIImagePickerController : UIImagePickerController
		{
			private readonly UIInterfaceOrientationMask _supportedOrientations;

			public LockedOrientationUIImagePickerController(UIInterfaceOrientationMask supportedOrientations)
			{
				_supportedOrientations = supportedOrientations;
			}

			public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
			{
				return _supportedOrientations;
			}
		}

		/// <summary>
		/// Fixes orientation issues caused by taking an image straight from the camera
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <param name="image">UI Image</param>
		/// <returns>UI image</returns>
		private async Task<UIImage> FixOrientation(CancellationToken ct, UIImage image)
		{
			if (image.Orientation == UIImageOrientation.Up)
			{
				return image;
			}

			// http://stackoverflow.com/questions/5427656/ios-uiimagepickercontroller-result-image-orientation-after-upload
			UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);

			image.Draw(new CGRect(0, 0, image.Size.Width, image.Size.Height));
			var normalizedImage = UIGraphics.GetImageFromCurrentImageContext();

			UIGraphics.EndImageContext();

			return normalizedImage;
		}
	}

	class CameraDelegate : UIImagePickerControllerDelegate
	{
		private readonly TaskCompletionSource<NSDictionary> _cts;

		public CameraDelegate()
		{
			_cts = new TaskCompletionSource<NSDictionary>();
			Task = _cts.Task;
		}

		public Task<NSDictionary> Task { get; }

		public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
		{
			_cts.TrySetResult(info);
			picker.DismissModalViewController(true);
		}

		public override void Canceled(UIImagePickerController picker)
		{
			base.Canceled(picker);

			_cts.TrySetResult(null);
		}
	}
}
