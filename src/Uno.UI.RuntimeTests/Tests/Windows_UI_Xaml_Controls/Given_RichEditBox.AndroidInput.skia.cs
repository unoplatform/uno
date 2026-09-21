#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AndroidInput_Composition_Commits_Before_Completion(bool nestedBatch)
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			string? textAtStart = null;
			string? textAtEnd = null;
			var endedCount = 0;
			editor.TextCompositionStarted += (_, _) => textAtStart = ((IImeSessionHost)editor).Text;
			editor.TextCompositionEnded += (_, _) =>
			{
				textAtEnd = ((IImeSessionHost)editor).Text;
				endedCount++;
			};

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ni"));
			Assert.AreEqual("AB", textAtStart, "Start must open the undo group before applying preedit.");
			AssertAndroidInputText(editor, connection, "AniB");
			Assert.IsTrue(editor.IsComposing);
			if (nestedBatch)
			{
				Assert.AreEqual(true, InvokeAndroidMethod(connection, "BeginBatchEdit"));
			}
			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "\u4f60"));
			if (nestedBatch)
			{
				Assert.AreEqual("AniB", ((IImeSessionHost)editor).Text);
				Assert.AreEqual(0, endedCount);
				InvokeAndroidMethod(connection, "EndBatchEdit");
			}

			AssertAndroidInputText(editor, connection, "A\u4f60B");
			Assert.AreEqual("A\u4f60B", textAtEnd);
			Assert.AreEqual(1, endedCount);
			Assert.IsFalse(editor.IsComposing);
			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			AssertAndroidInputText(editor, connection, "AB");
			Assert.IsFalse(editor.Document.CanUndo(), "The preedit must not create a separate undo entry.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Composition_Cancellation_Is_Applied_Before_Completion()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			string? textAtEnd = null;
			editor.TextCompositionEnded += (_, _) => textAtEnd = ((IImeSessionHost)editor).Text;

			ApplyAndroidInputText(connection, "SetComposingText", "ni");
			ApplyAndroidInputText(connection, "CommitText", string.Empty);

			Assert.AreEqual("AB", textAtEnd);
			AssertAndroidInputText(editor, connection, "AB");
			Assert.IsFalse(editor.IsComposing);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_ReadOnly_TextBox_Rejects_Mutation_But_Allows_Selection()
	{
		var editor = new TextBox { Text = "fixed", IsReadOnly = true, Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var host = ((ITextBoxHost)editor).Core;
			var connection = CreateReadOnlyAndroidInputConnection(host);
			var plugin = ReadAndroidProperty(GetAndroidRenderView(), "TextInputPlugin");
			var inputTypes = plugin.GetType().GetField("_inputTypes", AndroidInstanceFlags)!.GetValue(plugin);
			Assert.AreEqual(0, Convert.ToInt32(inputTypes), "A read-only session must advertise InputTypes.Null.");

			Assert.AreEqual(true, InvokeAndroidMethod(connection, "SetSelection", 0, 5));
			Assert.AreEqual("fixed", editor.SelectedText);
			var selectedTextMethod = connection.GetType().GetMethod("GetSelectedTextFormatted")!;
			var flags = Enum.ToObject(selectedTextMethod.GetParameters()[0].ParameterType, 0);
			using var nativeSelection = (IDisposable)selectedTextMethod.Invoke(connection, [flags])!;
			Assert.AreEqual("fixed", nativeSelection.ToString());
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "x"));
			Assert.IsFalse(ApplyAndroidInputText(connection, "SetComposingText", "x"));
			Assert.AreEqual(false, InvokeAndroidMethod(connection, "DeleteSurroundingText", 1, 1));
			Assert.AreEqual(false, InvokeAndroidMethod(connection, "DeleteSurroundingTextInCodePoints", 1, 1));
			Assert.AreEqual("fixed", editor.Text);
			Assert.AreEqual("fixed", ReadAndroidProperty(connection, "Editable").ToString());

			editor.Text = "programmatic";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("programmatic", ReadAndroidProperty(connection, "Editable").ToString());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AndroidInput_RichEditBox_Becomes_Noneditable_During_Composition(bool readOnly)
	{
		var editor = new RichEditBox { Width = 240, AllowFocusWhenDisabled = true };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			ApplyAndroidInputText(connection, "SetComposingText", "ni");
			AssertAndroidInputText(editor, connection, "ni");
			if (readOnly)
			{
				editor.IsReadOnly = true;
			}
			else
			{
				editor.IsEnabled = false;
			}

			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "\u4f60"));
			Assert.IsFalse(ApplyAndroidInputText(connection, "SetComposingText", "stale"));
			Assert.AreEqual("ni", ((IImeSessionHost)editor).Text);
			Assert.IsFalse(editor.IsComposing);
			if (readOnly)
			{
				editor.IsReadOnly = false;
			}
			editor.Document.GetRange(0, 2).Text = "TOM";
			Assert.AreEqual("TOM", ((IImeSessionHost)editor).Text);

			editor.IsEnabled = true;
			editor.Document.Selection.SetRange(3, 3);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var replacement = await WaitForAndroidInputConnection(editor, connection);
			Assert.IsTrue(ApplyAndroidInputText(replacement, "CommitText", "x"));
			AssertAndroidInputText(editor, replacement, "TOMx");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_ReadOnly_Rejects_Writes_Until_Reenabled()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "fixed");
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			editor.IsReadOnly = true;
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "native"));
			Assert.AreEqual("fixed", ((IImeSessionHost)editor).Text);

			Assert.ThrowsExactly<UnauthorizedAccessException>(
				() => editor.Document.SetText(TextSetOptions.None, "programmatic"));
			Assert.AreEqual("fixed", ((IImeSessionHost)editor).Text);

			editor.IsReadOnly = false;
			editor.Document.SetText(TextSetOptions.None, "programmatic");

			Assert.AreEqual("programmatic", ((IImeSessionHost)editor).Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AndroidInput_TextBox_Becomes_Noneditable_During_Composition(bool readOnly)
	{
		var editor = new TextBox { Width = 240, AllowFocusWhenDisabled = true };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var host = ((ITextBoxHost)editor).Core;
			var connection = await WaitForAndroidInputConnection(host);
			ApplyAndroidInputText(connection, "SetComposingText", "ni");
			Assert.AreEqual("ni", editor.Text);

			if (readOnly)
			{
				editor.IsReadOnly = true;
			}
			else
			{
				editor.IsEnabled = false;
			}
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "\u4f60"));
			Assert.AreEqual("ni", editor.Text);
			Assert.IsFalse(((IImeSessionHost)host).IsComposing);
			editor.Text = "managed";
			Assert.AreEqual("managed", editor.Text);
			editor.IsReadOnly = false;
			editor.IsEnabled = true;
			editor.Select(editor.Text.Length, 0);
			editor.Focus(FocusState.Programmatic);
			var replacement = await WaitForAndroidInputConnection(host, connection);
			Assert.AreEqual("managed", ReadAndroidProperty(replacement, "Editable").ToString());
			Assert.IsTrue(ApplyAndroidInputText(replacement, "CommitText", "x"));
			Assert.AreEqual("managedx", editor.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_TextBox_Completes_With_Committed_Text()
	{
		var editor = new TextBox { Width = 240, Text = "AB" };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(1, 0);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(((ITextBoxHost)editor).Core);
			string? textAtEnd = null;
			editor.TextCompositionEnded += (_, _) => textAtEnd = editor.Text;

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ni"));
			Assert.AreEqual("AniB", editor.Text);
			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "\u4f60"));

			Assert.AreEqual("A\u4f60B", textAtEnd);
			Assert.AreEqual("A\u4f60B", editor.Text);
			Assert.AreEqual(editor.Text, ReadAndroidProperty(connection, "Editable").ToString());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AndroidInput_Composition_Handler_Changes_Host(bool onCompletion)
	{
		var editor = new RichEditBox { Width = 240 };
		var next = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { editor, next } });
			editor.Document.SetText(TextSetOptions.None, "A");
			editor.Document.Selection.SetRange(1, 1);
			next.Document.SetText(TextSetOptions.None, "B");
			next.Document.Selection.SetRange(1, 1);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			bool? focusSucceeded = null;
			var nextOwnsLogicalFocus = false;
			string? textAtFocusRequest = null;
			void TransferFocus()
			{
				textAtFocusRequest = ((IImeSessionHost)editor).Text;
				focusSucceeded = next.Focus(FocusState.Programmatic);
				nextOwnsLogicalFocus = next.XamlRoot is { } root && ReferenceEquals(next, FocusManager.GetFocusedElement(root));
			}
			if (onCompletion)
			{
				editor.TextCompositionEnded += (_, _) => TransferFocus();
			}
			else
			{
				editor.TextCompositionStarted += (_, _) => TransferFocus();
			}

			ApplyAndroidInputText(connection, "SetComposingText", "ni");
			if (onCompletion)
			{
				ApplyAndroidInputText(connection, "CommitText", "\u4f60");
			}
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(true, focusSucceeded, $"Composition callback must transfer focus; text at request was '{textAtFocusRequest}'.");
			Assert.IsTrue(nextOwnsLogicalFocus);
			Assert.AreEqual(onCompletion ? "A\u4f60" : "A", ((IImeSessionHost)editor).Text);
			var replacement = await WaitForAndroidInputConnection(next, connection);
			Assert.AreNotSame(connection, replacement);
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "stale"));
			Assert.IsTrue(ApplyAndroidInputText(replacement, "CommitText", "x"));
			AssertAndroidInputText(next, replacement, "Bx");
			Assert.IsFalse(next.IsComposing);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Options_Restart_Preserves_Composition_And_Retires_Old_Connection()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			ApplyAndroidInputText(connection, "SetComposingText", "ni");

			editor.InputScope = new InputScope
			{
				Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } },
			};
			var replacement = await WaitForAndroidInputConnection(editor, connection);
			Assert.AreNotSame(connection, replacement);
			Assert.IsTrue(editor.IsComposing);
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "stale"));
			Assert.IsTrue(ApplyAndroidInputText(replacement, "CommitText", "\u4f60"));
			AssertAndroidInputText(editor, replacement, "\u4f60");
			Assert.IsFalse(editor.IsComposing);
			editor.Document.Undo();
			AssertAndroidInputText(editor, replacement, string.Empty);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Completion_Handler_Changes_Document()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			string? textAtEnd = null;
			editor.TextCompositionEnded += (_, _) =>
			{
				textAtEnd = ((IImeSessionHost)editor).Text;
				editor.Document.SetText(TextSetOptions.None, "external");
			};

			ApplyAndroidInputText(connection, "SetComposingText", "ni");
			ApplyAndroidInputText(connection, "CommitText", "\u4f60");

			Assert.AreEqual("\u4f60", textAtEnd);
			AssertAndroidInputText(editor, connection, "external");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Paragraph_Edit_Preserves_Formatting()
	{
		var editor = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "a\rb");
			editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.Selection.SetRange(3, 3);
			editor.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var connection = await WaitForAndroidInputConnection(editor);
			AssertAndroidInputText(editor, connection, "a\rb");

			ApplyAndroidInputText(connection, "CommitText", "x");

			AssertAndroidInputText(editor, connection, "a\rbx");
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private const BindingFlags AndroidInstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static object GetAndroidRenderView()
	{
		var activityType = AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => assembly.GetType("Microsoft.UI.Xaml.ApplicationActivity"))
			.OfType<Type>()
			.Single();
		return activityType.GetProperty("RenderView", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null)
			?? throw new InvalidOperationException("Android RenderView was not initialized.");
	}

	private static async Task<object> WaitForAndroidInputConnection(IImeSessionHost host, object? previous = null)
	{
		var renderView = GetAndroidRenderView();
		var plugin = ReadAndroidProperty(renderView, "TextInputPlugin");
		var owner = host switch
		{
			Control control => control,
			TextBoxCore core => core.Owner,
			_ => throw new InvalidOperationException("The Android input host has no control owner."),
		};
		var initialState = DescribeAndroidInputConnectionState(host, owner, renderView, plugin, previous);
		object? connection = null;
		await WindowHelper.WaitFor(() =>
		{
			connection = plugin.GetType().GetProperty("ActiveInputConnection", AndroidInstanceFlags)?.GetValue(plugin);
			return connection is not null
				&& !ReferenceEquals(connection, previous)
				&& ReferenceEquals(host, connection.GetType().GetProperty("ActiveHost", AndroidInstanceFlags)?.GetValue(connection))
				&& ReferenceEquals(host, ImeSessionCoordinator.ActiveHost)
				&& ReferenceEquals(host, plugin.GetType().GetField("_activeHost", AndroidInstanceFlags)?.GetValue(plugin))
				&& host.CanAcceptTextInput
				&& owner.FocusState != FocusState.Unfocused
				&& host.XamlRoot is { } xamlRoot
				&& ReferenceEquals(owner, FocusManager.GetFocusedElement(xamlRoot))
				&& ReadAndroidProperty(renderView, "IsAttachedToWindow") is true
				&& ReadAndroidProperty(renderView, "HasFocus") is true
				&& ReadAndroidProperty(renderView, "HasWindowFocus") is true;
		},
			expected: true,
			messageBuilder: _ => "A naturally created, focused Android InputConnection was not ready."
				+ Environment.NewLine + "Initial: " + initialState
				+ Environment.NewLine + "Final: " + DescribeAndroidInputConnectionState(host, owner, renderView, plugin, previous),
			timeoutMS: 5000);
		return connection ?? throw new InvalidOperationException("Android did not create an input connection.");
	}

	private static string DescribeAndroidInputConnectionState(
		IImeSessionHost host,
		Control owner,
		object renderView,
		object plugin,
		object? previous)
	{
		var xamlRoot = host.XamlRoot;
		var focusedElement = xamlRoot is null ? null : FocusManager.GetFocusedElement(xamlRoot);
		var coordinatorHost = ImeSessionCoordinator.ActiveHost;
		var pluginHost = plugin.GetType().GetField("_activeHost", AndroidInstanceFlags)?.GetValue(plugin);
		var connection = ReadAndroidNullableProperty(plugin, "ActiveInputConnection");
		var connectionHost = ReadAndroidNullableProperty(connection, "ActiveHost");
		var imm = plugin.GetType().GetField("_imm", AndroidInstanceFlags)?.GetValue(plugin);
		var context = ReadAndroidNullableProperty(renderView, "Context");
		var resources = ReadAndroidNullableProperty(context, "Resources");
		var configuration = ReadAndroidNullableProperty(resources, "Configuration");
		var nativeRoot = ReadAndroidNullableProperty(renderView, "RootView");
		var windowLayout = ReadAndroidNullableProperty(nativeRoot, "LayoutParameters");
		var attached = ReadAndroidNullableProperty(renderView, "IsAttachedToWindow");
		var nativeFocus = ReadAndroidNullableProperty(renderView, "HasFocus");
		var windowFocus = ReadAndroidNullableProperty(renderView, "HasWindowFocus");
		var phase = attached is not true ? "native-view-detached"
			: windowFocus is not true ? "native-window-unfocused: check foreground activity/system dialogs"
			: nativeFocus is not true ? "native-render-view-unfocused"
			: owner.FocusState == FocusState.Unfocused ? "xaml-owner-unfocused"
			: !ReferenceEquals(owner, focusedElement) ? "xaml-focus-mismatch"
			: !host.CanAcceptTextInput ? "host-not-editable"
			: !ReferenceEquals(host, coordinatorHost) ? "coordinator-host-mismatch"
			: !ReferenceEquals(host, pluginHost) ? "plugin-host-mismatch"
			: connection is null ? "no-platform-created-connection"
			: ReferenceEquals(connection, previous) ? "connection-not-replaced"
			: !ReferenceEquals(host, connectionHost) ? "connection-host-mismatch"
			: "connection-ready";

		return string.Join(Environment.NewLine,
			$"{DateTimeOffset.Now:O}; phase={phase}",
			$"ExpectedHost={AndroidDiagnosticIdentity(host)}; Owner={AndroidDiagnosticIdentity(owner)}; CanAcceptTextInput={host.CanAcceptTextInput}; IsLoaded={owner.IsLoaded}; IsEnabled={owner.IsEnabled}; IsTabStop={owner.IsTabStop}",
			$"XamlRoot={AndroidDiagnosticIdentity(xamlRoot)}; XamlFocusedElement={AndroidDiagnosticIdentity(focusedElement)}; ExpectedOwnerFocused={ReferenceEquals(owner, focusedElement)}; FocusState={owner.FocusState}",
			$"CoordinatorHost={AndroidDiagnosticIdentity(coordinatorHost)}; ExpectedCoordinatorHost={ReferenceEquals(host, coordinatorHost)}",
			$"RenderView={AndroidDiagnosticIdentity(renderView)}; IsAttachedToWindow={attached}; HasFocus={nativeFocus}; HasWindowFocus={windowFocus}; WindowFlags={ReadAndroidNullableProperty(windowLayout, "Flags")}",
			$"PluginHost={AndroidDiagnosticIdentity(pluginHost)}; ExpectedPluginHost={ReferenceEquals(host, pluginHost)}; InputType={plugin.GetType().GetField("_inputTypes", AndroidInstanceFlags)?.GetValue(plugin)}",
			$"CurrentConnection={AndroidDiagnosticIdentity(connection)}; PreviousConnection={AndroidDiagnosticIdentity(previous)}; IsPrevious={ReferenceEquals(connection, previous)}; ConnectionHost={AndroidDiagnosticIdentity(connectionHost)}; ExpectedConnectionHost={ReferenceEquals(host, connectionHost)}",
			$"InputMethodManager={AndroidDiagnosticIdentity(imm)}; IsAcceptingText={ReadAndroidNullableProperty(imm, "IsAcceptingText")}",
			$"Keyboard={ReadAndroidNullableProperty(configuration, "Keyboard")}; KeyboardHidden={ReadAndroidNullableProperty(configuration, "KeyboardHidden")}; HardKeyboardHidden={ReadAndroidNullableProperty(configuration, "HardKeyboardHidden")}");
	}

	private static string AndroidDiagnosticIdentity(object? value)
		=> value is null ? "null" : $"{value.GetType().FullName}#{RuntimeHelpers.GetHashCode(value):x}";

	private static object? ReadAndroidNullableProperty(object? target, string name)
		=> target?.GetType().GetProperty(name, AndroidInstanceFlags)?.GetValue(target);

	private static object CreateReadOnlyAndroidInputConnection(IImeSessionHost host)
	{
		Assert.IsFalse(host.CanAcceptTextInput);
		// Read-only editors need not be served by the IME. This explicitly probes mutation/selection, not activation.
		var plugin = ReadAndroidProperty(GetAndroidRenderView(), "TextInputPlugin");
		var connection = InvokeAndroidMethod(plugin, "OnCreateInputConnection", [null])
			?? throw new InvalidOperationException("Android did not create an input connection.");
		Assert.AreSame(host, ReadAndroidProperty(connection, "ActiveHost"));
		return connection;
	}

	private static object ReadAndroidProperty(object target, string name)
		=> target.GetType().GetProperty(name, AndroidInstanceFlags)?.GetValue(target)
			?? throw new MissingMemberException(target.GetType().FullName, name);

	private static object? InvokeAndroidMethod(object target, string name, params object?[] arguments)
	{
		var method = target.GetType().GetMethods(AndroidInstanceFlags).Single(candidate =>
			candidate.Name == name
			&& candidate.GetParameters().Length == arguments.Length
			&& candidate.GetParameters().Select((parameter, index) =>
				arguments[index] is null || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(matches => matches));
		return method.Invoke(target, arguments);
	}

	private static bool ApplyAndroidInputText(object connection, string name, string text)
	{
		var method = connection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
			.Single(candidate => candidate.Name == name && candidate.GetParameters().Length == 2);
		var javaStringType = method.GetParameters()[0].ParameterType.Assembly.GetType("Java.Lang.String", throwOnError: true)!;
		using var value = (IDisposable)(Activator.CreateInstance(javaStringType, [text])
			?? throw new InvalidOperationException("Java string construction failed."));
		return (bool)method.Invoke(connection, [value, 1])!;
	}

	private static void AssertAndroidInputText(RichEditBox editor, object connection, string expected)
	{
		Assert.AreEqual(expected, ((IImeSessionHost)editor).Text);
		Assert.AreEqual(expected.Replace('\r', '\n'), ReadAndroidProperty(connection, "Editable").ToString());
	}
}
