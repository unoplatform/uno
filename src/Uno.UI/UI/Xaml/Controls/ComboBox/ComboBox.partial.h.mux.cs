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


	private readonly SerialDisposable m_spEditableTextPreviewKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextTextChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextCandidateWindowBoundsChangedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextSizeChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextPointerPressedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextTappedEventHandler = new();
}
