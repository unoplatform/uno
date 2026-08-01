using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_IME_Surface_Defaults_Match_WinUI()
	{
		var sut = new RichEditBox();

		Assert.AreEqual(CandidateWindowAlignment.Default, sut.DesiredCandidateWindowAlignment);
		Assert.IsFalse(sut.PreventKeyboardDisplayOnProgrammaticFocus);
		Assert.IsTrue(sut.IsTextPredictionEnabled);
	}

	[TestMethod]
	public void When_Normalized_Native_Selection_Echo_Preserves_The_Active_Start()
	{
		var sut = new RichEditBox();
		sut.Document.SetText(TextSetOptions.None, "abc");
		sut.Document.Selection.SetRange(0, 3);
		sut.Document.Selection.Options |= SelectionOptions.StartActive;

		sut.SelectFromNative(selectionStart: 0, selectionLength: 3);

		Assert.IsTrue(sut.IsSelectionBackwardForTesting);
		Assert.AreEqual(0, sut.Document.Selection.StartPosition);
		Assert.AreEqual(3, sut.Document.Selection.EndPosition);
	}

	[TestMethod]
	public async Task When_Candidate_Window_Alignment_Changes_While_Focused()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			fake.Updates.Clear();

			sut.DesiredCandidateWindowAlignment = CandidateWindowAlignment.BottomEdge;

			CollectionAssert.AreEqual(
				new[] { ImeSessionUpdate.CandidateWindowAlignment },
				fake.Updates);
			Assert.AreEqual(CandidateWindowAlignment.BottomEdge, ((IImeSessionHost)sut).DesiredCandidateWindowAlignment);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Text_Selection_Layout_And_Scroll_Change_Update_IME()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox
		{
			AcceptsReturn = true,
			Height = 80,
			Width = 240,
		};
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.Updates.Clear();
			sut.Document.SetText(TextSetOptions.None, "first\rsecond\rthird\rfourth\rfifth");
			await WindowHelper.WaitForIdle();
			CollectionAssert.Contains(fake.Updates, ImeSessionUpdate.TextAndSelection);

			fake.Updates.Clear();
			sut.Document.Selection.SetRange(2, 2);
			await WindowHelper.WaitForIdle();
			CollectionAssert.Contains(fake.Updates, ImeSessionUpdate.TextAndSelection);

			fake.Updates.Clear();
			sut.Width = 260;
			await WindowHelper.WaitForIdle();
			CollectionAssert.Contains(fake.Updates, ImeSessionUpdate.TextAndSelection);

			if (((ITextBoxViewHost)sut).ContentElement is ScrollViewer scrollViewer)
			{
				scrollViewer.VerticalScrollMode = ScrollMode.Enabled;
				scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
				sut.Document.SetText(
					TextSetOptions.None,
					string.Join('\r', Enumerable.Range(0, 40).Select(static index => $"line {index}")));
				sut.UpdateLayout();
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(scrollViewer.ScrollableHeight > 0);

				fake.Updates.Clear();
				scrollViewer.ChangeView(null, Math.Min(20, scrollViewer.ScrollableHeight), null, disableAnimation: true);
				await WindowHelper.WaitForIdle();
				CollectionAssert.Contains(fake.Updates, ImeSessionUpdate.TextAndSelection);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Candidate_Window_Bounds_Are_Reported()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var expected = new Rect(12, 34, 56, 78);
			RichEditBox sender = null;
			CandidateWindowBoundsChangedEventArgs firstArgs = null;
			CandidateWindowBoundsChangedEventArgs secondArgs = null;
			sut.CandidateWindowBoundsChanged += (eventSender, args) =>
			{
				sender = eventSender;
				if (firstArgs is null)
				{
					firstArgs = args;
				}
				else
				{
					secondArgs = args;
				}
			};

			fake.SimulateCandidateWindowBoundsChanged(expected);
			fake.SimulateCandidateWindowBoundsChanged(expected);

			Assert.AreSame(sut, sender);
			Assert.AreEqual(expected, firstArgs.Bounds);
			Assert.AreEqual(expected, secondArgs.Bounds);
			Assert.AreNotSame(firstArgs, secondArgs);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Linguistic_Alternatives_Preserve_Order_And_Context()
	{
		var fake = new FakeImeTextBoxExtension
		{
			LinguisticAlternatives = new[] { "你", "泥", "拟" },
		};
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "AB");
			sut.Document.Selection.SetRange(1, 1);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");

			var alternatives = await sut.GetLinguisticAlternativesAsync();

			Assert.AreEqual("ni", fake.LastCompositionText);
			CollectionAssert.AreEqual(
				new[] { "A你B", "A泥B", "A拟B" },
				new List<string>(alternatives));

			fake.SimulateCompositionComplete("ni");
			alternatives = await sut.GetLinguisticAlternativesAsync();
			Assert.AreEqual(0, alternatives.Count);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Linguistic_Alternatives_Swallow_Backend_Fault_And_Propagate_Cancellation()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");

			fake.LinguisticAlternativesFactory = (_, _) =>
				Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("backend failure"));
			var alternatives = await sut.GetLinguisticAlternativesAsync();
			Assert.AreEqual(0, alternatives.Count);

			fake.LinguisticAlternativesFactory = async (_, cancellationToken) =>
			{
				await Task.Delay(Timeout.Infinite, cancellationToken);
				return Array.Empty<string>();
			};
			var operation = sut.GetLinguisticAlternativesAsync();
			operation.Cancel();
			try
			{
				await operation;
				Assert.Fail("Cancellation should propagate.");
			}
			catch (OperationCanceledException)
			{
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Programmatic_Focus_Suppresses_Only_Software_Keyboard()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox
		{
			PreventKeyboardDisplayOnProgrammaticFocus = true,
		};
		var other = new Button();
		try
		{
			WindowHelper.WindowContent = new StackPanel
			{
				Children =
				{
					sut,
					other,
				},
			};
			await WindowHelper.WaitForLoaded(sut);

			Assert.IsTrue(other.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(sut.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				fake.Activations.Exists(static activation =>
					activation.FocusState == FocusState.Programmatic &&
					activation.IsSoftwareKeyboardSuppressed),
				$"Expected a suppressed programmatic activation, got: {string.Join(", ", fake.Activations)}");
			Assert.IsTrue(sut.IsCaretRenderedForTesting);
			Assert.AreSame(sut, ImeSessionCoordinator.ActiveHost);

			Assert.IsTrue(other.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(sut.Focus(FocusState.Pointer));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FocusState.Pointer, fake.LastActivation.FocusState);
			Assert.IsFalse(fake.LastActivation.IsSoftwareKeyboardSuppressed);
			Assert.AreSame(sut, ImeSessionCoordinator.ActiveHost);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Focused_Input_Options_Change_Update_IME()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			fake.Updates.Clear();

			sut.InputScope = new InputScope
			{
				Names =
				{
					new InputScopeName { NameValue = InputScopeNameValue.EmailSmtpAddress },
				},
			};
			sut.IsTextPredictionEnabled = false;
			sut.AcceptsReturn = false;
			sut.IsSpellCheckEnabled = false;

			CollectionAssert.AreEqual(
				new[]
				{
					ImeSessionUpdate.InputScope,
					ImeSessionUpdate.TextPrediction,
					ImeSessionUpdate.AcceptsReturn,
					ImeSessionUpdate.SpellCheck,
				},
				fake.Updates);
			Assert.IsTrue(sut.IsComposing);
			Assert.AreEqual(InputScopeNameValue.EmailSmtpAddress, sut.InputScope.Names[0].NameValue);
			Assert.IsFalse(sut.IsTextPredictionEnabled);
			Assert.IsFalse(sut.AcceptsReturn);
			Assert.IsFalse(sut.IsSpellCheckEnabled);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Focused_Input_Options_Refresh_DOM_Input()
	{
		var sut = new RichEditBox
		{
			InputScope = new InputScope
			{
				Names =
				{
					new InputScopeName { NameValue = InputScopeNameValue.EmailSmtpAddress },
				},
			},
			IsSpellCheckEnabled = true,
		};
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await WindowHelper.WaitFor(() =>
				global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
					"(function(){const input=document.getElementById('uno-input');return input ? `${input.getAttribute('inputmode')}|${input.spellcheck}` : 'missing';})()")
				== "email|true");

			sut.InputScope = new InputScope
			{
				Names =
				{
					new InputScopeName { NameValue = InputScopeNameValue.Number },
				},
			};
			sut.IsSpellCheckEnabled = false;
			sut.AcceptsReturn = false;
			await WindowHelper.WaitForIdle();

			await WindowHelper.WaitFor(() =>
				global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
					"(function(){const input=document.getElementById('uno-input');if(!input){return 'missing';}const event=new InputEvent('beforeinput',{inputType:'insertLineBreak',cancelable:true});input.dispatchEvent(event);return `${input.getAttribute('inputmode')}|${input.spellcheck}|${input.dataset.unoAcceptsReturn}|${event.defaultPrevented}`;})()")
				== "numeric|false|false|true");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Composition_Commit_And_Cancel_Follow_Browser_Events()
	{
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "AB");
			sut.Document.Selection.SetRange(1, 1);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){const input=document.getElementById('uno-input');input.dispatchEvent(new CompositionEvent('compositionstart'));input.value='AniB';input.setSelectionRange(2,2);input.dispatchEvent(new CompositionEvent('compositionupdate',{data:'ni'}));return 'ok';})()");
			await WindowHelper.WaitForIdle();
			GetTextWithoutFinalEop(sut.Document, out var text);
			Assert.AreEqual("AniB", text);
			Assert.AreEqual(2, sut.Document.Selection.StartPosition);
			Assert.AreEqual(
				"AniB|2",
				global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
					"(function(){const input=document.getElementById('uno-input');return `${input.value}|${input.selectionStart}`;})()"));

			global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){const input=document.getElementById('uno-input');input.value='AB';input.setSelectionRange(1,1);input.dispatchEvent(new CompositionEvent('compositionend',{data:''}));return 'ok';})()");
			await WindowHelper.WaitForIdle();
			GetTextWithoutFinalEop(sut.Document, out text);
			Assert.AreEqual("AB", text);

			sut.Document.Selection.SetRange(1, 1);
			await WindowHelper.WaitForIdle();
			global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){const input=document.getElementById('uno-input');input.dispatchEvent(new CompositionEvent('compositionstart'));input.value='AhaoB';input.setSelectionRange(3,3);input.dispatchEvent(new CompositionEvent('compositionupdate',{data:'hao'}));input.value='A好B';input.setSelectionRange(2,2);input.dispatchEvent(new CompositionEvent('compositionend',{data:'好'}));return 'ok';})()");
			await WindowHelper.WaitForIdle();
			GetTextWithoutFinalEop(sut.Document, out text);
			Assert.AreEqual("A好B", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_External_Edit_Invalidates_Stale_Composition_Callbacks()
	{
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "AB");
			sut.Document.Selection.SetRange(1, 1);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){const input=document.getElementById('uno-input');input.dispatchEvent(new CompositionEvent('compositionstart'));input.value='AniB';input.setSelectionRange(2,2);input.dispatchEvent(new CompositionEvent('compositionupdate',{data:'ni'}));return 'ok';})()");
			await WindowHelper.WaitForIdle();
			sut.Document.SetText(TextSetOptions.None, "XY");
			await WindowHelper.WaitForIdle();

			global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
				"(function(){const input=document.getElementById('uno-input');input.dispatchEvent(new CompositionEvent('compositionupdate',{data:'stale'}));input.dispatchEvent(new CompositionEvent('compositionend',{data:'stale'}));return 'ok';})()");
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(sut.Document, out var text);
			Assert.AreEqual("XY", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Focused_Wasm_RichEditBox_Is_Retemplated_Native_Input_Is_Transferred()
	{
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var template = sut.Template;
			Assert.IsNotNull(template);

			sut.Template = null;
			await WindowHelper.WaitForIdle();
			sut.Template = template;
			await WindowHelper.WaitForIdle();

			await WindowHelper.WaitFor(() =>
				global::Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs(
					"(function(){const inputs=document.querySelectorAll('#uno-input');return `${inputs.length}|${document.activeElement===inputs[0]}`;})()")
				== "1|true");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Ime_Restart_Fails_Next_Start_Retries()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			Assert.AreSame(sut, ImeSessionCoordinator.ActiveHost);
			fake.StartFailuresRemaining = 1;

			ImeSessionCoordinator.RestartSession(sut);

			Assert.IsNull(ImeSessionCoordinator.ActiveHost);
			ImeSessionCoordinator.StartSession(
				sut,
				new ImeSessionActivation(FocusState.Programmatic, IsSoftwareKeyboardSuppressed: false));
			Assert.AreSame(sut, ImeSessionCoordinator.ActiveHost);
			Assert.AreEqual(3, fake.StartImeSessionCallCount);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(sut);
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Android_InputConnection_Uses_RichEditBox_Document_Path()
	{
		var fakePlugin = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fakePlugin);
		var sut = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "abcd");
			sut.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			sut.Document.Selection.SetRange(1, 3);
			sut.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var connection = new FakeAndroidInputConnection((IImeSessionHost)sut, fakePlugin);
			connection.SetComposingText("xy", cursorPosition: 2);
			GetTextWithoutFinalEop(sut.Document, out var text);
			Assert.AreEqual("axyd", text);
			Assert.IsTrue(sut.IsComposing);
			Assert.AreEqual(3, sut.Document.Selection.StartPosition);

			connection.CommitText("XYZ");
			GetTextWithoutFinalEop(sut.Document, out text);
			Assert.AreEqual("aXYZd", text);
			Assert.IsFalse(sut.IsComposing);
			Assert.AreEqual(4, sut.Document.Selection.StartPosition);
			Assert.AreEqual(FormatEffect.On, sut.Document.GetRange(0, 1).CharacterFormat.Bold);

			connection.SetSelection(1, 2);
			Assert.AreEqual(1, sut.Document.Selection.StartPosition);
			Assert.AreEqual(3, sut.Document.Selection.EndPosition);
			connection.SetSelection(4, 0);
			connection.DeleteSurroundingText(beforeLength: 1, afterLength: 1);
			GetTextWithoutFinalEop(sut.Document, out text);
			Assert.AreEqual("aXY", text);
			Assert.AreEqual(3, sut.Document.Selection.StartPosition);

			fakePlugin.Updates.Clear();
			sut.InputScope = new InputScope
			{
				Names =
				{
					new InputScopeName { NameValue = InputScopeNameValue.Number },
				},
			};
			CollectionAssert.Contains(fakePlugin.Updates, ImeSessionUpdate.InputScope);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private sealed class FakeAndroidInputConnection
	{
		private readonly IImeSessionHost _host;
		private readonly FakeImeTextBoxExtension _plugin;
		private string _text;
		private int _selectionStart;
		private int _selectionLength;
		private int _compositionStart = -1;
		private int _compositionLength;

		internal FakeAndroidInputConnection(IImeSessionHost host, FakeImeTextBoxExtension plugin)
		{
			_host = host;
			_plugin = plugin;
			_text = host.Text;
			_selectionStart = host.SelectionStart;
			_selectionLength = host.SelectionLength;
		}

		internal void SetComposingText(string text, int cursorPosition)
		{
			var start = _compositionStart >= 0 ? _compositionStart : _selectionStart;
			var length = _compositionStart >= 0 ? _compositionLength : _selectionLength;
			_text = _text.Remove(start, length).Insert(start, text);
			_compositionStart = start;
			_compositionLength = text.Length;
			_selectionStart = start + Math.Clamp(cursorPosition, 0, text.Length);
			_selectionLength = 0;

			if (!_plugin.IsComposing)
			{
				_plugin.SimulateCompositionStart();
			}
			_plugin.SimulateCompositionUpdate(
				text,
				cursorPosition,
				textAlreadyApplied: true);
			_host.UpdateTextFromNative(_text, _selectionStart, _selectionLength);
		}

		internal void CommitText(string text)
		{
			var start = _compositionStart >= 0 ? _compositionStart : _selectionStart;
			var length = _compositionStart >= 0 ? _compositionLength : _selectionLength;
			_text = _text.Remove(start, length).Insert(start, text);
			_selectionStart = start + text.Length;
			_selectionLength = 0;

			_plugin.SimulateCompositionComplete(text, textAlreadyApplied: true);
			_compositionStart = -1;
			_compositionLength = 0;
			_host.UpdateTextFromNative(_text, _selectionStart, _selectionLength);
		}

		internal void SetSelection(int selectionStart, int selectionLength)
		{
			_selectionStart = selectionStart;
			_selectionLength = selectionLength;
			_host.SelectFromNative(selectionStart, selectionLength);
		}

		internal void DeleteSurroundingText(int beforeLength, int afterLength)
		{
			var start = Math.Max(0, _selectionStart - beforeLength);
			var end = Math.Min(_text.Length, _selectionStart + _selectionLength + afterLength);
			_text = _text.Remove(start, end - start);
			_selectionStart = start;
			_selectionLength = 0;
			_host.UpdateTextFromNative(_text, _selectionStart, _selectionLength);
		}
	}
}
