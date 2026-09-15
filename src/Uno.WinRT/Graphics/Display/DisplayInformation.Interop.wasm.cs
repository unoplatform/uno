using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.Graphics.Display
{
	internal partial class DisplayInformation
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Graphics.Display.DisplayInformation";

			[JSImport($"{JsType}.getDevicePixelRatio")]
			internal static partial float GetDevicePixelRatio();

			[JSImport($"{JsType}.getScreenHeight")]
			internal static partial float GetScreenHeight();

			[JSImport($"{JsType}.getScreenOrientationAngle")]
			internal static partial int? GetScreenOrientationAngle();

			[JSImport($"{JsType}.getScreenOrientationType")]
			internal static partial string GetScreenOrientationType();

			[JSImport($"{JsType}.getScreenWidth")]
			internal static partial float GetScreenWidth();

			[JSImport($"{JsType}.setOrientationAsync")]
			internal static partial Task SetOrientationAsync(int orientations);

			[JSImport($"{JsType}.startDpiChanged")]
			internal static partial void StartDpiChanged();

			[JSImport($"{JsType}.startOrientationChanged")]
			internal static partial void StartOrientationChanged();

			[JSImport($"{JsType}.stopDpiChanged")]
			internal static partial void StopDpiChanged();

			[JSImport($"{JsType}.stopOrientationChanged")]
			internal static partial void StopOrientationChanged();
		}
	}
}
