using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarToggleButton
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

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		_contentChangedHandler.Disposable = null;
		_iconChangedHandler.Disposable = null;
	}
}
