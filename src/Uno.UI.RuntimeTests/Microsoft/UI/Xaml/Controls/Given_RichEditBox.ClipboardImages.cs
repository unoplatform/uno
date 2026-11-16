#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_Tom_Pastes_BitmapOnly_Clipboard_As_One_Object()
		{
			const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==";
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.Document.Selection.SetRange(1, 3);
				var package = new DataPackage();
				package.SetBitmap(RandomAccessStreamReference.CreateFromStream(
					new MemoryStream(Convert.FromBase64String(pngBase64)).AsRandomAccessStream()));
				Clipboard.SetContent(package);
				await WindowHelper.WaitForIdle();

				SUT.Document.Selection.Paste(0);
				await WindowHelper.WaitFor(() =>
				{
					SUT.Document.GetText(TextGetOptions.None, out var value);
					return value.StartsWith("a\ufffcd", StringComparison.Ordinal);
				});

				SUT.Document.GetRange(0, 3).GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("a\ufffcd", text);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}
	}
}
