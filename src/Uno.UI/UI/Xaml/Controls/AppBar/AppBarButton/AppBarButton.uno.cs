using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton
{
	private readonly SerialDisposable _contentChangedHandler = new();
	private readonly SerialDisposable _iconChangedHandler = new();

	// TODO Uno: ContentPresenter should fall back to Content when value is not available #19649
	private void SetupContentUpdate()
	{
		var contentPresenter = GetTemplateChild<ContentPresenter>("Content");

		void UpdateContent()
		{
			if (contentPresenter != null)
			{
				contentPresenter.Content = Icon ?? Content;
			}
		}

		_contentChangedHandler.Disposable = this.RegisterDisposablePropertyChangedCallback(ContentProperty, (s, e) => UpdateContent());
		_iconChangedHandler.Disposable = this.RegisterDisposablePropertyChangedCallback(IconProperty, (s, e) => UpdateContent());
		UpdateContent();
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		// TODO Uno: This might not be needed - the flyout is owned by the AppBarButton, so it should be removed along with the button itself - needs leak test validation.
		// Avoid memory leaks by unsubscribing and re-subscribing to flyout events
		if (Flyout is not null)
		{
			AttachFlyout(Flyout);
		}

		// Re-setup content update handlers that were disposed in OnUnloaded
		if (m_isTemplateApplied)
		{
			SetupContentUpdate();
		}
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		// Detach event handlers for flyout
		m_flyoutOpenedHandler.Disposable = null;
		m_flyoutClosedHandler.Disposable = null;

		m_menuHelper = null;

		_contentChangedHandler.Disposable = null;
		_iconChangedHandler.Disposable = null;
	}
}
