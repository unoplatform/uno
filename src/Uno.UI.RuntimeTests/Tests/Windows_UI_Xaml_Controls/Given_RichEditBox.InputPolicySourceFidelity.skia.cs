#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_InputPolicy_PageKeys_Centered_Fractional_Viewport_RoundTrip(bool upFirst, bool extend)
	{
		using var scope = RichEditBox.SetImeExtensionForTesting(new FakeImeTextBoxExtension());
		var editor = new RichEditBox
		{
			Width = 260,
			Height = 75,
			FontSize = 12,
			Padding = new Thickness(0),
			BorderThickness = new Thickness(0),
			Template = CreateTemplateParityTemplate("ScrollViewer"),
		};
		try
		{
			await UITestHelper.Load(editor);
			var text = string.Join('\r', Enumerable.Repeat("0123456789", 40));
			editor.Document.SetText(TextSetOptions.None, text);
			editor.Document.GetRange(0, text.Length).ParagraphFormat.SetLineSpacing(LineSpacingRule.Exactly, 15);
			const int anchor = 15 * 11 + 4;
			editor.Document.Selection.SetRange(anchor, anchor);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var parsed = GetDisplayBlock(editor).ParsedText;
			Assert.AreEqual(40, parsed.VisualLineCount);
			Assert.AreEqual(20d, parsed.GetVisualLine(15).Bounds.Height, 0.1);
			Assert.AreEqual(75d, GetGeometryScrollViewer(editor).ViewportHeight, 0.1);
			var firstTarget = (upFirst ? 11 : 19) * 11 + 4;
			var modifiers = extend ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None;

			var first = RaiseKeyForResult(editor, upFirst ? VirtualKey.PageUp : VirtualKey.PageDown, modifiers);

			Assert.IsTrue(first.Handled);
			Assert.AreEqual(extend ? Math.Min(anchor, firstTarget) : firstTarget, editor.Document.Selection.StartPosition);
			Assert.AreEqual(extend ? Math.Max(anchor, firstTarget) : firstTarget, editor.Document.Selection.EndPosition);
			Assert.AreEqual(extend && upFirst, editor.NativeSelectionIsBackward);

			var second = RaiseKeyForResult(editor, upFirst ? VirtualKey.PageDown : VirtualKey.PageUp, modifiers);

			Assert.IsTrue(second.Handled);
			Assert.AreEqual(anchor, editor.Document.Selection.StartPosition, "A fractional-page roundtrip must not drift by one line.");
			Assert.AreEqual(anchor, editor.Document.Selection.EndPosition);
			Assert.IsFalse(editor.NativeSelectionIsBackward);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_Reenable_Snapshots_State_Before_Reentrant_VisualState_Callback()
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var other = new Button { Content = "Nested enable change" };
		var editor = new RichEditBox
		{
			Width = 260,
			Height = 75,
			IsEnabled = false,
			AllowFocusWhenDisabled = true,
			Template = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="RichEditBox">
					<Grid>
						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver" />
								<VisualState x:Name="Focused" />
								<VisualState x:Name="Disabled" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
						<ScrollViewer x:Name="ContentElement" />
					</Grid>
				</ControlTemplate>
				"""),
		};
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { other, editor } });
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.AreNotSame(editor, ImeSessionCoordinator.ActiveHost);
			var root = VisualTreeHelper.GetChild(editor, 0) as FrameworkElement;
			Assert.IsNotNull(root);
			var states = VisualStateManager.GetVisualStateGroups(root).Single(group => group.Name == "CommonStates");
			var callbacks = 0;
			states.CurrentStateChanging += (_, _) =>
			{
				if (other.IsEnabled)
				{
					callbacks++;
					other.IsEnabled = false;
				}
			};

			editor.IsEnabled = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, callbacks, "The nested Button change must overwrite Control's reusable event args.");
			Assert.IsFalse(other.IsEnabled);
			Assert.AreSame(editor, ImeSessionCoordinator.ActiveHost);
			Assert.IsTrue(fake.StartImeSessionCallCount > 0);
			Assert.AreEqual(RichEditBox.RichEditCaretDisplayMode.ThumblessCaretShowing, editor.CaretMode);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
