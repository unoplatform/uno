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
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		partial void InitializePartial()
		{
			IFrameworkElementHelper.Initialize(this);
		}

		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (!IsDefaultStyleApplied && Equals(DefaultStyleKey, typeof(ContentControl)))
			{
				// For the specific case of a ContentControl, if being measured before default style is applied (ie created programmatically and not
				// yet in the visual tree), make sure the style is applied. This aligns the behaviour of WASM with other platforms (UWP introduces
				// ContentPresenter programmatically, Android/iOS skip it entirely)
				ApplyDefaultStyle();
			}
			return base.MeasureOverride(availableSize);
		}
	}
}
