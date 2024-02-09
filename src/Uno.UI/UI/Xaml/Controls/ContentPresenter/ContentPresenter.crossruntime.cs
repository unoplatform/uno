using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private readonly BorderLayerRenderer _borderRenderer;

	public ContentPresenter()
	{
		_borderRenderer = new BorderLayerRenderer(this);
		InitializeContentPresenter();
		InitializePlatform();
	}

	private void SetUpdateTemplate() => UpdateContentTemplateRoot();

	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveChild(ContentTemplateRoot);

	private void UpdateCornerRadius(CornerRadius _) => UpdateBorder();

	private void UpdateBorder() => _borderRenderer.Update();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();
}
