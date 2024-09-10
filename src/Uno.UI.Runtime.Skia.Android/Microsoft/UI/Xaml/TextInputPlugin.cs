using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class TextInputPlugin
{
	private readonly UnoSKCanvasView _view;
	private readonly InputMethodManager? _imm;
	private readonly AutofillManager? _afm;
	private InputTypes _inputTypes = InputTypes.TextVariationNormal;

	internal TextInputPlugin(UnoSKCanvasView view)
	{
		_view = view;
		_imm = (InputMethodManager?)view.Context!.GetSystemService(Context.InputMethodService);
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			_afm = (AutofillManager?)view.Context.GetSystemService(Java.Lang.Class.FromType(typeof(AutofillManager)));
		}
	}

	internal void NotifyViewEntered(TextBox textBox, int virtualId)
	{
		if (_afm == null)
		{
			return;
		}

		var physicalRect = GetPhysicalRect(textBox);
		_afm.NotifyViewEntered(_view, virtualId, new((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
	}

	private Windows.Foundation.Rect GetPhysicalRect(TextBox textBox)
	{
		int[] offset = new int[2];
		_view.GetLocationOnScreen(offset);
		var transform = UIElement.GetTransform(from: textBox, to: null);
		var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(textBox.Visual.Size.X, textBox.Visual.Size.Y)));
		var physicalRect = logicalRect.LogicalToPhysicalPixels();
		return physicalRect.OffsetRect(offset[0], offset[1]);
	}

	internal void NotifyViewExited(int virtualId)
	{
		if (_afm == null)
		{
			return;
		}

		_afm.NotifyViewExited(_view, virtualId);
	}

	internal void FinishAutofillContext(bool shouldSave)
	{
		if (_afm is null)
		{
			return;
		}

		if (shouldSave)
		{
			_afm.Commit();
		}
		else
		{
			_afm.Cancel();
		}
	}

	internal void NotifyValueChanged(int virtualId, string newValue)
	{
		if (_afm == null /*|| !NeedsAutofill()*/)
		{
			return;
		}

		_afm.NotifyValueChanged(_view, virtualId, AutofillValue.ForText(newValue));
	}

	internal void ShowTextInput(InputTypes inputTypes)
	{
		_inputTypes = inputTypes;
		_view.RequestFocus();
		_imm?.ShowSoftInput(_view, 0);
	}

	internal void HideTextInput()
	{
		_imm?.HideSoftInputFromWindow(_view.ApplicationWindowToken, 0);
	}

	internal void OnProvideAutofillVirtualStructure(ViewStructure? structure)
	{
#if false // Removing temporarily. We'll need to add it back.
		var textBoxes = AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.LiveTextBoxes;
		var index = structure!.AddChildCount(textBoxes.Count);
		var parentId = structure.AutofillId!;
		for (int i = 0; i < textBoxes.Count; i++)
		{
			var textBox = textBoxes[i];
			var child = structure.NewChild(index + i);
			child!.SetAutofillId(parentId, textBox.GetHashCode());
			child.SetDataIsSensitive(textBox is PasswordBox);

			// This is not really correct implementation.
			// We cannot determine ourselves the autofill hints.
			// Consider exposing a public API for the user to be able to specify this.
			child.SetAutofillHints([textBox is PasswordBox ? "password" : "username"]);
			child.SetAutofillType(AutofillType.Text);
			child.SetVisibility(ViewStates.Visible);
			// Do we need child.Hint? How to set it?

			var physicalRect = GetPhysicalRect(textBox);
			child.SetDimens((int)physicalRect.Left, (int)physicalRect.Top, 0, 0, (int)physicalRect.Width, (int)physicalRect.Height);
		}
#endif
	}

	internal void OnCreateInputConnection(EditorInfo editorInfo)
	{
		editorInfo.InputType = _inputTypes;
	}
}
