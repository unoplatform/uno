using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PasswordBox : TextBox
	{
		public event RoutedEventHandler PasswordChanged;

		public const string RevealButtonPartName = "RevealButton";
		private ButtonBase _revealButton;
		private readonly SerialDisposable _revealButtonSubscription = new SerialDisposable();
		private bool UseIsPasswordEnabledProperty => this.IsDependencyPropertySet(IsPasswordRevealButtonEnabledProperty) && !this.IsDependencyPropertySet(PasswordRevealModeProperty);

		public PasswordBox()
		{
			DefaultStyleKey = typeof(PasswordBox);
		}

		public new void SelectAll() => base.SelectAll();

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			RegisterSetPasswordScope();
			UpdateDescriptionVisibility(true);
		}

		private void RegisterSetPasswordScope()
		{
			_revealButton = this.GetTemplateChild(RevealButtonPartName) as ButtonBase;

			if (_revealButton != null)
			{
				var beginReveal = new PointerEventHandler(BeginReveal);
				var endReveal = new PointerEventHandler(EndReveal);

				// Button will handle Pressed and Released, so we have subscribe to handled events too
				_revealButton.AddHandler(PointerPressedEvent, beginReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerReleasedEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerExitedEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerCanceledEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerCaptureLostEvent, endReveal, handledEventsToo: true);

				_revealButtonSubscription.Disposable = Disposable.Create(() =>
				{
					_revealButton.RemoveHandler(PointerPressedEvent, beginReveal);
					_revealButton.RemoveHandler(PointerReleasedEvent, endReveal);
					_revealButton.RemoveHandler(PointerExitedEvent, endReveal);
					_revealButton.RemoveHandler(PointerCanceledEvent, endReveal);
					_revealButton.RemoveHandler(PointerCaptureLostEvent, endReveal);
				});
			}

			CheckRevealModeForScope();
		}

		private void BeginReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordRevealState(PasswordRevealState.Revealed);
		}

		private void EndReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordRevealState(PasswordRevealState.Obscured);
			EndRevealPartial();
		}

		partial void EndRevealPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_revealButtonSubscription.Disposable = null;
		}

		partial void SetPasswordRevealState(PasswordRevealState state);

		#region Password DependencyProperty

		public string Password
		{
			get { return (string)this.GetValue(PasswordProperty); }
			set { this.SetValue(PasswordProperty, value); }
		}

		public static DependencyProperty PasswordProperty { get; } =
			DependencyProperty.Register(
				"Password",
				typeof(string),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordChanged(e)
				)
			);

		private void OnPasswordChanged(DependencyPropertyChangedEventArgs e)
		{
			SetValue(TextProperty, (string)e.NewValue);

			PasswordChanged?.Invoke(this, new RoutedEventArgs(this));

			OnPasswordChangedPartial(e);

			if (Password.IsNullOrEmpty() &&
				((PasswordRevealMode == PasswordRevealMode.Peek) || (UseIsPasswordEnabledProperty && IsPasswordRevealButtonEnabled)))
			{
				_isButtonEnabled = true;
			}
		}

		partial void OnPasswordChangedPartial(DependencyPropertyChangedEventArgs e);

		#endregion

		#region PasswordChar

		private void OnPasswordCharChanged(DependencyPropertyChangedEventArgs e)
		{
			OnPasswordCharChangedPartial(e);
		}

		partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e);

		#endregion

		public new object Description
		{
			get => this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public static new global::Microsoft.UI.Xaml.DependencyProperty DescriptionProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(Description), typeof(object),
				typeof(global::Microsoft.UI.Xaml.Controls.PasswordBox),
				new FrameworkPropertyMetadata(default(object), propertyChangedCallback: (s, e) => (s as PasswordBox)?.UpdateDescriptionVisibility(false)));

		#region SelectionHighlightColor DependencyProperty

		/// <summary>
		/// Gets or sets the brush used to highlight the selected text.
		/// </summary>
		public new SolidColorBrush SelectionHighlightColor
		{
			get => base.SelectionHighlightColor;
			set => base.SelectionHighlightColor = value;
		}

		/// <summary>
		/// Identifies the SelectionHighlightColor dependency property.
		/// </summary>
		public new static DependencyProperty SelectionHighlightColorProperty => TextBox.SelectionHighlightColorProperty;

		#endregion

		protected override void OnTextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTextChanged(e);

			SetValue(PasswordProperty, (string)e.NewValue);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new PasswordBoxAutomationPeer(this);
		}

		public override string GetAccessibilityInnerText()
		{
			// We don't want to reveal the password
			return null;
		}

		/// <summary>
		/// Copies content from the OS clipboard into the text control.
		/// </summary>
		public new void PasteFromClipboard() => base.PasteFromClipboard();

		#region IsPasswordRevealButtonEnabled DependencyProperty
		public bool IsPasswordRevealButtonEnabled
		{
			get => (bool)this.GetValue(IsPasswordRevealButtonEnabledProperty);
			set => this.SetValue(IsPasswordRevealButtonEnabledProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty IsPasswordRevealButtonEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsPasswordRevealButtonEnabled),
				typeof(bool),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnIsPasswordRevealButtonEnabledChanged(e)
				)
			);

		private void OnIsPasswordRevealButtonEnabledChanged(DependencyPropertyChangedEventArgs e)
		{
			CheckRevealModeForScope();
			OnIsPasswordRevealButtonEnabledChangedPartial(e);
		}

		partial void OnIsPasswordRevealButtonEnabledChangedPartial(DependencyPropertyChangedEventArgs e);
		#endregion

		#region PasswordRevealMode DependencyProperty
		public PasswordRevealMode PasswordRevealMode
		{
			get => (PasswordRevealMode)this.GetValue(PasswordRevealModeProperty);
			set => this.SetValue(PasswordRevealModeProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty PasswordRevealModeProperty { get; } =
			DependencyProperty.Register(
				nameof(PasswordRevealMode),
				typeof(PasswordRevealMode),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: PasswordRevealMode.Peek,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordRevealModeChanged(e)
				)
			);

		private void OnPasswordRevealModeChanged(DependencyPropertyChangedEventArgs e)
		{
			CheckRevealModeForScope();
		}

		private void CheckRevealModeForScope()
		{
			// Only use IsPasswordRevealButtonEnabled if it is set and PasswordRevealMode is not
			if (UseIsPasswordEnabledProperty)
			{
				SetPasswordRevealState(PasswordRevealState.Obscured);
			}
			else
			{
				switch (PasswordRevealMode)
				{
					case PasswordRevealMode.Visible:
						SetPasswordRevealState(PasswordRevealState.Revealed);
						break;
					case PasswordRevealMode.Hidden:
					case PasswordRevealMode.Peek:
					default:
						SetPasswordRevealState(PasswordRevealState.Obscured);
						break;
				}
			}
		}
		#endregion

		internal override void UpdateFocusState(FocusState focusState)
		{
			var oldValue = FocusState;
			base.UpdateFocusState(focusState);
			OnFocusStateChanged(oldValue, focusState);
		}

		private void OnFocusStateChanged(FocusState oldValue, FocusState newValue)
		{
			if (oldValue == newValue) { return; }


			if (oldValue == FocusState.Unfocused)
			{
				if (UseIsPasswordEnabledProperty)
				{
					_isButtonEnabled = IsPasswordRevealButtonEnabled;

					if (_isButtonEnabled)
					{
						VisualStateManager.GoToState(this, TextBoxConstants.ButtonVisibleStateName, true);
					}
					else
					{
						VisualStateManager.GoToState(this, TextBoxConstants.ButtonCollapsedStateName, true);
					}
				}
				else
				{
					if (PasswordRevealMode == PasswordRevealMode.Peek && Password.IsNullOrEmpty())
					{
						_isButtonEnabled = true;
					}
					else
					{
						_isButtonEnabled = false;
					}

					VisualStateManager.GoToState(this, TextBoxConstants.ButtonCollapsedStateName, true);
				}
			}
		}

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description == null)
			{
				// Avoid loading DescriptionPresenter element in template if not needed.
				return;
			}

			var descriptionPresenter = this.FindName("DescriptionPresenter") as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

#if !IS_UNIT_TESTS
		/// <summary>
		/// Occurs when text is pasted into the control.
		/// </summary>
		public new event TextControlPasteEventHandler Paste
		{
			add => base.Paste += value;
			remove => base.Paste -= value;
		}
#endif
	}
}
