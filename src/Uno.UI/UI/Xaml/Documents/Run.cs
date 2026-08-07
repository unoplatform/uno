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
			InvalidateInlinesForFormatChange(ForegroundProperty);
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateInlinesForFormatChange(FontFamilyProperty);
			InvalidateSegmentsPartial();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlinesForFormatChange(FontSizeProperty);
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlinesForFormatChange(FontStyleProperty);
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlinesForFormatChange(FontStretchProperty);
			InvalidateSegmentsPartial();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlinesForFormatChange(FontWeightProperty);
			InvalidateSegmentsPartial();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			InvalidateInlinesForFormatChange(BaseLineAlignmentProperty);
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			InvalidateInlinesForFormatChange(CharacterSpacingProperty);
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			InvalidateInlinesForFormatChange(TextDecorationsProperty);
		}

		partial void InvalidateSegmentsPartial();
	}
}
