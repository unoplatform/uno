using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using System.Linq;

namespace Uno.UI.RuntimeTests.Extensions
{
	internal static class FrameExtensions
	{
		public static int CountAllMyPages(this Frame sut) => sut.EnumerateAllChildren(v => v is Page).Count();

		/// Actively waiting for pages to be stacked is
		/// required as NativeFramePresenter.UpdateStack awaits
		/// for animations to finish, and there's no way to determine
		/// from the Frame PoV that the animation is finished.
		public static async Task WaitForPages(this Frame frame, int count)
		{
			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < TimeSpan.FromSeconds(5))
			{
				await TestServices.WindowHelper.WaitForIdle();

				if (frame.CountAllMyPages() == count)
				{
					break;
				}
			}

			Assert.AreEqual(count, frame.CountAllMyPages());
		}
	}
}
