using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Declares a Content presenter
	/// </summary>
	/// <remarks>
	/// The content presenter is used for compatibility with WPF concepts,
	/// but the ContentSource property is not available, because there are ControlTemplates for now.
	/// </remarks>
	public partial class ContentPresenter : FrameworkElement
	{
		public ContentPresenter()
		{
			InitializeContentPresenter();
		}

		private void SetUpdateTemplate()
		{
			UpdateContentTemplateRoot();
		}

		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
		}

		private void UpdateCornerRadius(CornerRadius radius)
		{
		}

		private void UpdateBorder()
		{
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}
	}
}
