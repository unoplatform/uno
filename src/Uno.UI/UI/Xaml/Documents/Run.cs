using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Documents
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
#if XAMARIN || __WASM__
					coerceValueCallback: Controls.TextBlock.CoerceText,
#endif
					propertyChangedCallback: (s, e) => ((Run)s).OnTextChanged()
				)
			);

		public void OnTextChanged()
		{
			OnTextChangedPartial();
			InvalidateSegments();
			InvalidateSegmentsPartial();
		}

		partial void OnTextChangedPartial();

		#endregion

		protected override void OnForegroundChanged()
		{
			base.OnForegroundChanged();
			InvalidateElement();
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateElement();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateElement();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateElement();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateElement();
			InvalidateSegmentsPartial();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			InvalidateElement();
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			InvalidateElement();
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			InvalidateElement();
		}

		partial void InvalidateSegmentsPartial();
	}
}
