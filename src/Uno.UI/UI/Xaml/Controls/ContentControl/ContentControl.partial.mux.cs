// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentControl_Partial.cpp, tag winui3/release/1.8.2, commit bac7a9c33

// Public members stay nullable-oblivious to match WinUI.
#nullable disable

using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentControl
{
	public ContentControl()
	{
		DefaultStyleKey = typeof(ContentControl);
	}

	/// <summary>
	/// Called when the value of the <see cref="Content"/> property changes.
	/// </summary>
	/// <param name="oldContent">The old value of the Content property.</param>
	/// <param name="newContent">The new value of the Content property.</param>
	protected virtual void OnContentChanged(object oldContent, object newContent)
	{
		Invalidate(
			(Template is null) &&
			(oldContent is UIElement || newContent is UIElement)
		);

		if (newContent is not null)
		{
			DataTemplate contentTemplate = ContentTemplate;
			if (contentTemplate is null)
			{
				var contentTemplateSelector = ContentTemplateSelector;
				if (contentTemplateSelector is not null)
				{
					contentTemplate = RefreshSelectedTemplate(contentTemplateSelector, newContent, reloadContent: false);
				}

				SelectedContentTemplate = contentTemplate;
			}
		}
	}

	/// <summary>
	/// Called when the value of the <see cref="ContentTemplate"/> property changes.
	/// </summary>
	/// <param name="oldContentTemplate">The old value of the ContentTemplate property.</param>
	/// <param name="newContentTemplate">The new value of the ContentTemplate property.</param>
	protected virtual void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
	{
		Invalidate(Template is null);

		if (newContentTemplate is null)
		{
			DataTemplate contentTemplate = null;
			if (ContentTemplateSelector is { } contentTemplateSelector)
			{
				contentTemplate = RefreshSelectedTemplate(contentTemplateSelector, content: null, reloadContent: true);
			}

			SelectedContentTemplate = contentTemplate;
		}
	}

	/// <summary>
	/// Called when the value of the <see cref="ContentTemplateSelector"/> property changes.
	/// </summary>
	/// <param name="oldContentTemplateSelector">The old value of the ContentTemplateSelector property.</param>
	/// <param name="newContentTemplateSelector">The new value of the ContentTemplateSelector property.</param>
	protected virtual void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
	{
		var contentTemplate = ContentTemplate;
		if (contentTemplate is null)
		{
			if (newContentTemplateSelector is not null)
			{
				contentTemplate = RefreshSelectedTemplate(newContentTemplateSelector, content: null, reloadContent: true);
			}

			SelectedContentTemplate = contentTemplate;
		}
	}

	private void OnSelectedContentTemplateChanged(DataTemplate oldSelectedContentTemplate, DataTemplate newSelectedContentTemplate)
	{
		if (ContentTemplate is null)
		{
			Invalidate(Template is null);
		}
	}

	private DataTemplate RefreshSelectedTemplate(DataTemplateSelector contentTemplateSelector, object content, bool reloadContent)
	{
		return contentTemplateSelector.SelectTemplate(reloadContent ? Content : content, this)
			?? contentTemplateSelector.SelectTemplate(reloadContent ? Content : content) /*Uno specific*/;
	}

	private void UpdateContentTransitions(TransitionCollection oldValue, TransitionCollection newValue)
	{
		if (ContentTemplateRoot is not IFrameworkElement contentRoot)
		{
			return;
		}

		if (oldValue is not null)
		{
			foreach (var item in oldValue)
			{
				item.DetachFromElement(contentRoot);
			}
		}

		if (newValue is not null)
		{
			foreach (var item in newValue)
			{
				item.AttachToElement(contentRoot);
			}
		}
	}

	// Uno specific: kept to preserve the previously public override in the API surface.
	protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
}
