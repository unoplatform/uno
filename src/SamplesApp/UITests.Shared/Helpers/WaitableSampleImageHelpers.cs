using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Shared.Helpers;

internal static class WaitableSampleImageHelpers
{
	private static Task WaitImage(ImageBrush image)
	{
		var tcs = new TaskCompletionSource();
		image.ImageOpened += (_, _) => tcs.SetResult();
		image.ImageFailed += (_, _) => tcs.SetResult();
		return tcs.Task;
	}

	private static Task WaitImage(Image image)
	{
		var tcs = new TaskCompletionSource();
		image.ImageOpened += (_, _) => tcs.SetResult();
		image.ImageFailed += (_, _) => tcs.SetResult();
		return tcs.Task;
	}

	private static Task WaitImage(SvgImageSource image)
	{
		var tcs = new TaskCompletionSource();
		image.Opened += (_, _) => tcs.SetResult();
		image.OpenFailed += (_, _) => tcs.SetResult();
		return tcs.Task;
	}

	private static Task WaitImage(BitmapImage image)
	{
		var tcs = new TaskCompletionSource();
		image.ImageOpened += (_, _) => tcs.SetResult();
		image.ImageFailed += (_, _) => tcs.SetResult();
		return tcs.Task;
	}

	public static Task WaitAllImages(params ImageBrush[] images)
		=> Task.WhenAll(images.Select(WaitImage));

	public static Task WaitAllImages(params Image[] images)
		=> Task.WhenAll(images.Select(WaitImage));

	public static Task WaitAllImages(params SvgImageSource[] images)
		=> Task.WhenAll(images.Select(WaitImage));

	public static Task WaitAllImages(params BitmapImage[] images)
		=> Task.WhenAll(images.Select(WaitImage));
}
