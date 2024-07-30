using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	private bool m_isEditModeConfigured;

	// Setting Editable Mode configures several event listeners, we use this variable to prevent configuring Editable mode twice.
	// Editable ComboBox is designed to set the focus on TextBox when ComboBox is focused, there are some cases when we don't want
	// this behavior eg(Shift+Tab).
	private bool m_shouldMoveFocusToTextBox;

	private readonly SerialDisposable m_spEditableTextPreviewKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextTextChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextCandidateWindowBoundsChangedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextSizeChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextPointerPressedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextTappedEventHandler = new();
}
