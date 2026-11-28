using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Links added, replaced and removed after load stay reachable by pointer, including on a RichTextBlock whose only
	// reason to take pointer input is its links. SkiaWasm is excluded (mouse injection), as in Given_RichTextBlock_Input.
	[TestClass]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public class Given_RichTextBlock_Hyperlinks_Input
	{
		private static async Task ClickAt(Point point)
		{
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.MoveTo(point);
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();
			mouse.Release();
			await WindowHelper.WaitForIdle();
		}

		private static Hyperlink CreateClickMeLink(Action onClick)
		{
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(new Run { Text = "CLICKMECLICKME" });
			hyperlink.Click += (_, _) => onClick();
			return hyperlink;
		}

		[TestMethod]
		[Timeout(120_000)]
		[DataRow("block")]
		[DataRow("inline")]
		[DataRow("span")]
		[DataRow("replace")]
		public async Task When_Link_Added_After_Load_Is_Clicked(string change)
		{
			// Not selectable: only a link subscribes this RichTextBlock to pointer input.
			var SUT = new RichTextBlock { Width = 400, FontSize = 20, TextWrapping = TextWrapping.NoWrap, IsTextSelectionEnabled = false };
			var existing = new Paragraph();
			existing.Inlines.Add(new Run { Text = "Plain text" });
			SUT.Blocks.Add(existing);

			var clicks = 0;
			var hyperlink = CreateClickMeLink(() => clicks++);

			try
			{
				await UITestHelper.Load(SUT);

				switch (change)
				{
					case "block":
						var added = new Paragraph();
						added.Inlines.Add(hyperlink);
						SUT.Blocks.Insert(0, added);
						break;
					case "inline":
						existing.Inlines.Insert(0, hyperlink);
						break;
					case "span":
						var span = new Span();
						span.Inlines.Add(hyperlink);
						existing.Inlines.Insert(0, span);
						break;
					case "replace":
						var replacement = new Paragraph();
						replacement.Inlines.Add(hyperlink);
						SUT.Blocks[0] = replacement;
						break;
				}

				await WindowHelper.WaitForIdle();

				var bounds = SUT.GetAbsoluteBounds();
				await ClickAt(new Point(bounds.X + 12, bounds.Y + 10));

				Assert.AreEqual(1, clicks, $"A link added after load ({change}) should be clickable");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		public async Task When_Link_Removed_After_Load_Is_Not_Clicked()
		{
			var SUT = new RichTextBlock { Width = 400, FontSize = 20, TextWrapping = TextWrapping.NoWrap, IsTextSelectionEnabled = false };
			var clicks = 0;
			var linkParagraph = new Paragraph();
			linkParagraph.Inlines.Add(CreateClickMeLink(() => clicks++));
			SUT.Blocks.Add(linkParagraph);
			var plain = new Paragraph();
			plain.Inlines.Add(new Run { Text = "Plain text" });
			SUT.Blocks.Add(plain);

			try
			{
				await UITestHelper.Load(SUT);
				var bounds = SUT.GetAbsoluteBounds();
				var onLink = new Point(bounds.X + 12, bounds.Y + 10);

				await ClickAt(onLink);
				Assert.AreEqual(1, clicks, "The link should be clickable before it is removed");

				SUT.Blocks.Remove(linkParagraph);
				await WindowHelper.WaitForIdle();
				await ClickAt(onLink);

				Assert.AreEqual(1, clicks, "A removed link must not be activated");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[Timeout(120_000)]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Pressed_Link_Removed_It_Keeps_No_Pointer_State(bool collapse)
		{
			// The list of links is rebuilt at the next read or measure, which a collapsed control doesn't get: a removed
			// link must lose its state when it leaves the content.
			var SUT = new RichTextBlock { Width = 400, FontSize = 20, TextWrapping = TextWrapping.NoWrap };
			var clicks = 0;
			var hyperlink = CreateClickMeLink(() => clicks++);
			var linkParagraph = new Paragraph();
			linkParagraph.Inlines.Add(hyperlink);
			SUT.Blocks.Add(linkParagraph);
			var plain = new Paragraph();
			plain.Inlines.Add(new Run { Text = "Plain text" });
			SUT.Blocks.Add(plain);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			try
			{
				await UITestHelper.Load(SUT);
				var bounds = SUT.GetAbsoluteBounds();
				mouse.MoveTo(new Point(bounds.X + 12, bounds.Y + 10));
				await WindowHelper.WaitForIdle();
				mouse.Press();
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(hyperlink.HasPointerState, "The press should reach the link");

				if (collapse)
				{
					SUT.Visibility = Visibility.Collapsed;
				}
				SUT.Blocks.Remove(linkParagraph);
				Assert.IsFalse(hyperlink.HasPointerState, "A removed link keeps no pressed or hover state");

				await WindowHelper.WaitForIdle();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, clicks, "A removed link must not be activated");
				Assert.IsFalse(hyperlink.HasPointerState);
			}
			finally
			{
				mouse.Release();
				WindowHelper.WindowContent = null;
			}
		}
	}
}
