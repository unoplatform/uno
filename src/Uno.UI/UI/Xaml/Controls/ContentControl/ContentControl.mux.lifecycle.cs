#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Text;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ContentControl : Control
{
	public ContentControl()
	{
		DefaultStyleKey = typeof(ContentControl);
	}

	public object Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(object),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((ContentControl)s)?.OnContentChanged(e.OldValue, e.NewValue)
			)
		);

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

	#region ContentTemplate DependencyProperty

	public DataTemplate ContentTemplate
	{
		get => (DataTemplate)GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}

	public static DependencyProperty ContentTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTemplate),
			typeof(DataTemplate),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentControl)s)?.OnContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
			)
		);

	private void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
	{
		Invalidate(Template is null);

		if (newContentTemplate == null)
		{
			DataTemplate contentTemplate = null;
			if (ContentTemplateSelector is { } contentTemplateSelector)
			{
				contentTemplate = RefreshSelectedTemplate(contentTemplateSelector, content: null, reloadContent: true);
			}

			SelectedContentTemplate = contentTemplate;
		}

	}
	#endregion


	#region ContentTemplateSelector DependencyProperty

	public DataTemplateSelector ContentTemplateSelector
	{
		get => (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty);
		set => SetValue(ContentTemplateSelectorProperty, value);
	}

	public static DependencyProperty ContentTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTemplateSelector),
			typeof(DataTemplateSelector),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((ContentControl)s)?.OnContentTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector)
			)
		);

	private void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
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
	#endregion

	#region SelectedContentTemplate DependencyProperty

	public DataTemplate SelectedContentTemplate
	{
		get => (DataTemplate)GetValue(SelectedContentTemplateProperty);
		set => SetValue(SelectedContentTemplateProperty, value);
	}

	public static DependencyProperty SelectedContentTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedContentTemplate),
			typeof(DataTemplate),
			typeof(ContentControl),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
				(s, e) => ((ContentControl)s)?.OnSelectedContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
			)
		);

	private void OnSelectedContentTemplateChanged(DataTemplate oldSelectedContentTemplate, DataTemplate newSelectedContentTemplate)
	{
		if (ContentTemplate is null)
		{
			Invalidate(Template is null);
		}
	}
	#endregion

	private DataTemplate RefreshSelectedTemplate(DataTemplateSelector contentTemplateSelector, object content, bool reloadContent)
	{
		return contentTemplateSelector.SelectTemplate(reloadContent ? Content : content, this);
	}

	private protected override void OnTemplateChanged(DependencyPropertyChangedEventArgs e)
	{
		if (Content is UIElement contentAsUIElement &&
			contentAsUIElement.GetUIElementAdjustedParentInternal() is { } parent)
		{
			parent.RemoveChild(contentAsUIElement);
		}

		base.OnTemplateChanged(e);
	}

	private void Invalidate(bool clearChildren)
	{
		if (clearChildren && GetChildren() is { Count: > 0 })
		{
			ClearChildren();
			// only clear the suggested cp if we actually removed all children!
			m_pTemplatePresenter = null;
		}

		InvalidateMeasure();
	}

	private bool _inOnApplyTemplate;
	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		base.ApplyTemplate(out addedVisuals);
		if (_inOnApplyTemplate)
		{
			return;
		}

		_inOnApplyTemplate = true;

		if (VisualTreeHelper.GetChildrenCount(this) == 0 && Content is { } content)
		{
			CreateDefaultVisuals(this, content as DependencyObject);
			addedVisuals = VisualTreeHelper.GetChildrenCount(this) > 0;
		}

		_inOnApplyTemplate = false;
	}

	private void CreateDefaultVisuals(ContentControl parent, DependencyObject content)
	{
		if (content is UIElement ui)
		{
			parent.AddChild(ui);
		}
		else
		{
			parent.Template = CreateDefaultTemplate(parent);
			parent.ApplyTemplate(out _);
		}
	}

	private const string c_strTextTemplateStorage = """
		<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Grid Background="{TemplateBinding Background}">
				<TextBlock Text="{Binding}" HorizontalAlignment="Left" VerticalAlignment="Top" TextAlignment="Left" TextWrapping="NoWrap" />
			</Grid>
		</ControlTemplate>
		""";

	internal static ControlTemplate CreateDefaultTemplate(FrameworkElement parent)
	{
		var template = (ControlTemplate)XamlReader.Load(c_strTextTemplateStorage);
		template.TargetType = parent.GetType();
		return template;
	}

	public WeakReference<UIElement> m_pTemplatePresenter;

	public UIElement ContentTemplateRoot
	{
		get
		{
			UIElement templateRoot = null;

			if (m_pTemplatePresenter?.TryGetTarget(out var pTemplatePresenter) == true)
			{
				templateRoot = VisualTreeHelper.GetChild(pTemplatePresenter, 0) as UIElement;
			}

			return templateRoot;
		}
	}

	// gets called when a contentpresenter in the template of a contentcontrol is used. It will call back
	// offering its content to compare to the cc's content. If they are the same, we consider that
	// contentpresenter our templateroot.
	internal void ConsiderContentPresenterForContentTemplateRoot(ContentPresenter candidate, object value)
	{
		if (Content == value)
		{
			m_pTemplatePresenter = new WeakReference<UIElement>(candidate);
		}
	}

	#region Transitions Dependency Property

	public TransitionCollection ContentTransitions
	{
		get => (TransitionCollection)this.GetValue(ContentTransitionsProperty);
		set => this.SetValue(ContentTransitionsProperty, value);
	}

	public static DependencyProperty ContentTransitionsProperty { get; } =
		DependencyProperty.Register(nameof(ContentTransitions), typeof(TransitionCollection), typeof(ContentControl), new FrameworkPropertyMetadata(null, OnContentTransitionsChanged));
#nullable enable

	private static void OnContentTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var control = dependencyObject as ContentControl;

		if (control != null)
		{
			var oldValue = (TransitionCollection)args.OldValue;
			var newValue = (TransitionCollection)args.NewValue;

			control.UpdateContentTransitions(oldValue, newValue);
		}
	}

	private void UpdateContentTransitions(TransitionCollection? oldValue, TransitionCollection? newValue)
	{
		var contentRoot = this.ContentTemplateRoot as IFrameworkElement;
		if (contentRoot == null)
		{
			return;
		}
		if (oldValue != null)
		{
			foreach (var item in oldValue)
			{
				item.DetachFromElement(contentRoot);
			}
		}
		if (newValue != null)
		{
			foreach (var item in newValue)
			{
				item.AttachToElement(contentRoot);
			}
		}
	}

	#endregion

#if __CROSSRUNTIME__ // Uno-specific: Keeps API compat.
	protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
#endif
}
#endif
