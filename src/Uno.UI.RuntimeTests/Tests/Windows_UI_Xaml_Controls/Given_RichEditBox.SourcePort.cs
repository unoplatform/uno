#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
#if HAS_UNO
using Uno.Helpers.Theming;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_SourcePort_EmptyPlaceholder_Collapses()
	{
		var editor = new RichEditBox { Width = 280, Height = 100 };
		try
		{
			await UITestHelper.Load(editor);
			var placeholder = FindSourcePortPlaceholder(editor);
			Assert.IsNotNull(placeholder);
			Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility);

			editor.PlaceholderText = "Document hint";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, placeholder.Visibility);

			editor.PlaceholderText = string.Empty;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SourcePort_VisiblePlaceholder_UsesPinnedAccessibilityPolicy()
	{
		var editor = new RichEditBox { Width = 280, Height = 100, PlaceholderText = "Document hint" };
		try
		{
			await UITestHelper.Load(editor);
			var placeholder = FindSourcePortPlaceholder(editor);
			Assert.IsNotNull(placeholder);
			Assert.AreEqual(Visibility.Visible, placeholder.Visibility);
			Assert.AreEqual(AccessibilityView.Control, AutomationProperties.GetAccessibilityView(placeholder));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SourcePort_ContentClearsPlaceholderDescription()
	{
#if HAS_UNO
		var editor = new RichEditBox { Width = 280, Height = 100, PlaceholderText = "Document hint" };
		try
		{
			await UITestHelper.Load(editor);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			Assert.AreEqual(1, peer.GetDescribedBy()?.Count() ?? 0);
			var placeholder = FindSourcePortPlaceholder(editor);
			Assert.IsNotNull(placeholder);
			Assert.IsTrue(AutomationProperties.GetDescribedBy(editor).Contains(placeholder));

			editor.Document.SetText(TextSetOptions.None, "Document content");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility);
			Assert.IsFalse(AutomationProperties.GetDescribedBy(editor).Contains(placeholder));
			Assert.AreEqual(0, peer.GetDescribedBy()?.Count() ?? 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_SourcePort_SelectionChangingArgs_AreReusedAndReset()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "abcd");
		editor.Document.Selection.SetRange(0, 0);
		RichEditBoxSelectionChangingEventArgs? first = null;
		var count = 0;
		editor.SelectionChanging += (_, args) =>
		{
			Assert.IsFalse(args.Cancel);
			if (++count == 1)
			{
				first = args;
				args.Cancel = true;
			}
			else
			{
				Assert.AreSame(first, args);
				Assert.AreEqual(2, args.SelectionStart);
				Assert.AreEqual(1, args.SelectionLength);
			}
		};

		editor.Document.Selection.SetRange(1, 2);
		Assert.AreEqual(0, editor.Document.Selection.StartPosition);
		editor.Document.Selection.SetRange(2, 3);
		Assert.AreEqual(2, editor.Document.Selection.StartPosition);
		Assert.AreEqual(2, count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_SourcePort_TextChangingArgs_AreReused()
	{
		var editor = new RichEditBox();
		RichEditBoxTextChangingEventArgs? first = null;
		RichEditBoxTextChangingEventArgs? second = null;
		var count = 0;
		editor.TextChanging += (_, args) =>
		{
			if (++count == 1)
			{
				first = args;
			}
			else
			{
				second = args;
			}
		};

		editor.Document.SetText(TextSetOptions.None, "first");
		editor.Document.SetText(TextSetOptions.None, "second");
		Assert.AreEqual(2, count);
		Assert.IsNotNull(first);
		Assert.IsTrue(first.IsContentChanging);
		Assert.AreSame(first, second);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SourcePort_NonDeferredHeader_CollapsesWithoutContent()
	{
#if HAS_UNO
		var editor = new RichEditBox
		{
			Width = 280,
			Height = 100,
			Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="RichEditBox">
					<StackPanel>
						<ContentPresenter x:Name="HeaderContentPresenter" Height="20" Content="{TemplateBinding Header}" />
						<ScrollViewer x:Name="ContentElement" Height="70" />
					</StackPanel>
				</ControlTemplate>
				"""),
		};
		try
		{
			await UITestHelper.Load(editor);
			var header = editor.GetTemplateChild("HeaderContentPresenter") as ContentPresenter;
			Assert.IsNotNull(header);
			Assert.AreEqual(Visibility.Collapsed, header.Visibility);
			editor.Header = "Visible header";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, header.Visibility);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_SourcePort_InputScope_DefaultsToNull()
	{
		var editor = new RichEditBox();
		Assert.IsNull(editor.InputScope);
		editor.InputScope = new InputScope();
		Assert.IsNotNull(editor.InputScope);
		editor.ClearValue(RichEditBox.InputScopeProperty);
		Assert.IsNull(editor.InputScope);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_SourcePort_SelectionChanging_SameProposedRange_DoesNotOverrideCancel()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "abcd");
		editor.Document.Selection.SetRange(0, 0);
		var count = 0;
		editor.SelectionChanging += (_, args) =>
		{
			if (++count == 1)
			{
				editor.Document.Selection.SetRange(args.SelectionStart, args.SelectionStart + args.SelectionLength);
				args.Cancel = true;
			}
		};

		editor.Document.Selection.SetRange(1, 3);

		Assert.AreEqual(0, editor.Document.Selection.StartPosition);
		Assert.AreEqual(0, editor.Document.Selection.EndPosition);
		Assert.AreEqual(1, count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_SourcePort_PersonalFullName_RespectsExplicitSpellChecking(bool explicitlyEnabled)
	{
		var editor = new RichEditBox
		{
			Width = 280,
			Height = 100,
			InputScope = new InputScope
			{
				Names = { new InputScopeName { NameValue = InputScopeNameValue.PersonalFullName } },
			},
		};
		if (explicitlyEnabled)
		{
			editor.IsSpellCheckEnabled = true;
		}
		try
		{
			await UITestHelper.Load(editor);
			Assert.AreEqual(explicitlyEnabled, editor.IsSpellCheckEnabled);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_SourcePort_AutoHeightAnimation_CompletesAndUnloads()
	{
#if HAS_UNO
		var editor = new RichEditBox { Width = 280, TextWrapping = TextWrapping.Wrap, IsSpellCheckEnabled = false };
		var panel = new StackPanel { Children = { editor } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "first");
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var initialHeight = editor.ActualHeight;

			editor.Document.SetText(TextSetOptions.None, "first\rsecond\rthird\rfourth\rfifth");
			editor.UpdateLayout();
			Assert.IsTrue(editor.IsHeightAnimationRunningForTesting);

			await WindowHelper.WaitFor(
				() => !editor.IsHeightAnimationRunningForTesting,
				timeoutMS: 3000,
				message: "The automatic-height storyboard did not complete.");
			Assert.IsFalse(editor.IsHeightAnimationRunningForTesting);
			Assert.IsTrue(double.IsNaN(editor.Height));
			Assert.IsTrue(editor.ActualHeight > initialHeight);

			editor.Document.SetText(TextSetOptions.None, "short");
			editor.UpdateLayout();
			Assert.IsTrue(editor.IsHeightAnimationRunningForTesting);
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(editor.IsHeightAnimationRunningForTesting);
			Assert.IsTrue(double.IsNaN(editor.Height));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SourcePort_FocusLoss_ClearsCandidateWindowBounds()
	{
#if HAS_UNO
		var fake = new FakeImeTextBoxExtension();
		using var ime = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 280, Height = 100 };
		var button = new Button { Content = "Other focus" };
		var panel = new StackPanel { Children = { editor, button } };
		var bounds = new List<Rect>();
		try
		{
			await UITestHelper.Load(panel);
			editor.CandidateWindowBoundsChanged += (_, args) => bounds.Add(args.Bounds);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var candidate = new Rect(10, 20, 30, 40);
			fake.SimulateCandidateWindowBoundsChanged(candidate);
			Assert.IsTrue(button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { candidate, default(Rect) }, bounds);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SourcePort_AutomationFocus_AllowsSoftwareKeyboard()
	{
#if HAS_UNO
		var fake = new FakeImeTextBoxExtension();
		using var ime = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 280, Height = 100, PreventKeyboardDisplayOnProgrammaticFocus = true };
		var button = new Button { Content = "Other focus" };
		var panel = new StackPanel { Children = { button, editor } };
		try
		{
			await UITestHelper.Load(panel);
			Assert.IsTrue(button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			peer.SetFocus();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FocusState.Pointer, fake.LastActivation.FocusState);
			Assert.IsFalse(fake.LastActivation.IsSoftwareKeyboardSuppressed);
			Assert.IsNotNull(editor.XamlRoot);
			Assert.AreSame(editor, FocusManager.GetFocusedElement(editor.XamlRoot));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_SourcePort_UrlInputScope_DisablesLinkNotifications()
	{
#if HAS_UNO
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "Uno");
		editor.Document.GetRange(0, 3).Link = "\"https://platform.uno\"";
		var launched = 0;
		editor.LinkLauncherForTesting = _ =>
		{
			launched++;
			return Task.FromResult(true);
		};
		editor.InputScope = new InputScope
		{
			Names = { new InputScopeName { NameValue = InputScopeNameValue.Url } },
		};
		Assert.IsFalse(editor.TryNavigateLinkAt(1));
		Assert.AreEqual(0, launched);

		editor.InputScope = new InputScope();
		Assert.IsTrue(editor.TryNavigateLinkAt(1));
		Assert.AreEqual(1, launched);
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SourcePort_Math_PreservesHighContrastRendering()
	{
#if HAS_UNO
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;
		var colors = CreateSourcePortHighContrastColors();
		var editor = new RichEditBox
		{
			Width = 280,
			Height = 160,
			FontSize = 32,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
			IsSpellCheckEnabled = false,
		};
		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = colors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast Black";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			editor.Document.SetMathML("<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>MMMM</mi><mi>WWWW</mi></mfrac></math>");
			editor.Document.GetRange(0, int.MaxValue).CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Red;
			await UITestHelper.Load(editor);

			var adjusted = await UITestHelper.ScreenShot(editor);
			Assert.IsTrue(CountSourcePortColor(adjusted, Microsoft.UI.Colors.White) > 20);
			Assert.AreEqual(0, CountSourcePortColor(adjusted, Microsoft.UI.Colors.Red),
				"Math glyph assemblies and fraction rules must use the system foreground.");

			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			editor.Document.Selection.SetRange(0, int.MaxValue);
			await WindowHelper.WaitForIdle();
			var selected = await UITestHelper.ScreenShot(editor);
			Assert.IsTrue(CountSourcePortColor(selected, Microsoft.UI.Colors.Yellow) > 20,
				"Selected structured math must use the system selection foreground.");
			Assert.IsTrue(CountSourcePortColor(selected, Microsoft.UI.Colors.Blue) > 100);

			editor.Document.Selection.SetRange(0, 0);
			editor.HighContrastAdjustment = ElementHighContrastAdjustment.None;
			await WindowHelper.WaitForIdle();
			var unadjusted = await UITestHelper.ScreenShot(editor);
			Assert.IsTrue(CountSourcePortColor(unadjusted, Microsoft.UI.Colors.Red) > 20);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SourcePort_ListMarkers_UseHighContrastForeground(bool terminalParagraph)
	{
#if HAS_UNO
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;
		var text = terminalParagraph ? "body\r" : "body";
		var editor = new RichEditBox
		{
			Width = 280,
			Height = 180,
			FontSize = 32,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
			IsSpellCheckEnabled = false,
		};
		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = CreateSourcePortHighContrastColors();
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast Black";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			editor.Document.SetText(TextSetOptions.None, text);
			var range = editor.Document.GetRange(0, int.MaxValue);
			range.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Red;
			var position = terminalParagraph ? text.Length : 0;
			var paragraph = editor.Document.GetRange(position, position).ParagraphFormat;
			paragraph.SetIndents(0, 36, 0);
			paragraph.ListType = MarkerType.Arabic;
			paragraph.ListStyle = MarkerStyle.Period;
			paragraph.ListStart = 1;
			paragraph.ListTab = 18;
			await UITestHelper.Load(editor);

			var block = GetDisplayBlock(editor);
			if (terminalParagraph)
			{
				Assert.AreEqual("1.", block.EndingParagraphLayout?.MarkerText);
			}
			var caret = block.ParsedText.GetRectForIndex(position);
			var screenshot = await UITestHelper.ScreenShot(block);
			Assert.AreEqual(0, CountSourcePortColor(screenshot, Microsoft.UI.Colors.Red));
			Assert.IsTrue(CountSourcePortColor(
				screenshot,
				Microsoft.UI.Colors.White,
				maxX: (int)(caret.Left + block.Padding.Left) - 2,
				minY: (int)(caret.Top + block.Padding.Top),
				maxY: (int)Math.Ceiling(caret.Bottom + block.Padding.Top)) > 1,
				"The list marker before the paragraph text must remain visible in high contrast.");
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
#else
		await Task.CompletedTask;
#endif
	}

#if HAS_UNO
	private static HighContrastSystemColors CreateSourcePortHighContrastColors() => new(
		ButtonFaceColor: Microsoft.UI.Colors.Black,
		ButtonTextColor: Microsoft.UI.Colors.White,
		GrayTextColor: Microsoft.UI.Colors.Gray,
		HighlightColor: Microsoft.UI.Colors.Blue,
		HighlightTextColor: Microsoft.UI.Colors.Yellow,
		HotlightColor: Microsoft.UI.Colors.Yellow,
		WindowColor: Microsoft.UI.Colors.Black,
		WindowTextColor: Microsoft.UI.Colors.White,
		ActiveCaptionColor: Microsoft.UI.Colors.Black,
		BackgroundColor: Microsoft.UI.Colors.Black,
		CaptionTextColor: Microsoft.UI.Colors.White,
		InactiveCaptionColor: Microsoft.UI.Colors.Black,
		InactiveCaptionTextColor: Microsoft.UI.Colors.Gray,
		DisabledTextColor: Microsoft.UI.Colors.Gray);
#endif

	private static int CountSourcePortColor(
		RawBitmap bitmap,
		global::Windows.UI.Color expected,
		int minX = 0,
		int maxX = int.MaxValue,
		int minY = 0,
		int maxY = int.MaxValue)
	{
		var count = 0;
		for (var y = Math.Max(0, minY); y < Math.Min(bitmap.Height, maxY); y++)
		{
			for (var x = Math.Max(0, minX); x < Math.Min(bitmap.Width, maxX); x++)
			{
				var actual = bitmap.GetPixel(x, y);
				if (actual.A > 200
					&& Math.Abs(actual.R - expected.R) <= 2
					&& Math.Abs(actual.G - expected.G) <= 2
					&& Math.Abs(actual.B - expected.B) <= 2)
				{
					count++;
				}
			}
		}
		return count;
	}

	private static TextBlock? FindSourcePortPlaceholder(DependencyObject root)
	{
		if (root is TextBlock { Name: "PlaceholderTextContentPresenter" } placeholder)
		{
			return placeholder;
		}
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			if (FindSourcePortPlaceholder(VisualTreeHelper.GetChild(root, i)) is { } result)
			{
				return result;
			}
		}
		return null;
	}
}
