#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow("javascript:void(0)", false)]
	[DataRow("JaVaScRiPt:void(0)", false)]
	[DataRow("tel:+15555550100", true)]
	[DataRow("contoso-shell:open", true)]
	public async Task When_Wasm_Approved_Link_Only_Dispatches_External_Navigation(string target, bool shouldLaunch)
	{
		var confirmationCount = 0;
		var editor = new RichEditBox
		{
			LinkConfirmationForTesting = _ =>
			{
				confirmationCount++;
				return Task.FromResult(true);
			},
		};
		WasmSemanticDomHelper.InvokeBrowserJs(
			"(function(){globalThis.__richEditBoxLaunchProbe={open:window.open,urls:[]};window.open=function(url){globalThis.__richEditBoxLaunchProbe.urls.push(url);return {};};return 'ok';})()");
		try
		{
			editor.Document.SetText(TextSetOptions.None, "link");
			editor.Document.GetRange(0, 4).Link = $"\"{target}\"";

			Assert.IsTrue(editor.TryNavigateLinkAt(1));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, confirmationCount);
			Assert.AreEqual(
				shouldLaunch ? "1" : "0",
				WasmSemanticDomHelper.InvokeBrowserJs("globalThis.__richEditBoxLaunchProbe.urls.length.toString()"));
			if (shouldLaunch)
			{
				Assert.AreEqual(
					target,
					WasmSemanticDomHelper.InvokeBrowserJs("globalThis.__richEditBoxLaunchProbe.urls[0]"));
			}
		}
		finally
		{
			WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){window.open=globalThis.__richEditBoxLaunchProbe.open;delete globalThis.__richEditBoxLaunchProbe;return 'ok';})()");
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Untrusted_Link_Waits_For_Explicit_Confirmation(bool approve)
	{
		var confirmation = new TaskCompletionSource<bool>();
		var launchCount = 0;
		var editor = new RichEditBox
		{
			LinkConfirmationForTesting = _ => confirmation.Task,
			LinkLauncherForTesting = _ =>
			{
				launchCount++;
				return Task.FromResult(true);
			},
		};
		editor.Document.SetText(TextSetOptions.None, "link");
		editor.Document.GetRange(0, 4).Link = "\"contoso-shell:open\"";

		Assert.IsTrue(editor.TryNavigateLinkAt(1));
		Assert.AreEqual(0, launchCount);
		confirmation.SetResult(approve);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(approve ? 1 : 0, launchCount);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Enter_Link_Uses_WinUI_Launcher_Policy(bool approve)
	{
		var confirmationCount = 0;
		var launchCount = 0;
		var editor = new RichEditBox
		{
			LinkConfirmationForTesting = _ =>
			{
				confirmationCount++;
				return Task.FromResult(approve);
			},
			LinkLauncherForTesting = _ =>
			{
				launchCount++;
				return Task.FromResult(true);
			},
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "link");
			editor.Document.GetRange(0, 4).Link = "\"tel:+15555550100\"";
			editor.Document.Selection.SetRange(1, 1);
			editor.IsReadOnly = true;
			Assert.IsTrue(editor.Focus(FocusState.Keyboard));
			await WindowHelper.WaitForIdle();

			RaiseKey(editor, VirtualKey.Enter, unicodeKey: '\r');
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, confirmationCount);
			Assert.AreEqual(approve ? 1 : 0, launchCount);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("link", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Untrusted_Link_Dialog_Uses_Owning_Root_And_Defaults_To_Cancel(bool approve)
	{
		var launched = false;
		var editor = new RichEditBox
		{
			LinkLauncherForTesting = _ =>
			{
				launched = true;
				return Task.FromResult(true);
			},
		};
		ContentDialog? dialog = null;
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "link");
			const string target = "contoso-shell:open";
			editor.Document.GetRange(0, 4).Link = $"\"{target}\"";

			Assert.IsTrue(editor.TryNavigateLinkAt(1));
			await WindowHelper.WaitFor(() =>
				(dialog = VisualTreeHelper.GetOpenPopupsForXamlRoot(editor.XamlRoot)
					.Select(popup => popup.Child).OfType<ContentDialog>().SingleOrDefault()) is not null);

			Assert.IsNotNull(dialog);
			Assert.AreSame(editor.XamlRoot, dialog.XamlRoot);
			Assert.AreEqual(ContentDialogButton.Close, dialog.DefaultButton);
			Assert.IsFalse(string.IsNullOrWhiteSpace(dialog.Title?.ToString()));
			var content = dialog.Content as string;
			Assert.IsNotNull(content);
			StringAssert.Contains(content, target);
			Assert.IsFalse(launched);

			var buttonName = approve ? "PrimaryButton" : "CloseButton";
			var button = VisualTreeUtils.FindVisualChildByName(dialog, buttonName) as Button;
			Assert.IsNotNull(button);
			await WindowHelper.WaitForLoaded(button);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;
			Assert.IsNotNull(peer);
			peer.Invoke();
			await WindowHelper.WaitFor(() => !dialog._popup.IsOpen);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(approve, launched);
		}
		finally
		{
			dialog?.Hide();
			WindowHelper.WindowContent = null;
		}
	}
}
