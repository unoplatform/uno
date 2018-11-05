using System;
using System.Collections.Generic;
using System.Text;
using Android.Widget;
using Uno.UI;
using Java.Lang.Reflect;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Uno.Logging;
using Android.Views;
using Android.Runtime;
using Android.Text;
using Android.Views.InputMethods;
using Android.OS;
using Windows.UI.Xaml.Input;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBoxView : EditText, DependencyObject
	{
		private bool _isRunningTextChanged;
		private bool _isInitialized = false;

		public TextBoxView()
			: base(ContextHelper.Current)
		{
			InitializeBinder();

			base.SetSingleLine(true);

			//This Background color is set to remove the native android underline on the EditText.
			this.SetBackgroundColor(Colors.Transparent);
			//Remove default native padding.
			this.SetPadding(0, 0, 0, 0);

			_isInitialized = true;

			// This ensures the TextBoxView gets resized as Text changes
			LayoutParameters = new Android.Views.ViewGroup.LayoutParams(
				 Android.Views.ViewGroup.LayoutParams.WrapContent,
				 Android.Views.ViewGroup.LayoutParams.WrapContent
			);
		}

		/// <summary>
		/// An alias to the Text property, that allows the interception of the changes.
		/// </summary>
		/// <remarks>
		/// Setting the text via the Text property sets the carret back 
		/// at the beginning, even if the text is the same.
		/// </remarks>
		public string BindableText
		{
			get { return Text; }
			set
			{
				if (!string.Equals(value ?? string.Empty, Text))
				{
					Text = value;
				}
			}
		}

        protected override void OnTextChanged(Java.Lang.ICharSequence text, int start, int lengthBefore, int lengthAfter)
		{
			if (!_isRunningTextChanged && _isInitialized)
			{
				// The Text property cannot be overriden, so we can't prevent this method from being called even if
				// the content really has not changed...

				try
				{
					_isRunningTextChanged = true;

					base.OnTextChanged(text, start, lengthBefore, lengthAfter);

					if (IsStoreInitialized)
					{
						// OnTextChanged is called before the ctor has been executed...
						NotifyTextChanged();
					}
				}
				finally
				{
					_isRunningTextChanged = false;
				}
			}
		}

		protected virtual void NotifyTextChanged()
		{
			SetBindingValue(Text, "BindableText");
		}

		internal void SetCursorColor(Color color)
		{
			EditTextCursorColorChanger.SetCursorColor(this, color);
		}

		/// <summary>
		/// Class that uses reflection to change the color of an EditText cursor at runtime
		/// </summary>
		private class EditTextCursorColorChanger
		{
			private static bool _prepared = false;
			private static Field _editorField;
			private static Field _cursorDrawableField;
			private static Field _cursorDrawableResField;

			private static void PrepareFields(Context context)
			{
				_prepared = true;

				var textView = new TextView(context);
				var editText = new EditText(context);

				_cursorDrawableResField = textView.Class.GetDeclaredField("mCursorDrawableRes");
				_cursorDrawableResField.Accessible = true;

				_editorField = textView.Class.GetDeclaredField("mEditor");
				_editorField.Accessible = true;

				_cursorDrawableField = _editorField.Get(editText).Class.GetDeclaredField("mCursorDrawable");
				_cursorDrawableField.Accessible = true;
			}

			public static void SetCursorColor(EditText editText, Color color)
			{
				try
				{
					if (!_prepared)
					{
						PrepareFields(editText.Context);
					}

					var mCursorDrawableRes = _cursorDrawableResField.GetInt(editText);
					var drawables = new Drawable[2];
					drawables[0] = Android.Support.V4.Content.ContextCompat.GetDrawable(editText.Context, mCursorDrawableRes);
					drawables[1] = Android.Support.V4.Content.ContextCompat.GetDrawable(editText.Context, mCursorDrawableRes);
					drawables[0].SetColorFilter(color, PorterDuff.Mode.SrcIn);
					drawables[1].SetColorFilter(color, PorterDuff.Mode.SrcIn);
					var editor = _editorField.Get(editText);
					_cursorDrawableField.Set(editor, drawables);
				}
				catch (Exception)
				{
					editText.Log().WarnIfEnabled(() => "Failed to change the cursor color. Some devices don't support this.");
				}
			}
		}

		public override void RequestLayout()
		{
			if (IsLoaded && HasSelection) // Getting HasSelection throws an exception if TextBoxView is not loaded.
			{
				// We don't want to RequestLayout when selecting text because
				// it triggers a layout pass which resets selection and
				// dismisses the copy/cut/paste context bar (Android 4.4 and below).
				return;
			}

			base.RequestLayout();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			// On some devices (LG G3), the cursor doesn't appear if the Text is empty.
			// This is due to the TextBoxView's content having a width of 0 if the Text is empty.
			// This code ensures that the TextBoxView's content always has a minimum width, allowing the cursor to be visible.
			var minContentWidth = ViewHelper.LogicalToPhysicalPixels(10d); // arbitrary number, large enough to accomodate cursor
			var minWidth = PaddingLeft + minContentWidth + PaddingRight;
			var newMeasuredWidth = Math.Max(MeasuredWidth, minWidth);
			SetMeasuredDimension(newMeasuredWidth, MeasuredHeight);
		}

		public
#if __ANDROID_23__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(TextBoxView),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBoxView)s).OnForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		private void OnForegroundChanged(Brush oldValue, Brush newValue)
		{
			var scb = newValue as SolidColorBrush;

			if (scb != null)
			{
				this.SetTextColor(scb.Color);
				this.SetCursorColor(scb.Color);
			}
		}
	}
}
