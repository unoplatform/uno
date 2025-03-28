using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;
using Uno.Extensions;
using Uno.UI.DataBinding;
using static System.Math;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private bool _wasLastEditModified;


		private class Factory : EditableFactory
		{
			private readonly ManagedWeakReference _owner;

			public Factory(ManagedWeakReference owner)
			{
				_owner = owner;
			}
			public override IEditable NewEditable(Java.Lang.ICharSequence source)
			{
				return new TextBoxStringBuilder(_owner, source);
			}
		}

		/// <summary>
		/// Subclass of <see cref="SpannableStringBuilder"/> which allows input text to be modified by <see cref="TextBox"/>'s event handlers. 
		/// </summary>
		/// <remarks>
		/// Overriding the <see cref="Replace(int, int, ICharSequence, int, int)"/> method is more powerful than applying an <see cref="IInputFilter"/>
		/// to the native text field, because we can make arbitrary modifications to the entire content string, whereas <see cref="IInputFilter"/> only
		/// allows modifications to the new text being added to the buffer.
		/// </remarks>
		private class TextBoxStringBuilder : SpannableStringBuilder
		{
			private readonly ManagedWeakReference _owner;
			private TextBox Owner => _owner.Target as TextBox;

			public TextBoxStringBuilder(ManagedWeakReference owner, ICharSequence text) : base(text)
			{
				_owner = owner;
			}
			public override IEditable Replace(int start, int end, ICharSequence tb, int tbstart, int tbend)
			{
				if (Owner is { IsNativeViewReadOnly: true })
				{
					return this;
				}

				// Create a copy of this string builder to preview the change, allowing the TextBox's event handlers to act on the modified text.
				var copy = new SpannableStringBuilder(this);
				copy.Replace(start, end, tb, tbstart, tbend);
				var previewText = copy.ToString();

				// Modifying the text beyond max length will not be accepted. We discard this replace in advance to prevent
				// raising the chain of text-related events: BeforeTextChanging, TextChanging, and TextChanged.
				if (Owner.MaxLength > 0 && previewText.Length > Owner.MaxLength)
				{
					return this;
				}

				var finalText = Owner.ProcessTextInput(previewText);

				if (Owner._wasLastEditModified = previewText != finalText)
				{
					// Text was modified. Use new text as the replacement string, re-applying spans to ensure EditText's and keyboard's internals aren't disrupted.
					ICharSequence replacement;
					if (tb is ISpanned spanned)
					{
						var spannable = new SpannableString(finalText);
						TextUtils.CopySpansFrom(spanned, tbstart, Min(tbend, spannable.Length()), null, spannable, 0);
						replacement = spannable;
					}
					else
					{
						replacement = new Java.Lang.String(finalText);
					}

					base.Replace(0, Length(), replacement, 0, finalText.Length);
				}
				else
				{
					// Text was not modified, use original replacement ICharSequence
					base.Replace(start, end, tb, tbstart, tbend);
				}

				return this;
			}
		}

		/// <summary>
		/// Intercepts communication between the software keyboard and <see cref="TextBoxView"/>, in order to prevent incorrect changes.
		/// </summary>
		/// <remarks>
		/// Certain keyboards get confused when the input string is changed by TextChanging, particularly in the case of predictive text. We
		/// override the <see cref="TextBoxView.OnCreateInputConnection(EditorInfo)"/> method to intercept erroneous changes.
		///
		/// Most overrides are delegated to the EditableInputConnection object returned by the base EditText.OnCreateInputConnection() method (which can't
		/// be directly subclassed because it's internal).
		/// </remarks>
		internal class TextBoxInputConnection : BaseInputConnection
		{
			private (ICharSequence Text, int CursorPosition) _lastComposing;
			private readonly TextBoxView _textBoxView;
			private readonly BaseInputConnection _editableInputConnection;

			public TextBoxInputConnection(TextBoxView targetView, IInputConnection editableInputConnection) : base(targetView, fullEditor: true)
			{
				_textBoxView = targetView;
				_editableInputConnection = editableInputConnection as BaseInputConnection;
			}

			#region Redirects
			public override IEditable Editable => _editableInputConnection?.Editable ?? _textBoxView.EditableText;

			public override bool BeginBatchEdit() => _editableInputConnection?.BeginBatchEdit() ?? false;

			public override bool EndBatchEdit() => _editableInputConnection?.EndBatchEdit() ?? false;

			public override void CloseConnection()
			{
				// EditableInputConnection calls super.CloseConnection() Not obvious if we should call base here, or if it's not necessary (or would be harmful).
				_editableInputConnection?.CloseConnection();
			}

			public override bool ClearMetaKeyStates(MetaKeyStates states) => _editableInputConnection?.ClearMetaKeyStates(states) ?? false;

			public override bool CommitCompletion(CompletionInfo text) => _editableInputConnection?.CommitCompletion(text) ?? false;

			public override bool CommitCorrection(CorrectionInfo correctionInfo) => _editableInputConnection?.CommitCorrection(correctionInfo) ?? false;

			public override bool PerformEditorAction(ImeAction actionCode) => _editableInputConnection?.PerformEditorAction(actionCode) ?? false;

			public override bool PerformContextMenuAction(int id) => _editableInputConnection?.PerformContextMenuAction(id) ?? false;

			public override ExtractedText GetExtractedText(ExtractedTextRequest request, GetTextFlags flags) => _editableInputConnection?.GetExtractedText(request, flags);

			public override bool PerformPrivateCommand(string action, Bundle data) => _editableInputConnection?.PerformPrivateCommand(action, data) ?? false;

			public override bool RequestCursorUpdates(int cursorUpdateMode) => _editableInputConnection?.RequestCursorUpdates(cursorUpdateMode) ?? false;
			#endregion

			public override bool CommitText(ICharSequence text, int newCursorPosition)
			{
				if (!text.ToString().IsNullOrWhiteSpace()
					&& (_textBoxView.Owner?._wasLastEditModified ?? false)
					&& newCursorPosition == _lastComposing.CursorPosition
					&& text?.ToString() == _lastComposing.Text?.ToString())
				{
					// On many keyboards this is called after SetComposingText() if the new text came from a predictive option and the TextBox
					// modified the text. Generally it results in spurious modifications to the text, so we reject it in this case.

					// Note: this method seems to be *always* called for the space bar, vs SetComposingText for letter keys
					return false;
				}

				var output = _editableInputConnection?.CommitText(text, newCursorPosition);
				return output ?? false;
			}

			public override bool SetComposingText(ICharSequence text, int newCursorPosition)
			{
				_lastComposing = (text, newCursorPosition);
				var output = base.SetComposingText(text, newCursorPosition);
				return output;
			}
		}
	}
}
