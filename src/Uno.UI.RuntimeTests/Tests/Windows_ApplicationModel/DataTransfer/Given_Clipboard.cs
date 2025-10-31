#if __ANDROID__ || __IOS__ || __WASM__

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

// Testing Append, Write and Read in one test method

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_Clipboard
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Put_And_Get()
		{
			try
			{
				var text = "some text which should be intact";

				var package = new DataPackage();
				package.SetText(text);
#if WINAPPSDK
				Clipboard.SetContent(package);
#else
				await Clipboard.SetContentAsync(package);
#endif

				var clipboardView = Clipboard.GetContent();
				var textFromClipboard = await clipboardView.GetTextAsync();

				Assert.AreEqual(text, textFromClipboard, false);
			}
			finally
			{
				Clipboard.Clear();
			}
		}

#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Uri_Content()
		{
			try
			{
				var uri = new Uri("https://platform.uno");
				var package = new DataPackage();
				package.SetUri(uri);
#if WINAPPSDK
				Clipboard.SetContent(package);
#else
				await Clipboard.SetContentAsync(package);
#endif

				var clipboardView = Clipboard.GetContent();
				var uriFromClipboard = await clipboardView.GetUriAsync();

				Assert.AreEqual(uri, uriFromClipboard);
			}
			finally
			{
				Clipboard.Clear();
			}
		}
#endif

#if __ANDROID__ || __WASM__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Html_Content()
		{
			try
			{
				var html = "<p>This is <strong>bold</strong> text</p>";
				var package = new DataPackage();
				package.SetHtmlFormat(html);
#if WINAPPSDK
				Clipboard.SetContent(package);
#else
				await Clipboard.SetContentAsync(package);
#endif

				var clipboardView = Clipboard.GetContent();
				var htmlFromClipboard = await clipboardView.GetHtmlFormatAsync();

				Assert.AreEqual(html, htmlFromClipboard);
			}
			finally
			{
				Clipboard.Clear();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Html_And_Text_Content()
		{
			try
			{
				var html = "<p>This is <strong>bold</strong> text</p>";
				var text = "This is bold text";
				var package = new DataPackage();
				package.SetHtmlFormat(html);
				package.SetText(text);
#if WINAPPSDK
				Clipboard.SetContent(package);
#else
				await Clipboard.SetContentAsync(package);
#endif

				var clipboardView = Clipboard.GetContent();
				var htmlFromClipboard = await clipboardView.GetHtmlFormatAsync();
				var textFromClipboard = await clipboardView.GetTextAsync();

				Assert.AreEqual(html, htmlFromClipboard);
				Assert.AreEqual(text, textFromClipboard);
			}
			finally
			{
				Clipboard.Clear();
			}
		}
#endif
	}
}

#endif
