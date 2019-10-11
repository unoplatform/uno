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
			_revealButton?.ApplyTemplate();

			// Button doesn't raise the PointerPressed event, so we listen for it on the template visual root.
			var revealTarget = (_revealButton?.TemplatedRoot ?? _revealButton?.ContentTemplateRoot) as UIElement;

			if (revealTarget != null)
			{
				revealTarget.PointerPressed += BeginReveal;
				revealTarget.PointerReleased += EndReveal;
				revealTarget.PointerExited += EndReveal;
				revealTarget.PointerCanceled += EndReveal;

				_revealButtonSubscription.Disposable = Disposable.Create(() =>
				{
					revealTarget.PointerPressed -= BeginReveal;
					revealTarget.PointerReleased -= EndReveal;
					revealTarget.PointerExited -= EndReveal;
					revealTarget.PointerCanceled -= EndReveal;
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
