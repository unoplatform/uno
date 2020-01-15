using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SamplesApp.UITests;

namespace UITests.Shared.Helpers
{
	public static class UITestHelper
	{
		public static bool CompareScreenShots(FileInfo img1, FileInfo img2, float thresholdPercentage = 1f) => CompareScreenShots(
			new Bitmap(img1.FullName),
			new Bitmap(img2.FullName),
			thresholdPercentage
		);

		public static bool CompareScreenShots(Bitmap image1, Bitmap image2, float thresholdPercentage = 1f)
		{
			float diffPercentage = 0;
			float diff = 0;

			if (image1.Size != image2.Size)
			{
				return false;
			}
			else
			{
				for (int x = 0; x < image1.Width; x++)
				{
					for (int y = 0; y < image1.Height; y++)
					{
						Color img1P = image1.GetPixel(x, y);
						Color img2P = image2.GetPixel(x, y);

						diff += Math.Abs(img1P.R - img2P.R);
						diff += Math.Abs(img1P.G - img2P.G);
						diff += Math.Abs(img1P.B - img2P.B);
					}
				}

				diffPercentage = 100 * (diff / 255) / (image1.Width * image1.Height * 3);
				if (diffPercentage < thresholdPercentage)
				{
					return false;
				}
			}
			return true;
		}
	}
}
