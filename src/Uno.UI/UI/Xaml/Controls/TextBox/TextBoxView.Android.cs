#nullable enable

using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics;
using Java.Lang.Reflect;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class TextBoxView : EditText, DependencyObject
	{
		private bool _isRunningTextChanged;
		private bool _isInitialized;
		private (InputTypes InputType, InputTypes RawInputType) _inputTypes;

		private readonly ManagedWeakReference? _ownerRef;
		internal TextBox? Owner => _ownerRef?.Target as TextBox;

		private Action? _foregroundChanged;

		public TextBoxView(TextBox owner)
			: base(ContextHelper.Current)
		{
			_ownerRef = WeakReferencePool.RentWeakReference(this, owner);
			InitializeBinder();

			UpdateSingleLineMode();

			//This Background color is set to remove the native android underline on the EditText.
			this.SetBackgroundColor(Colors.Transparent);
			//Remove default native padding.
			this.SetPadding(0, 0, 0, 0);

			if (FeatureConfiguration.TextBox.HideCaret)
			{
				SetCursorVisible(false);
			}

			_isInitialized = true;

			// This ensures the TextBoxView gets resized as Text changes
			LayoutParameters = new Android.Views.ViewGroup.LayoutParams(
				 Android.Views.ViewGroup.LayoutParams.WrapContent,
				 Android.Views.ViewGroup.LayoutParams.WrapContent
			);

			_inputTypes = (InputType, InputType);
		}

		internal void SetInputTypes(InputTypes inputType, InputTypes rawInputType)
		{
			_inputTypes = (inputType, rawInputType);
			ResetInputTypes();
		}

		internal void ResetInputTypes()
		{
			InputType = _inputTypes.InputType;
			SetRawInputType(_inputTypes.RawInputType);
		}

		internal void UpdateSingleLineMode()
		{
			if (Owner is { } owner)
			{
				SetMaxLines(owner.AcceptsReturn ? int.MaxValue : 1);

				base.SetSingleLine(owner.TextWrapping == TextWrapping.NoWrap && !owner.AcceptsReturn);
			}
		}

		internal void SetTextNative(string text)
		{
			var textSafe = text ?? string.Empty;
			if (textSafe != Text)
			{
				/// Setting the text via the Text property sets the caret back 
				/// at the beginning, even if the text is the same.
				Text = textSafe;
			}
		}

		public override bool OnTextContextMenuItem(int id)
		{
			if (id == Android.Resource.Id.Paste)
			{
				var args = new TextControlPasteEventArgs();
				Owner?.RaisePaste(args);
				if (args.Handled)
				{
					return true;
				}
			}

			return base.OnTextContextMenuItem(id);
		}

		protected override void OnTextChanged(Java.Lang.ICharSequence? text, int start, int lengthBefore, int lengthAfter)
		{
			if (!_isRunningTextChanged && _isInitialized)
			{
				// The Text property cannot be overridden, so we can't prevent this method from being called even if
				// the content really has not changed...

				try
				{
					_isRunningTextChanged = true;

					base.OnTextChanged(text, start, lengthBefore, lengthAfter);

					NotifyTextChanged();
				}
				finally
				{
					_isRunningTextChanged = false;
				}
			}
		}

		private void NotifyTextChanged()
		{
			if (Owner != null) // OnTextChanged is called before the ctor has been executed...
			{
				var text = Owner.ProcessTextInput(Text);
				SetTextNative(text);
			}
		}

		public override IInputConnection OnCreateInputConnection(EditorInfo? outAttrs)
		{
			return new TextBox.TextBoxInputConnection(this, base.OnCreateInputConnection(outAttrs));
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
			private static bool _prepared;
			private static Field? _editorField;
			private static Field? _cursorDrawableField;
			private static Field? _cursorDrawableResField;

			private static void PrepareFields(Context? context)
			{
				_prepared = true;

				Java.Lang.Class textViewClass;
				using (var textView = new TextView(context))
				{
					textViewClass = textView.Class;
				}
				var editText = new EditText(context);

				if ((int)Build.VERSION.SdkInt < 29)
				{
					_cursorDrawableResField = textViewClass.GetDeclaredField("mCursorDrawableRes");
					_cursorDrawableResField.Accessible = true;
				}

				_editorField = textViewClass.GetDeclaredField("mEditor");
				_editorField.Accessible = true;

				if ((int)Build.VERSION.SdkInt < 28) // 28 means BuildVersionCodes.P
				{
					_cursorDrawableField = _editorField.Get(editText)?.Class.GetDeclaredField("mCursorDrawable");
				}
				else if ((int)Build.VERSION.SdkInt == 28)
				{
					// set differently in Android P (API 28)
					_cursorDrawableField = _editorField.Get(editText)?.Class.GetDeclaredField("mDrawableForCursor");
				}
				// Android versions 29+ are handled directly through SetColorFilter API

				if (_cursorDrawableField != null)
				{
					_cursorDrawableField.Accessible = true;
				}
			}

			public static void SetCursorColor(EditText editText, Color color)
			{
				try
				{
					if (!_prepared)
					{
						PrepareFields(editText.Context);
					}

					if (PorterDuff.Mode.SrcIn == null)
					{
						editText.Log().Warn("Failed to change the cursor color. Some devices don't support this.");
						return;
					}

					if ((int)Build.VERSION.SdkInt >= 29)
					{
						var drawable = editText.TextCursorDrawable;
						if (BlendMode.SrcAtop != null)
						{
							var colorFilter = BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(
								(Android.Graphics.Color)color,
								BlendModeCompat.SrcAtop!);
							drawable?.SetColorFilter(colorFilter);
						}
					}
					else if (_cursorDrawableField == null || _cursorDrawableResField == null || _editorField == null || PorterDuff.Mode.SrcIn == null)
					{
						// PrepareFields() failed, give up now
						editText.Log().Warn("Failed to change the cursor color. Some devices don't support this.");
						return;
					}
					else
					{
						var mCursorDrawableRes = _cursorDrawableResField.GetInt(editText);
						var editor = _editorField.Get(editText);

						var colorFilter = new PorterDuffColorFilter((Android.Graphics.Color)color, PorterDuff.Mode.SrcIn);

						if ((int)Build.VERSION.SdkInt < 28) // 28 means BuildVersionCodes.P
						{
							var drawables = new Drawable[2];
							drawables[0] = ContextCompat.GetDrawable(editText.Context!, mCursorDrawableRes)!;
							drawables[1] = ContextCompat.GetDrawable(editText.Context!, mCursorDrawableRes)!;
							drawables[0].SetColorFilter(colorFilter);
							drawables[1].SetColorFilter(colorFilter);
							_cursorDrawableField.Set(editor, drawables);
						}
						else
						{
							var drawable = ContextCompat.GetDrawable(editText.Context!, mCursorDrawableRes)!;
							drawable.SetColorFilter(colorFilter);
							_cursorDrawableField.Set(editor, drawable);
						}
					}
				}
				catch (Exception)
				{
					editText.Log().Warn("Failed to change the cursor color. Some devices don't support this.");
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
			var minContentWidth = ViewHelper.LogicalToPhysicalPixels(10d); // arbitrary number, large enough to accommodate cursor
			var minWidth = PaddingLeft + minContentWidth + PaddingRight;
			var newMeasuredWidth = Math.Max(MeasuredWidth, minWidth);
			SetMeasuredDimension(newMeasuredWidth, MeasuredHeight);
		}

		public
#if __ANDROID__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		public static DependencyProperty ForegroundProperty { get; } =
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
			if (newValue is SolidColorBrush scb)
			{
				Brush.SetupBrushChanged(oldValue, newValue, ref _foregroundChanged, () => ApplyColor());

				void ApplyColor()
				{
					SetTextColor(scb.Color);
					SetCursorColor(scb.Color);
				}
			}
		}
	}
}
