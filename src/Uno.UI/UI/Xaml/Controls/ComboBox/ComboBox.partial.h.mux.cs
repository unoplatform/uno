using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	private bool m_isInSearchingMode;
	private bool m_ignoreCancelKeyDowns;
	private bool m_isEditModeConfigured;

	// Setting Editable Mode configures several event listeners, we use this variable to prevent configuring Editable mode twice.
	// Editable ComboBox is designed to set the focus on TextBox when ComboBox is focused, there are some cases when we don't want
	// this behavior eg(Shift+Tab).
	private bool m_shouldMoveFocusToTextBox;

	private bool m_restoreIndexSet;

	private readonly SerialDisposable m_spEditableTextPreviewKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextTextChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextCandidateWindowBoundsChangedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextSizeChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextPointerPressedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextTappedEventHandler = new();

	private TextBlock m_tpEditableContentPresenterTextBlock;

	private object m_customValueRef;

	private bool IsSmallFormFactor => false; // TODO Uno: This is currently not supported.

	private bool IsInline => IsSmallFormFactor && m_itemCount <= s_itemCountThreashold;

	private bool IsFullMode() => IsSmallFormFactor && m_itemCount > s_itemCountThreashold;

	private int m_itemCount;

	private int m_indexToRestoreOnCancel = -1;
}
