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
			get { return (string)this.GetValue(TextProperty); }
			set { this.SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
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
			this.InvalidateInlines();
		}

		partial void OnTextChangedPartial();

		#endregion

		protected override void OnForegroundChanged()
		{
			base.OnForegroundChanged();
			this.InvalidateInlines();
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			this.InvalidateInlines();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			this.InvalidateInlines();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			this.InvalidateInlines();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			this.InvalidateInlines();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			this.InvalidateInlines();
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			this.InvalidateInlines();
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			this.InvalidateInlines();
		}
	}
}
