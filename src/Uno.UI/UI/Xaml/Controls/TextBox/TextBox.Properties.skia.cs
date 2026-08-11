#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBox
{
	// The Skia-only public surface. The implementation lives in TextBoxCore; these stay here because a
	// dependency property needs GetValue/SetValue, which the core cannot have — it is not a DependencyObject.

	public static DependencyProperty CanUndoProperty { get; } = DependencyProperty.Register(
		nameof(CanUndo),
		typeof(bool),
		typeof(TextBox),
		new FrameworkPropertyMetadata(defaultValue: false)
		{
			PropMethodCall = GetCanUndo,
		});

	public bool CanUndo => (bool)GetValue(CanUndoProperty);

	private static object? GetCanUndo(DependencyObject instance, bool isGet, object? valueToSet)
	{
		if (!isGet)
		{
			throw new InvalidOperationException($"{nameof(CanUndoProperty)} is read-only.");
		}

		return Uno.UI.Helpers.Boxes.Box(((TextBox)instance)._core.CanUndoInternal);
	}

	public static DependencyProperty CanRedoProperty { get; } = DependencyProperty.Register(
		nameof(CanRedo),
		typeof(bool),
		typeof(TextBox),
		new FrameworkPropertyMetadata(defaultValue: false)
		{
			PropMethodCall = GetCanRedo,
		});

	public bool CanRedo => (bool)GetValue(CanRedoProperty);

	private static object? GetCanRedo(DependencyObject instance, bool isGet, object? valueToSet)
	{
		if (!isGet)
		{
			throw new InvalidOperationException($"{nameof(CanRedoProperty)} is read-only.");
		}

		return Uno.UI.Helpers.Boxes.Box(((TextBox)instance)._core.CanRedoInternal);
	}

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty CanPasteClipboardContentProperty { get; } = CreateCanPasteClipboardContentProperty();

	public bool CanPasteClipboardContent
	{
		get => GetCanPasteClipboardContentValue();
		private set => SetCanPasteClipboardContentValue(value);
	}

	bool ITextBoxHost.CanPasteClipboardContent
	{
		get => CanPasteClipboardContent;
		set => CanPasteClipboardContent = value;
	}

	public static DependencyProperty SelectionFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionFlyout), typeof(FlyoutBase), typeof(TextBox),
			new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	public FlyoutBase SelectionFlyout
	{
		get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
		set => SetValue(SelectionFlyoutProperty, value);
	}

	FlyoutBase? ITextBoxHost.SelectionFlyout => SelectionFlyout;

	public static DependencyProperty ProofingMenuFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(ProofingMenuFlyout), typeof(FlyoutBase), typeof(TextBox),
			new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	public FlyoutBase ProofingMenuFlyout
	{
		get
		{
			var flyout = _core.EnsureAndUpdateProofingMenu();
			SetValue(ProofingMenuFlyoutProperty, flyout);
			return flyout;
		}
	}

	public int SelectionStart
	{
		get => _core.SelectionStart;
		set => _core.SelectionStart = value;
	}

	public int SelectionLength
	{
		get => _core.SelectionLength;
		set => _core.SelectionLength = value;
	}

	public void ClearUndoRedoHistory() => _core.ClearUndoRedoHistory();

	public void Undo() => _core.Undo();

	public void Redo() => _core.Redo();

	public event TypedEventHandler<TextBox, TextCompositionStartedEventArgs>? TextCompositionStarted;

	public event TypedEventHandler<TextBox, TextCompositionChangedEventArgs>? TextCompositionChanged;

	public event TypedEventHandler<TextBox, TextCompositionEndedEventArgs>? TextCompositionEnded;

	void ITextBoxHost.RaiseTextCompositionStarted(TextCompositionStartedEventArgs args) => TextCompositionStarted?.Invoke(this, args);

	void ITextBoxHost.RaiseTextCompositionChanged(TextCompositionChangedEventArgs args) => TextCompositionChanged?.Invoke(this, args);

	void ITextBoxHost.RaiseTextCompositionEnded(TextCompositionEndedEventArgs args) => TextCompositionEnded?.Invoke(this, args);

	internal ContentControl ContentElement => _core.ContentElement;

	internal TextBoxView? TextBoxView => _core.TextBoxView;

	internal TextBoxCore.CaretDisplayMode CaretMode => _core.CaretMode;

	internal TextBoxCore.TouchTextSelectionConvention TouchSelectionConvention
	{
		get => _core.TouchSelectionConvention;
		set => _core.TouchSelectionConvention = value;
	}

	internal bool IsComposing => _core.IsComposing;

	internal int CompositionUnderlineStart => _core.CompositionUnderlineStart;

	internal int CompositionUnderlineLength => _core.CompositionUnderlineLength;

	internal void ForceFocusLoss() => _core.ForceFocusLoss();

	internal void DismissAllFlyouts() => _core.DismissAllFlyouts();

	internal bool FireContextMenuOpeningEventSynchronously(Point point) => _core.FireContextMenuOpeningEventSynchronously(point);

	internal Point? GetContextMenuShowPosition() => _core.GetContextMenuShowPosition();

	internal bool IsBackwardSelection => _core.IsBackwardSelection;

	internal (CaretWithStemAndThumb start, CaretWithStemAndThumb end)? VisibleGrippersForTesting => _core.VisibleGrippersForTesting;

	internal static IDisposable SetImeExtensionForTesting(IImeTextBoxExtension extension) => TextBoxCore.SetImeExtensionForTesting(extension);
}
