using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

#pragma warning disable CS0067 // Events declared for WinUI API compatibility
#pragma warning disable IDE0051 // Partial methods and template part constants used only on Skia

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a rich text editing control that supports formatted text, hyperlinks, and inline images.
	/// </summary>
	/// <remarks>
	/// MUX Reference: RichEditBox_Partial.h, RichEditBox_Partial.cpp
	/// Diverges from WinUI: WinUI's RichEditBox is built on the native Windows RichEdit control
	/// (CTextBoxBase → CRichEditBox in the native layer, using ITextServices2/ITextDocument2 COM interfaces).
	/// Uno implements the editing infrastructure directly in managed code on Skia,
	/// with the document model backed by RichEditTextDocument.
	/// Non-Skia platforms remain NotImplemented.
	/// </remarks>
	public partial class RichEditBox : Control
	{
		// MUX Reference: RichEditBox_Partial.h - Template part names
		private const string ContentElementPartName = "ContentElement";
		private const string PlaceholderTextPartName = "PlaceholderTextContentPresenter";
		private const string HeaderContentPartName = "HeaderContentPresenter";
		private const string DescriptionPresenterPartName = "DescriptionPresenter";
		private const string BorderElementPartName = "BorderElement";

		/// <summary>
		/// Occurs when the content of the text box changes.
		/// </summary>
		public event RoutedEventHandler TextChanged;

		/// <summary>
		/// Occurs when the text in the text box starts to change.
		/// </summary>
		public event TypedEventHandler<RichEditBox, RichEditBoxTextChangingEventArgs> TextChanging;

		/// <summary>
		/// Occurs when the text selection has changed.
		/// </summary>
		public event RoutedEventHandler SelectionChanged;

		/// <summary>
		/// Occurs when the text selection is about to change.
		/// </summary>
		public event TypedEventHandler<RichEditBox, RichEditBoxSelectionChangingEventArgs> SelectionChanging;

		/// <summary>
		/// Occurs when text is pasted into the control.
		/// </summary>
		public event TextControlPasteEventHandler Paste;

		/// <summary>
		/// Occurs when text is about to be copied to the clipboard.
		/// </summary>
		public event TypedEventHandler<RichEditBox, TextControlCopyingToClipboardEventArgs> CopyingToClipboard;

		/// <summary>
		/// Occurs when text is about to be cut to the clipboard.
		/// </summary>
		public event TypedEventHandler<RichEditBox, TextControlCuttingToClipboardEventArgs> CuttingToClipboard;

#if __SKIA__
		/// <summary>
		/// Occurs when a context menu is opening.
		/// </summary>
		public event ContextMenuOpeningEventHandler ContextMenuOpening;
#endif

		public RichEditBox()
		{
			DefaultStyleKey = typeof(RichEditBox);

#if __SKIA__
			InitializeSkia();
#endif
		}

		partial void InitializeSkia();

		// MUX Reference: RichEditBox_Partial.cpp - OnApplyTemplate
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

#if __SKIA__
			OnApplyTemplateSkia();
#endif

			UpdateHeaderVisibility();
			UpdatePlaceholderVisibility();
			UpdateDescriptionVisibility(true);
			UpdateVisualState();
		}

		partial void OnApplyTemplateSkia();

		/// <summary>
		/// Gets the document associated with this RichEditBox.
		/// </summary>
		/// <remarks>
		/// MUX Reference: RichEditBox_Partial.cpp - get_DocumentImpl
		/// Diverges from WinUI: WinUI returns a COM ITextDocument2 wrapper.
		/// Uno returns the managed RichEditTextDocument that backs the Skia document model.
		/// </remarks>
		public RichEditTextDocument Document
		{
			get
			{
#if __SKIA__
				return GetDocument();
#else
				throw new NotImplementedException("RichEditBox.Document is only implemented on Skia.");
#endif
			}
		}

		/// <summary>
		/// Gets the text document associated with this RichEditBox.
		/// </summary>
		public RichEditTextDocument TextDocument => Document;

#if __SKIA__
		private partial RichEditTextDocument GetDocument();
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new RichEditBoxAutomationPeer(this);
		}

		// MUX Reference: RichEditBox_Partial.cpp - OnGotFocus / OnLostFocus
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

#if __SKIA__
			OnGotFocusSkia();
#endif

			UpdateVisualState();
			UpdatePlaceholderVisibility();
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

#if __SKIA__
			OnLostFocusSkia();
#endif

			UpdateVisualState();
			UpdatePlaceholderVisibility();
		}

		partial void OnGotFocusSkia();
		partial void OnLostFocusSkia();

		// MUX Reference: RichEditBox_Partial.cpp - UpdateVisualState
		private void UpdateVisualState()
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", true);
			}
			else if (FocusState != FocusState.Unfocused)
			{
				VisualStateManager.GoToState(this, "Focused", true);
			}
			else if (_isPointerOver)
			{
				VisualStateManager.GoToState(this, "PointerOver", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}

		private bool _isPointerOver;

		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
			_isPointerOver = true;
			UpdateVisualState();
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);
			_isPointerOver = false;
			UpdateVisualState();
		}

		// MUX Reference: RichEditBox_Partial.cpp - UpdateHeaderVisibility
		private void UpdateHeaderVisibility()
		{
			var headerPresenter = GetTemplateChild(HeaderContentPartName) as ContentPresenter;
			if (headerPresenter != null)
			{
				headerPresenter.Visibility = (Header != null || HeaderTemplate != null)
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		// MUX Reference: RichEditBox_Partial.cpp - ShowPlaceholderTextHandler
		private void UpdatePlaceholderVisibility()
		{
			var placeholder = GetTemplateChild(PlaceholderTextPartName) as FrameworkElement;
			if (placeholder != null)
			{
				var isEmpty = true;
#if __SKIA__
				isEmpty = IsDocumentEmpty();
#endif
				placeholder.Visibility = isEmpty && FocusState == FocusState.Unfocused
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

#if __SKIA__
		private partial bool IsDocumentEmpty();
#endif

		private void UpdateDescriptionVisibility(bool initial)
		{
			var descriptionPresenter = GetTemplateChild(DescriptionPresenterPartName) as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		// ===== Event raisers =====

		internal void RaiseTextChanged()
		{
			TextChanged?.Invoke(this, new RoutedEventArgs(this));
		}

		internal void RaiseTextChanging()
		{
			TextChanging?.Invoke(this, new RichEditBoxTextChangingEventArgs());
		}

		internal void RaiseSelectionChanged()
		{
			SelectionChanged?.Invoke(this, new RoutedEventArgs(this));
		}

		internal void RaisePaste(TextControlPasteEventArgs args)
		{
			Paste?.Invoke(this, args);
		}
	}
}
