using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents
{
	[ContentProperty(Name = nameof(Text))]
	public partial class Run : Inline
	{
		#region Text Dependency Property

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(Run),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					propertyChangedCallback: (s, e) => ((Run)s).OnTextChanged()
				)
			);

		public void OnTextChanged()
		{
			OnTextChangedPartial();
			// The run's length feeds every ancestor's cached position counts, so drop those first.
			MarkDirty();
			InvalidateInlines(true);
			InvalidateSegmentsPartial();
		}

		partial void OnTextChangedPartial();

		#endregion

		protected override void OnForegroundChanged()
		{
			base.OnForegroundChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			InvalidateInlinesForFormatChange();
		}

		partial void InvalidateSegmentsPartial();
	}
}
