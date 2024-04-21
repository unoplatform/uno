using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UITests.Shared.Helpers;

internal static class WaitableSampleImageHelpers
{
	public static Task WaitAllImages(params ImageBrush[] images)
	{
		int counter = 0;
		var tcs = new TaskCompletionSource();
		foreach (var image in images)
		{
			image.ImageOpened += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};

			image.ImageFailed += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};
		}

		return tcs.Task;
	}

	public static Task WaitAllImages(params Image[] images)
	{
		int counter = 0;
		var tcs = new TaskCompletionSource();
		foreach (var image in images)
		{
			image.ImageOpened += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};

			image.ImageFailed += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};
		}

		return tcs.Task;
	}

	public static Task WaitAllImages(params BitmapImage[] images)
	{
		int counter = 0;
		var tcs = new TaskCompletionSource();
		foreach (var image in images)
		{
			image.ImageOpened += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};

			image.ImageFailed += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};
		}

		return tcs.Task;
	}

	public static Task WaitAllImages(params SvgImageSource[] images)
	{
		int counter = 0;
		var tcs = new TaskCompletionSource();
		foreach (var image in images)
		{
			image.Opened += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};

			image.OpenFailed += (_, _) =>
			{
				counter++;
				if (counter == images.Length)
				{
					tcs.SetResult();
				}
			};
		}

		return tcs.Task;
	}
}
