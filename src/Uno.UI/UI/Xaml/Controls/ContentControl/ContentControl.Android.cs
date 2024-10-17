using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	// Android partial
	public partial class ContentControl
	{
		partial void InitializePartial()
		{
			IFrameworkElementHelper.Initialize(this);
		}

		partial void RegisterContentTemplateRoot()
		{
			// The same view can be shared by multiple ContentControls as their Content.
			// Normally, that is okay. However with IsContentPresenterBypassEnabled,
			// it can be problematic with the content added as direct child by multiple parents.
			// In such case, the inner-most/the most late one should own this view.
			// But because, it was already added as child by the predecessor,
			if (ContentTemplateRoot is not null &&
				ContentTemplateRoot == Content &&
				ContentTemplateRoot.Parent is BindableView currentParent)
			{
				// remove it from its current parent, prior to adding it under `this`.
				currentParent.RemoveView(ContentTemplateRoot);
			}

			AddView(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			this.RemoveViewAndDispose(ContentTemplateRoot);
		}
	}
}
