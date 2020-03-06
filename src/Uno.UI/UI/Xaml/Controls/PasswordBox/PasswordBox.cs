using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class PasswordBox : TextBox
	{
		public event RoutedEventHandler PasswordChanged;

		public const string RevealButtonPartName = "RevealButton";
		private Button _revealButton;
		private readonly SerialDisposable _revealButtonSubscription = new SerialDisposable();

		public PasswordBox()
		{

		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			RegisterSetPasswordScope();
		}

		private void RegisterSetPasswordScope()
		{
			_revealButton = this.GetTemplateChild(RevealButtonPartName) as Button;

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

			SetPasswordScope(true);
		}

		private void BeginReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordScope(false);
		}

		private void EndReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordScope(true);
			EndRevealPartial();
		}

		partial void EndRevealPartial();

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_revealButtonSubscription.Disposable = null;
		}

		partial void SetPasswordScope(bool shouldHideText);

		#region Password DependencyProperty

		public string Password
		{
			get { return (string)this.GetValue(PasswordProperty); }
			set { this.SetValue(PasswordProperty, value); }
		}

		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.Register(
				"Password",
				typeof(string),
				typeof(PasswordBox),
				new PropertyMetadata(
					defaultValue: string.Empty,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordChanged(e)
				)
			);

		private void OnPasswordChanged(DependencyPropertyChangedEventArgs e)
		{
			Text = (string)e.NewValue;

			var handler = PasswordChanged;
			if (handler != null)
			{
				handler(this, new RoutedEventArgs(this));
			}

			OnPasswordChangedPartial(e);
		}

		partial void OnPasswordChangedPartial(DependencyPropertyChangedEventArgs e);

		#endregion

		protected override void OnTextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTextChanged(e);

			Password = (string)e.NewValue;
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
	}
}
