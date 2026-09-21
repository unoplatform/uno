#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ImeSessionCoordinator_RootOwnership
{
	[TestMethod]
	[DataRow("started")]
	[DataRow("updated")]
	[DataRow("completed")]
	[DataRow("partially-committed")]
	[DataRow("canceled")]
	[DataRow("ended")]
	[DataRow("candidate-bounds")]
	public void When_Scoped_Callbacks_Stay_With_Their_XamlRoot(string callback)
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var first = new FakeHost();
		var second = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(first);
			ImeSessionCoordinator.StartSession(second);
			var firstExtension = GetExtension(first);
			var secondExtension = GetExtension(second);
			Assert.AreNotSame(first.XamlRoot, second.XamlRoot);
			Assert.AreNotSame(firstExtension, secondExtension);
			Assert.AreEqual(0, firstExtension.EndCount);

			firstExtension.Emit(callback);
			CollectionAssert.AreEqual(new[] { callback }, first.Callbacks);
			Assert.AreEqual(0, second.Callbacks.Count);

			secondExtension.Emit(callback);
			CollectionAssert.AreEqual(new[] { callback }, first.Callbacks);
			CollectionAssert.AreEqual(new[] { callback }, second.Callbacks);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(first);
			ImeSessionCoordinator.EndSession(second);
		}
	}

	[TestMethod]
	public void When_One_Root_Ends_Late_Callbacks_Do_Not_Reach_Another_Root()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var first = new FakeHost();
		var second = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(first);
			ImeSessionCoordinator.StartSession(second);
			var firstExtension = GetExtension(first);
			var secondExtension = GetExtension(second);
			var queuedUpdate = firstExtension.CaptureUpdate();

			ImeSessionCoordinator.EndSession(first);
			queuedUpdate();
			firstExtension.Emit("candidate-bounds");

			Assert.IsNull(ImeSessionCoordinator.GetExtension(first));
			Assert.AreSame(secondExtension, ImeSessionCoordinator.GetExtension(second));
			Assert.AreEqual(1, firstExtension.EndCount);
			Assert.AreEqual(0, secondExtension.EndCount);
			Assert.AreEqual(0, first.Callbacks.Count);
			Assert.AreEqual(0, second.Callbacks.Count);

			secondExtension.Emit("updated");
			CollectionAssert.AreEqual(new[] { "updated" }, second.Callbacks);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(first);
			ImeSessionCoordinator.EndSession(second);
		}
	}

	[TestMethod]
	public void When_Host_Moves_Roots_Queued_Callbacks_Do_Not_Follow_It()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var host = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(host);
			var oldExtension = GetExtension(host);
			var queuedUpdate = oldExtension.CaptureUpdate();

			host.XamlRoot = CreateRoot();
			Assert.IsNull(ImeSessionCoordinator.GetExtension(host));
			queuedUpdate();
			Assert.AreEqual(0, host.Callbacks.Count);

			ImeSessionCoordinator.StartSession(host);
			var newExtension = GetExtension(host);
			Assert.AreNotSame(oldExtension, newExtension);
			Assert.AreEqual(1, oldExtension.EndCount);

			queuedUpdate();
			oldExtension.Emit("updated");
			Assert.AreEqual(0, host.Callbacks.Count);

			newExtension.Emit("updated");
			CollectionAssert.AreEqual(new[] { "updated" }, host.Callbacks);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(host);
		}
	}

	[TestMethod]
	public void When_Host_Refocuses_Queued_Callbacks_Do_Not_Enter_The_New_Session()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var host = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(host);
			var oldExtension = GetExtension(host);
			var queuedUpdate = oldExtension.CaptureUpdate();

			ImeSessionCoordinator.EndSession(host);
			ImeSessionCoordinator.StartSession(host);
			var newExtension = GetExtension(host);
			Assert.AreNotSame(oldExtension, newExtension);

			queuedUpdate();
			Assert.AreEqual(0, host.Callbacks.Count);
			newExtension.Emit("updated");
			CollectionAssert.AreEqual(new[] { "updated" }, host.Callbacks);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(host);
		}
	}

	[TestMethod]
	public void When_Host_Detaches_Ending_Still_Releases_Its_Original_Session()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var host = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(host);
			var extension = GetExtension(host);
			var queuedUpdate = extension.CaptureUpdate();
			host.XamlRoot = null;

			ImeSessionCoordinator.EndSession(host);
			queuedUpdate();

			Assert.AreEqual(1, extension.EndCount);
			Assert.IsNull(ImeSessionCoordinator.GetExtension(host));
			Assert.AreEqual(0, host.Callbacks.Count);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(host);
		}
	}

	[TestMethod]
	public async Task When_One_Root_Updates_Or_Restarts_The_Other_Session_Is_Unchanged()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var first = new FakeHost();
		var second = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(first);
			ImeSessionCoordinator.StartSession(second);
			var firstExtension = GetExtension(first);
			var secondExtension = GetExtension(second);
			var activation = new ImeSessionActivation(FocusState.Keyboard, IsSoftwareKeyboardSuppressed: true);

			ImeSessionCoordinator.StartSession(first, activation);
			ImeSessionCoordinator.UpdateSession(first, ImeSessionUpdate.TextAndSelection);
			ImeSessionCoordinator.RestartSession(first);

			Assert.AreEqual(3, firstExtension.StartCount);
			Assert.AreEqual(1, firstExtension.EndCount);
			Assert.AreEqual(activation, firstExtension.LastActivation);
			CollectionAssert.AreEqual(new[] { ImeSessionUpdate.TextAndSelection }, firstExtension.Updates);
			Assert.AreEqual(1, secondExtension.StartCount);
			Assert.AreEqual(0, secondExtension.EndCount);
			Assert.AreEqual(0, secondExtension.Updates.Count);

			firstExtension.Alternatives = new[] { "first" };
			secondExtension.Alternatives = new[] { "second" };
			Assert.AreSame(
				firstExtension.Alternatives,
				await ImeSessionCoordinator.GetLinguisticAlternativesAsync(first, "a", CancellationToken.None));
			Assert.AreSame(
				secondExtension.Alternatives,
				await ImeSessionCoordinator.GetLinguisticAlternativesAsync(second, "b", CancellationToken.None));
		}
		finally
		{
			ImeSessionCoordinator.EndSession(first);
			ImeSessionCoordinator.EndSession(second);
		}
	}

	[TestMethod]
	public void When_One_Root_Fails_To_Restart_Only_Its_Owner_Is_Released()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var first = new FakeHost();
		var second = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(first);
			ImeSessionCoordinator.StartSession(second);
			var firstExtension = GetExtension(first);
			var secondExtension = GetExtension(second);
			firstExtension.ThrowOnStart = true;

			ImeSessionCoordinator.RestartSession(first);

			Assert.IsNull(ImeSessionCoordinator.GetExtension(first));
			Assert.AreSame(secondExtension, ImeSessionCoordinator.GetExtension(second));
			Assert.AreEqual(0, secondExtension.EndCount);
			firstExtension.Emit("updated");
			secondExtension.Emit("updated");
			Assert.AreEqual(0, first.Callbacks.Count);
			CollectionAssert.AreEqual(new[] { "updated" }, second.Callbacks);

			firstExtension.ThrowOnStart = false;
			ImeSessionCoordinator.StartSession(first);
			Assert.AreSame(firstExtension, ImeSessionCoordinator.GetExtension(first));
		}
		finally
		{
			ImeSessionCoordinator.EndSession(first);
			ImeSessionCoordinator.EndSession(second);
		}
	}

	[TestMethod]
	public void When_Shared_Extension_Is_Installed_It_Retains_Single_Host_Routing()
	{
		var extension = new FakeExtension();
		using var scope = ImeSessionCoordinator.SetExtensionForTesting(extension);
		var first = new FakeHost();
		var second = new FakeHost();
		try
		{
			ImeSessionCoordinator.StartSession(first);
			var endsBeforeHandoff = extension.EndCount;
			ImeSessionCoordinator.StartSession(second);
			Assert.AreEqual(endsBeforeHandoff + 1, extension.EndCount);
			Assert.AreSame(second, ImeSessionCoordinator.ActiveHost);
			Assert.IsNull(ImeSessionCoordinator.GetExtension(first));
			Assert.AreSame(extension, ImeSessionCoordinator.GetExtension(second));

			ImeSessionCoordinator.EndSession(first);
			extension.Emit("updated");
			Assert.AreEqual(endsBeforeHandoff + 1, extension.EndCount);
			Assert.AreEqual(0, first.Callbacks.Count);
			CollectionAssert.AreEqual(new[] { "updated" }, second.Callbacks);
		}
		finally
		{
			ImeSessionCoordinator.EndSession(first);
			ImeSessionCoordinator.EndSession(second);
		}
	}

	[TestMethod]
	public async Task When_Scoped_Sessions_Follow_TextBox_RichEditBox_And_PasswordBox_Focus()
	{
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => new FakeExtension());
		var textBox = new TextBox { Text = "plain", Width = 200 };
		var richEditBox = new RichEditBox { Width = 200 };
		var passwordBox = new PasswordBox { Password = "password", Width = 200 };
		var textHost = ((ITextBoxHost)textBox).Core;
		var passwordHost = ((ITextBoxHost)passwordBox).Core;
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { textBox, richEditBox, passwordBox } });
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var textExtension = GetExtension(textHost);
			textExtension.Emit("started");
			Assert.IsTrue(textHost.IsComposing);

			Assert.IsTrue(richEditBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var richExtension = GetExtension(richEditBox);
			Assert.AreNotSame(textExtension, richExtension);
			Assert.IsFalse(textHost.IsComposing);
			textExtension.Emit("updated");
			Assert.AreEqual("plain", textBox.Text);

			richExtension.Emit("started");
			richExtension.Emit("updated");
			Assert.IsTrue(richEditBox.IsComposing);
			richEditBox.Document.GetText(TextGetOptions.None, out var composedText);

			Assert.IsTrue(passwordBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(richEditBox.IsComposing);
			Assert.IsNull(ImeSessionCoordinator.GetExtension(passwordHost));
			richExtension.Emit("updated");
			richEditBox.Document.GetText(TextGetOptions.None, out var finalText);
			Assert.AreEqual(composedText, finalText);
			Assert.AreEqual("password", passwordBox.Password);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	private static FakeExtension GetExtension(IImeSessionHost host)
	{
		var extension = ImeSessionCoordinator.GetExtension(host) as FakeExtension;
		Assert.IsNotNull(extension);
		return extension;
	}

	// These routing tests need root identity, not a native window or visual tree.
	private static XamlRoot CreateRoot() => new(null!);

	private sealed class FakeHost : IImeSessionHost
	{
		public XamlRoot? XamlRoot { get; set; } = CreateRoot();
		public TextBoxView? TextBoxView => null;
		public int SelectionStart => 0;
		public int SelectionLength => 0;
		public bool IsBackwardSelection => false;
		public InputScope InputScope { get; } = new();
		public bool IsTextPredictionEnabled => true;
		public CandidateWindowAlignment DesiredCandidateWindowAlignment => CandidateWindowAlignment.Default;
		public string Text => string.Empty;
		public bool AcceptsReturn => true;
		public bool IsSpellCheckEnabled => true;
		public bool CanAcceptTextInput => true;
		public int MaxLength => 0;
		public bool IsComposing => false;
		public CharacterCasing CharacterCasing => CharacterCasing.Normal;
		public List<string> Callbacks { get; } = new();

		public void UpdateTextFromNative(string text, int selectionStart, int selectionLength)
		{
		}

		public void SelectFromNative(int selectionStart, int selectionLength)
		{
		}

		public bool RaisePaste() => false;
		public void OnImeCompositionStarted() => Callbacks.Add("started");
		public void OnImeCompositionUpdated(string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied) => Callbacks.Add("updated");
		public void OnImeCompositionCompleted(string committedText, bool textAlreadyApplied) => Callbacks.Add("completed");
		public void OnImeCompositionPartiallyCommitted(string committedText, string compositionText, int cursorPosition, int resolvedLength, bool textAlreadyApplied) => Callbacks.Add("partially-committed");
		public void OnImeCompositionCanceled(bool textAlreadyApplied) => Callbacks.Add("canceled");
		public void OnImeCompositionEnded() => Callbacks.Add("ended");
		public void OnCandidateWindowBoundsChanged(Rect bounds) => Callbacks.Add("candidate-bounds");
	}

	private sealed class FakeExtension : IHostScopedImeTextBoxExtension
	{
		public bool IsComposing { get; private set; }
		public int StartCount { get; private set; }
		public int EndCount { get; private set; }
		public bool ThrowOnStart { get; set; }
		public ImeSessionActivation LastActivation { get; private set; }
		public List<ImeSessionUpdate> Updates { get; } = new();
		public IReadOnlyList<string> Alternatives { get; set; } = Array.Empty<string>();

		public event EventHandler? CompositionStarted;
		public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
		public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
		public event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted;
		public event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;
		public event EventHandler? CompositionEnded;
		public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged;

		public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
		{
			StartCount++;
			if (ThrowOnStart)
			{
				throw new InvalidOperationException("Injected session failure.");
			}
			LastActivation = activation;
		}

		public void EndImeSession()
		{
			EndCount++;
			if (IsComposing)
			{
				Emit("ended");
			}
		}

		public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update) => Updates.Add(update);

		public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
			=> Task.FromResult(Alternatives);

		public Action CaptureUpdate()
		{
			var handler = CompositionUpdated;
			return () => handler?.Invoke(this, new ImeCompositionEventArgs("queued"));
		}

		public void Emit(string callback)
		{
			switch (callback)
			{
				case "started":
					IsComposing = true;
					CompositionStarted?.Invoke(this, EventArgs.Empty);
					break;
				case "updated":
					CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs("text", cursorPosition: 4));
					break;
				case "completed":
					CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs("text"));
					break;
				case "partially-committed":
					CompositionPartiallyCommitted?.Invoke(this, new ImePartialCompositionEventArgs("te", "xt", cursorPosition: 2, resolvedLength: 0));
					break;
				case "canceled":
					CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
					break;
				case "ended":
					IsComposing = false;
					CompositionEnded?.Invoke(this, EventArgs.Empty);
					break;
				case "candidate-bounds":
					CandidateWindowBoundsChanged?.Invoke(this, new ImeCandidateWindowBoundsChangedEventArgs(new Rect(1, 2, 3, 4)));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(callback));
			}
		}
	}
}
