using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.NativeElementHosting;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void TryRegisterNativeElement(object oldValue, object newValue)
	{
		IsNativeHost = newValue is BrowserHtmlElement;

		if (IsNativeHost)
		{
			ContentTemplateRoot = null;
		}
	}

	partial void AttachNativeElement()
	{
		if (Content is BrowserHtmlElement element)
		{
			element.AttachToElement(HtmlId);
		}
	}

	partial void DetachNativeElement(object content)
	{
		if (content is BrowserHtmlElement element)
		{
			element.DetachFromElement(HtmlId);
		}
	}
}
