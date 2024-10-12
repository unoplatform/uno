#if UNO_HAS_ENHANCED_LIFECYCLE
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private bool _inOnApplyTemplate;
	private bool _dataContextInvalid = true;
	private const FrameworkPropertyMetadataOptions ContentPropertyOptions = FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure;

	public FrameworkTemplate SelectedContentTemplate
	{
		get => (FrameworkTemplate)GetValue(SelectedContentTemplateProperty);
		set => SetValue(SelectedContentTemplateProperty, value);
	}

	public static DependencyProperty SelectedContentTemplateProperty { get; } = DependencyProperty.Register(
		nameof(SelectedContentTemplate),
		typeof(FrameworkTemplate),
		typeof(ContentPresenter),
		new FrameworkPropertyMetadata(null, OnSelectedContentTemplateChanged));

	private static void OnSelectedContentTemplateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var @this = (ContentPresenter)dependencyObject;
		if (@this.ContentTemplate is null)
		{
			@this.Invalidate(true);
		}
	}

	private protected override FrameworkTemplate GetTemplate() => (ContentTemplate ?? SelectedContentTemplate) ?? GetDefaultContentPresenterTemplate();

	private static DataTemplate _defaultContentPresenterTemplate;

	private static DataTemplate GetDefaultContentPresenterTemplate()
	{
		return _defaultContentPresenterTemplate ??= new DisplayMemberTemplate();
	}

	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		addedVisuals = false;

		if (_inOnApplyTemplate)
		{
			base.ApplyTemplate(out addedVisuals);
			_dataTemplateUsedLastUpdate = GetTemplate();

			goto Cleanup;
		}

		var store = ((IDependencyObjectStoreProvider)this).Store;

		// Applying the template will not delete existing visuals. This will be done conditionally
		// when the template is invalidated.
		if (!HasTemplateChild())
		{
			var templatedParent = GetTemplatedParent() as ContentControl;
			if (templatedParent is not null)
			{
				bool needsRefresh = false;
				DependencyProperty dpTarget = ContentPresenter.SelectedContentTemplateProperty;
				if (store.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !store.IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.SelectedContentTemplateProperty;
					store.SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				dpTarget = ContentPresenter.ContentTemplateProperty;
				if (store.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !store.IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.ContentTemplateProperty;
					store.SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				dpTarget = ContentPresenter.ContentProperty;
				if (store.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !store.IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.ContentProperty;
					store.SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				// Setting up the binding doesn't get you the values.  We need to call refresh to get the latest value
				// for m_pContentTemplate, SelectedContentTemplate and/or m_pContent for the tests below.
				if (needsRefresh)
				{
					//templatedParent.RefreshTemplateBindings(TemplateBindingsRefreshType.All);
				}
			}

			_inOnApplyTemplate = true;
			TrySetDataContextFromContent(Content);

			if (ContentTemplate is not null || SelectedContentTemplate is not null)
			{
				// Expand the template.
				base.ApplyTemplate(out addedVisuals);
			}
			// if ContentTemplate is empty control template
			// we don't want ContentPresenter to create visuals
			else if (Content is { } content)
			{
				if (content is UIElement ui)
				{
					AddChild(ui);
				}
				else
				{
					TextBlock textBlockChildOfDefaultTemplate;
					base.ApplyTemplate(out addedVisuals);

					// We have a default(secret) Data template for ContentPresenter that should have its TextBlock present in all the UIA views by default.
					// But at the same time we want to mitigate this behavior specifically for Controls like Button where the TextBlock would represent redundant data.
					// At the same time we want to provide a mechanism for other controls if they want to opt-in this behavior. So if any control doesn't want these
					// secret TextBlocks to be present in a certain view they can set AutomationProperties.AccessibilityView="Raw" on the corresponding ContentPresenter,
					// we here exceptionally make sure the set property gets reflected on the secret TextBlock if the default template is getting used.
					var value = AutomationProperties.GetAccessibilityView(this);

					textBlockChildOfDefaultTemplate = GetTextBlockChildOfDefaultTemplate(allowNullContent: false);
					if (textBlockChildOfDefaultTemplate is not null)
					{
						if (value != AccessibilityView.Content)
						{
							AutomationProperties.SetAccessibilityView(textBlockChildOfDefaultTemplate, value);
						}
						//if (store.GetCurrentHighestValuePrecedence(ContentPresenter.OpticalMarginAlignmentProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = OpticalMarginAlignment; ;
						//	if (tempValue != OpticalMarginAlignment.None)
						//	{
						//		textBlockChildOfDefaultTemplate.OpticalMarginAlignment = tempValue;
						//	}
						//}
						//if (store.GetCurrentHighestValuePrecedence(ContentPresenter.TextLineBoundsProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = TextLineBounds;
						//	if (tempValue != TextLineBounds.Full)
						//	{
						//		textBlockChildOfDefaultTemplate.TextLineBounds = tempValue;
						//	}
						//}
						if (store.GetCurrentHighestValuePrecedence(ContentPresenter.TextWrappingProperty) == DependencyPropertyValuePrecedences.Local)
						{
							var tempValue = TextWrapping;
							if (tempValue != TextWrapping.NoWrap)
							{
								textBlockChildOfDefaultTemplate.TextWrapping = tempValue;
							}
						}
						//if (store.GetCurrentHighestValuePrecedence(ContentPresenter.LineStackingStrategyProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = LineStackingStrategy;
						//	if (tempValue != LineStackingStrategy.MaxHeight)
						//	{
						//		textBlockChildOfDefaultTemplate.LineStackingStrategy = tempValue;
						//	}
						//}
						if (store.GetCurrentHighestValuePrecedence(ContentPresenter.MaxLinesProperty) == DependencyPropertyValuePrecedences.Local)
						{
							var tempValue = MaxLines;
							if (tempValue != 0)
							{
								textBlockChildOfDefaultTemplate.MaxLines = tempValue;
							}
						}
						//if (store.GetCurrentHighestValuePrecedence(ContentPresenter.LineHeightProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = LineHeight;
						//	if (tempValue > 0)
						//	{
						//		textBlockChildOfDefaultTemplate.LineHeight = tempValue;
						//	}
						//}
					}
				}
			}

			addedVisuals = HasTemplateChild();
			_dataTemplateUsedLastUpdate = GetTemplate();
		}
		else if (_dataContextInvalid)
		{
			TrySetDataContextFromContent(Content);
		}

	Cleanup:
		// Uno-specific
		ContentTemplateRoot = VisualTreeHelper.GetChild(this, 0) as UIElement;
		_dataContextInvalid = false;
		_inOnApplyTemplate = false;
	}

	// Fetches the child TextBlock of the default template if we are using the default template; null otherwise.
	private TextBlock GetTextBlockChildOfDefaultTemplate(bool allowNullContent)
	{
		var content = Content;
		// Make sure we are indeed using the default template (i.e. content is non-null and is not a UIElement).
		if (allowNullContent || (content is not null && content is not UIElement))
		{
			var children = this.GetChildren();
			if (children is { Count: >= 1 })
			{
				var child = children[0];
				if (child is not null)
				{
					// The TextBlock can now be the first child of the ContentPresenter
					if (child is TextBlock childTb)
					{
						return childTb;
					}
					else
					{
						// Old template with the Grid
						children = child.GetChildren();
						if (children is { Count: 1 })
						{
							child = children[0];
							if (child is TextBlock childTb2)
							{
								return childTb2;
							}
						}
					}
				}
			}
		}

		return null;
	}


	private void Invalidate(bool clearChildren)
	{
		if (clearChildren)
		{
			ClearChildren();

			IsUsingDefaultTemplate = false;
		}
		else
		{
			_dataContextInvalid = true;
		}

		InvalidateMeasure();
	}

	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

#if !UNO_HAS_BORDER_VISUAL
		UpdateBorder();
#endif
		// We do this in Enter not Loaded since Loaded is a lot more tricky
		// (e.g. you can have Unloaded without Loaded, you can have multiple loaded events without unloaded in between, etc.)
		if (IsNativeHost)
		{
			AttachNativeElement();
		}
	}

	internal override void LeaveImpl(LeaveParams @params)
	{
		base.LeaveImpl(@params);

		if (IsNativeHost)
		{
			DetachNativeElement(Content);
		}
	}

	public void UpdateContentTemplateRoot()
	{
	}

	internal TextBlock CreateDefaultContent()
	{
		var textBlock = new TextBlock();
		// Act as if the TextBlock was the result of a template expansion
		textBlock.SetTemplatedParent(this);
		textBlock.HorizontalAlignment = HorizontalAlignment.Left;
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		BindDefaultTextBlock(textBlock);

		// Uno-specific
		ContentTemplateRoot = textBlock;

		IsUsingDefaultTemplate = true;

		return textBlock;
	}

	private void BindDefaultTextBlock(TextBlock textBlock)
	{
		var binding = new Binding();
		binding.Mode = BindingMode.OneWay;
		textBlock.SetBinding(TextBlock.TextProperty, binding);
	}
}
#endif
