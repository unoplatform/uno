#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
public sealed class Given_Wayland_ImeSession
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Injected_Batches_Reach_Editor(bool richEdit)
	{
		var (editor, host) = CreateEditor(richEdit, "aé😀z");
		try
		{
			await UITestHelper.Load(editor);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var extension = RequireWaylandExtension(host);
			var input = new PendingBatchInjector(extension);
			await WindowHelper.WaitFor(() => input.IsEnabled);
			host.SelectFromNative(2, 0);
			extension.UpdateImeSession(host, ImeSessionUpdate.TextAndSelection);

			input.Done(deleteBefore: 2, deleteAfter: 4, commit: "界", preedit: "に", cursor: 3);
			Assert.AreEqual("aé😀z", host.Text, "A done batch must wait for the UI dispatch.");
			Assert.AreEqual(1, input.PostedCount);
			input.Drain();
			Assert.AreEqual("a界にz", host.Text);
			Assert.AreEqual(3, host.SelectionStart);
			Assert.AreEqual(0, host.SelectionLength);
			Assert.IsTrue(host.IsComposing);

			input.Done(commit: "日本");
			input.Drain();
			Assert.AreEqual("a界日本z", host.Text, "Commit must replace the preedit rather than append to it.");
			Assert.AreEqual(4, host.SelectionStart);
			Assert.IsFalse(host.IsComposing);

			input.Done(preedit: "仮", cursor: 3);
			input.Drain();
			Assert.AreEqual("a界日本仮z", host.Text);
			Assert.IsTrue(host.IsComposing);
			input.Done(preedit: "置換", cursor: 6);
			input.Drain();
			Assert.AreEqual("a界日本置換z", host.Text, "A new preedit replaces the previous transient span.");
			Assert.AreEqual(6, host.SelectionStart);
			Assert.IsTrue(host.IsComposing);
			input.Done(preedit: string.Empty);
			input.Drain();
			Assert.AreEqual("a界日本z", host.Text, "An empty preedit must cancel only the transient text.");
			Assert.AreEqual(4, host.SelectionStart);
			Assert.IsFalse(host.IsComposing);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(host);
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Focus_Changes_Before_Injected_Batch_Is_Dispatched(bool richEdit)
	{
		var (first, firstHost) = CreateEditor(richEdit, "first");
		var (second, secondHost) = CreateEditor(!richEdit, "second");
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { first, second } });
			Assert.IsTrue(first.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var extension = RequireWaylandExtension(firstHost);
			var input = new PendingBatchInjector(extension);
			await WindowHelper.WaitFor(() => input.IsEnabled);
			input.Done(deleteBefore: 1, deleteAfter: 1, commit: "stale", preedit: "stale", cursor: 5);
			Assert.AreEqual("first", firstHost.Text);
			Assert.AreEqual(1, input.PostedCount);

			// Keep the injected batch queued until the focus change has settled.
			Assert.IsTrue(second.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			input.Drain();
			Assert.AreEqual("first", firstHost.Text);
			Assert.AreEqual("second", secondHost.Text);
			Assert.IsFalse(firstHost.IsComposing);
			Assert.IsFalse(secondHost.IsComposing);
			Assert.IsNull(ImeSessionCoordinator.GetExtension(firstHost));
			Assert.AreSame(extension, RequireWaylandExtension(secondHost));
			await WindowHelper.WaitFor(() => input.IsEnabled);

			secondHost.SelectFromNative(6, 0);
			extension.UpdateImeSession(secondHost, ImeSessionUpdate.TextAndSelection);
			input.Done(commit: "!");
			input.Drain();
			Assert.AreEqual("second!", secondHost.Text);
			Assert.AreEqual("first", firstHost.Text);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(firstHost);
			ImeSessionCoordinator.EndSession(secondHost);
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	private static (Control Editor, IImeSessionHost Host) CreateEditor(bool richEdit, string text)
	{
		if (richEdit)
		{
			var editor = new RichEditBox { Width = 240, Height = 80 };
			editor.Document.SetText(TextSetOptions.None, text);
			return (editor, (IImeSessionHost)editor);
		}
		var textBox = new TextBox { Width = 240, Text = text };
		return (textBox, ((ITextBoxHost)textBox).Core);
	}

	[TestMethod]
	public async Task When_Focus_Moves_Between_TextBox_And_RichEditBox()
	{
		var first = new TextBox { Text = "café 😀 日本語", Width = 240 };
		var second = new TextBox { Text = "second", Width = 240 };
		var rich = new RichEditBox { Width = 240, Height = 80 };
		rich.Document.SetText(TextSetOptions.None, "العربية 😀");
		var firstHost = ((ITextBoxHost)first).Core;
		var secondHost = ((ITextBoxHost)second).Core;
		var richHost = (IImeSessionHost)rich;
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { first, second, rich } });
			Assert.IsTrue(first.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			RequireWaylandExtension(firstHost);
			first.Select(5, 2);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, ((IImeSessionHost)firstHost).SelectionStart);
			Assert.AreEqual(2, ((IImeSessionHost)firstHost).SelectionLength);

			Assert.IsTrue(second.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsNull(ImeSessionCoordinator.GetExtension(firstHost));
			RequireWaylandExtension(secondHost);

			Assert.IsTrue(rich.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsNull(ImeSessionCoordinator.GetExtension(secondHost));
			RequireWaylandExtension(richHost);
			Assert.IsFalse(richHost.IsComposing);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
		Assert.IsNull(ImeSessionCoordinator.GetExtension(firstHost));
		Assert.IsNull(ImeSessionCoordinator.GetExtension(secondHost));
		Assert.IsNull(ImeSessionCoordinator.GetExtension(richHost));
	}

	private static IImeTextBoxExtension RequireWaylandExtension(IImeSessionHost host)
	{
		var extension = ImeSessionCoordinator.GetExtension(host);
		if (extension is null)
		{
			Assert.Inconclusive("The compositor must advertise text-input-v3 to exercise native Wayland IME sessions.");
			throw new InvalidOperationException();
		}
		Assert.AreEqual("Uno.UI.Runtime.Wayland.WaylandImeTextBoxExtension", extension.GetType().FullName);
		return extension;
	}

	// Test-only reflection drives the ApiExtensibility-resolved host without referencing its assembly.
	// String dependencies preserve the injected members in trimmed test heads; production needs no test API or IVT.
	private sealed class PendingBatchInjector
	{
		private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic;
		private readonly IImeTextBoxExtension _extension;
		private readonly object _stateLock;
		private readonly object _pending;
		private readonly FieldInfo _serial;
		private readonly FieldInfo _enabled;
		private readonly FieldInfo _postToUI;
		private readonly MethodInfo _done;
		private readonly ConstructorInfo _doneArgs;
		private readonly Queue<Action> _posted = new();

		[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.NonPublicMethods,
			"Uno.UI.Runtime.Wayland.WaylandImeTextBoxExtension", "Uno.UI.Runtime.Wayland")]
		[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicProperties,
			"Uno.UI.Runtime.Wayland.WaylandTextInputBatch", "Uno.UI.Runtime.Wayland")]
		[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors,
			"Uno.UI.Runtime.Wayland.Protocols.ZwpTextInputV3+DoneEventArgs", "Uno.UI.Runtime.Wayland")]
		[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "The Wayland runtime is present only in heads selected by SkiaWayland.")]
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "DynamicDependency preserves the reflected host members and done arguments in test heads.")]
		internal PendingBatchInjector(IImeTextBoxExtension extension)
		{
			_extension = extension;
			var type = extension.GetType();
			_stateLock = type.GetField("_stateLock", InstanceMembers)!.GetValue(extension)!;
			_pending = type.GetField("_pending", InstanceMembers)!.GetValue(extension)!;
			_serial = type.GetField("_commitSerial", InstanceMembers)!;
			_enabled = type.GetField("_enabled", InstanceMembers)!;
			_postToUI = type.GetField("_postToUI", InstanceMembers)!;
			_done = type.GetMethod("OnDone", InstanceMembers)!;
			_doneArgs = _done.GetParameters()[1].ParameterType.GetConstructor(InstanceMembers, null, [typeof(uint)], null)!;
		}

		internal bool IsEnabled => (bool)_enabled.GetValue(_extension)!;
		internal int PostedCount => _posted.Count;

		internal void Drain()
		{
			while (_posted.TryDequeue(out var action))
			{
				action();
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The injector constructor preserves the pending batch properties through DynamicDependency.")]
		internal void Done(uint deleteBefore = 0, uint deleteAfter = 0, string? commit = null, string? preedit = null, int cursor = 0)
		{
			lock (_stateLock)
			{
				var type = _pending.GetType();
				type.GetProperty("DeleteBefore", InstanceMembers)!.SetValue(_pending, deleteBefore);
				type.GetProperty("DeleteAfter", InstanceMembers)!.SetValue(_pending, deleteAfter);
				type.GetProperty("Commit", InstanceMembers)!.SetValue(_pending, commit);
				type.GetProperty("Preedit", InstanceMembers)!.SetValue(_pending, preedit);
				type.GetProperty("PreeditCursor", InstanceMembers)!.SetValue(_pending, cursor);
				var args = _doneArgs.Invoke([_serial.GetValue(_extension)!]);
				var postToUI = (Action<Action>)_postToUI.GetValue(_extension)!;
				var injectionThread = Environment.CurrentManagedThreadId;
				// Drain only the synthetic done, before a real compositor done can clear its preedit.
				Action<Action> post = action =>
				{
					if (Environment.CurrentManagedThreadId == injectionThread)
					{
						_posted.Enqueue(action);
					}
					else
					{
						postToUI(action);
					}
				};
				_postToUI.SetValue(_extension, post);
				try
				{
					_done.Invoke(_extension, [null, args]);
				}
				finally
				{
					_postToUI.SetValue(_extension, postToUI);
				}
			}
		}
	}
}
