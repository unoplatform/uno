using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Documents
{
	[ContentProperty(Name = "Text")]
	public partial class Run : Inline
	{
		#region Text Dependency Property

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty { get ; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(Run),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
#if XAMARIN || __WASM__
					coerceValueCallback: Controls.TextBlock.CoerceText,
#endif
					propertyChangedCallback: (s, e) => ((Run)s).OnTextChanged()
				)
			);

		public void OnTextChanged()
		{
			OnTextChangedPartial();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		partial void OnTextChangedPartial();

		#endregion

		protected override void OnForegroundChanged()
		{
			base.OnForegroundChanged();
			InvalidateInlines();
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			InvalidateInlines();
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			InvalidateInlines();
			InvalidateSegmentsPartial();
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			InvalidateInlines();
		}

		partial void InvalidateSegmentsPartial();
	}
}
