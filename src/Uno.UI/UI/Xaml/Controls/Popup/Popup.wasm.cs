using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		private readonly SerialDisposable _closePopup = new SerialDisposable();

		internal UIElement Anchor { get; set; }

		protected override void OnChildChanged(FrameworkElement oldChild, FrameworkElement newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			if (newChild != null)
			{
				PopupRoot.SetPopup(newChild, this);
			}
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Popup.IsOpenChanged({oldIsOpen}, {newIsOpen})");
			}

			if (newIsOpen)
			{
				_closePopup.Disposable = Window.Current.OpenPopup(this);
			}
			else
			{
				_closePopup.Disposable = null;
			}
		}
	}
}
