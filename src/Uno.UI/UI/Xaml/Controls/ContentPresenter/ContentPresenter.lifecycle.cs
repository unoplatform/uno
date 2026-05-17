#if UNO_HAS_ENHANCED_LIFECYCLE
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

#if __CROSSRUNTIME__
using View = Microsoft.UI.Xaml.UIElement;
// In case other platforms supported enhanced lifecycle, add the corresponding 'using View = ...' for them.
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private bool _inOnApplyTemplate;
	private bool _dataContextInvalid = true;
	private object _cachedContent;
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

	protected virtual void OnContentChanged(object oldValue, object newValue)
	{
		if (GetTemplatedParent() is ContentControl contentControl)
		{
			contentControl.ConsiderContentPresenterForContentTemplateRoot(this, newValue);
		}

		// BEGIN Uno-specific
		if (oldValue is View || newValue is View)
		{
			// Make sure not to reuse the previous Content as a ContentTemplateRoot (i.e., in case there's no data template)
			// If setting Content to a new View, recreate the template
			ContentTemplateRoot = null;
		}
		// END Uno-specific

		bool fInvalidationNeeded = false;
		bool participatesInUnloading = this.ParticipatesInUnloadingContentTransition();
		// Invalidating the tree is only "worthwhile" if we don't have a locally defined content template or selected template,
		// Checking this early prevents unnecessary calls to FrameworkCallbacks_AreObjectsOfSameType, which is expensive,
		// and causes a performance hit in long list (Xbox store results) scrolling scenarios
		bool fInvalidationWorthwhile = ContentTemplate == null && SelectedContentTemplate == null;

		// 1. content (old) is a uielement
		// 2. content (new) is a uielement
		// 3. content is a string and this contentpresenter has contenttransitions
		// in these cases we also need to invalidate our first child in order to show that
		// the transitions on our child should seemingly 'bubble up' to this cp.
		if (oldValue is View)
		{
			fInvalidationNeeded = fInvalidationWorthwhile;
		}
		else if (newValue is View)
		{
			fInvalidationNeeded = fInvalidationWorthwhile;
		}
		else if (participatesInUnloading)
		{
			// For this case we also validate if there are ContentTransitions since the tree has to be invalidated in order for them to be processed.
			fInvalidationNeeded = fInvalidationWorthwhile || (ContentTransitions != null && ContentTransitions.Count > 0);
		}
		// Uno docs: MOR is likely Managed Object Reference
		// Uno docs: This ported code may not fully match WinUI and could be revised in future.
		// 4. Content change should trigger template invalidation if the types of
		// old and new content do not match. Exception to this rule is when the
		// old and new content are both of type valueObject, in which case some special
		// rules apply -
		// 4a. Content(MOR) -> NULL.
		// 4b. NULL -> Content(MOR) where cached content is MOR with same type object.
		// 4c. If neither the old nor the new content is UIElement, compare the types of the two objects.
		//     If the types do not match, invalidate the template.
		else
		{
			fInvalidationNeeded = fInvalidationWorthwhile;
			if (newValue is null)
			{
				var child = this.GetFirstChild();
				if (child is not null)
				{
					if (oldValue is not null && (oldValue is not DependencyObject /*|| ((DependencyObject)oldValue).GetTypeIndex() == ExternalObjectReference*/))
					{
						// Cache reference to old content value.
						_cachedContent = oldValue;
						child.Visibility = Visibility.Collapsed;
						fInvalidationNeeded = false;
					}
				}
				else
				{
					fInvalidationNeeded = false;
				}
			}
			else if (oldValue is null && newValue is not null && _cachedContent is not null)
			{
				var child = GetFirstChild();
				child.Visibility = Visibility.Visible;
				if (newValue.GetType() == _cachedContent.GetType())
				{
					fInvalidationNeeded = false;
				}

				_cachedContent = null;
			}
			else
			{
				if (newValue.GetType() == oldValue?.GetType())
				{
					fInvalidationNeeded = false;
				}
			}
			// If we got all the way here, this means that there are no content transitions
			// that participate in unloading. However, if we do have other content transitions,
			// one of these might participate in loading. If we are not invalidating our visual
			// child, though, we won't create a new tree and instead we'll just reuse the old one
			// which has already being loaded, so the load trigger will never fire. Given that, we
			// will lie to the layout manager and make it look like the visual child just entered
			// during this tick. When CTransition::OnLayoutChanged gets called as part of the arrange
			// pass, we will detect this and process the load trigger for this element.
			//if (!fInvalidationNeeded && ContentTransitions is not null && ContentTransitions.Count > 0)
			//{
			//	var child = this.GetFirstChild();
			//	if (child is not null)
			//	{
			//		var layoutManager = VisualTree.GetLayoutManagerForElement(child);
			//		if (layoutManager is not null)
			//		{
			//			child.m_enteredTreeCounter = layoutManager.GetLayoutCounter();
			//		}
			//	}
			//}
		}

		this.Invalidate(fInvalidationNeeded);

		TryRegisterNativeElement(oldValue, newValue);
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
				if (content is View ui)
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
		ContentTemplateRoot = VisualTreeHelper.GetChild(this, 0) as View;
		_dataContextInvalid = false;
		_inOnApplyTemplate = false;
	}

	// Fetches the child TextBlock of the default template if we are using the default template; null otherwise.
	private TextBlock GetTextBlockChildOfDefaultTemplate(bool allowNullContent)
	{
		var content = Content;
		// Make sure we are indeed using the default template (i.e. content is non-null and is not a UIElement).
		if (allowNullContent || (content is not null && content is not View))
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

			// Clear cached reference to the old content value.
			_cachedContent = null;

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

	private bool ParticipatesInUnloadingContentTransition()
	{
		//var contentTransitions = ContentTransitions;
		//if (contentTransitions is not null && contentTransitions.Count > 0)
		//{
		//	foreach (var transition in contentTransitions)
		//	{
		//		bool participate = transition.ParticipateInTransitions(this, TransitionTrigger.Unload);
		//		if (participate)
		//		{
		//			return true;
		//		}
		//	}
		//}
		return false;
	}
}
#endif
