// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockOverflow.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#pragma warning disable CS0109

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichTextBlockOverflow
	{
		#region MaxLines Dependency Property

		public int MaxLines
		{
			get => (int)GetValue(MaxLinesProperty);
			set => SetValue(MaxLinesProperty, value);
		}

		public static DependencyProperty MaxLinesProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxLines),
				typeof(int),
				typeof(RichTextBlockOverflow),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));

		#endregion

		#region Padding Dependency Property

		public Thickness Padding
		{
			get => (Thickness)GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register(
				nameof(Padding),
				typeof(Thickness),
				typeof(RichTextBlockOverflow),
				new FrameworkPropertyMetadata(
					(Thickness)Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure));

		#endregion

		#region OverflowContentTarget Dependency Property

		public RichTextBlockOverflow OverflowContentTarget
		{
			get => (RichTextBlockOverflow)GetValue(OverflowContentTargetProperty);
			set => SetValue(OverflowContentTargetProperty, value);
		}

		public static DependencyProperty OverflowContentTargetProperty { get; } =
			DependencyProperty.Register(
				nameof(OverflowContentTarget),
				typeof(RichTextBlockOverflow),
				typeof(RichTextBlockOverflow),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((RichTextBlockOverflow)s).OnOverflowContentTargetChangedPartial(
						(RichTextBlockOverflow)e.OldValue, (RichTextBlockOverflow)e.NewValue)));

		partial void OnOverflowContentTargetChangedPartial(RichTextBlockOverflow oldTarget, RichTextBlockOverflow newTarget);

		#endregion

		#region HasOverflowContent Dependency Property

		public static DependencyProperty HasOverflowContentProperty { get; } =
			DependencyProperty.Register(
				nameof(HasOverflowContent),
				typeof(bool),
				typeof(RichTextBlockOverflow),
				new FrameworkPropertyMetadata(false));

		public bool HasOverflowContent
		{
			get => (bool)GetValue(HasOverflowContentProperty);
			private set => SetValue(HasOverflowContentProperty, value);
		}

		private void SetHasOverflowContent(bool value) => HasOverflowContent = value;

		#endregion

		#region IsTextTrimmed Dependency Property

		public static DependencyProperty IsTextTrimmedProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextTrimmed),
				typeof(bool),
				typeof(RichTextBlockOverflow),
				new FrameworkPropertyMetadata(false));

		public bool IsTextTrimmed
		{
			get => (bool)GetValue(IsTextTrimmedProperty);
			private set => SetValue(IsTextTrimmedProperty, value);
		}

		private TypedEventHandler<RichTextBlockOverflow, IsTextTrimmedChangedEventArgs> _isTextTrimmedChanged;

		public event TypedEventHandler<RichTextBlockOverflow, IsTextTrimmedChangedEventArgs> IsTextTrimmedChanged
		{
			add => _isTextTrimmedChanged += value;
			remove => _isTextTrimmedChanged -= value;
		}

		// Sets IsTextTrimmed and raises IsTextTrimmedChanged when the value flips.
		private void SetIsTextTrimmed(bool value)
		{
			if (IsTextTrimmed != value)
			{
				IsTextTrimmed = value;
				_isTextTrimmedChanged?.Invoke(this, new IsTextTrimmedChangedEventArgs());
			}
		}

		#endregion

		#region ContentSource

		// The master RichTextBlock that overflows content into this element. Resolved during layout from the
		// previous link in the chain (CRichTextBlockOverflow::GetMaster). The overflow layout chain is
		// Skia-only; on other targets there is no master to resolve.
		public RichTextBlock ContentSource =>
#if __SKIA__
			GetMaster();
#else
			null;
#endif

		#endregion
	}
}
