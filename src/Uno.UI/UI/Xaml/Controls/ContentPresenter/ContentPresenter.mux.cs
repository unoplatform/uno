// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentPresenter.cpp, tag winui3/release/1.8.2, commit bac7a9c33
// BindDefaultTextBlock comes from ContentPresenter_Partial.cpp; it is kept next to its only caller.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;

using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	/// <remarks>
	/// Internal in WinUI too - it is not part of the public API surface. It exists so the presenter can
	/// template-bind the template its templated ContentControl resolved.
	/// </remarks>
	internal DataTemplate SelectedContentTemplate
	{
		get => (DataTemplate)GetValue(SelectedContentTemplateProperty);
		set => SetValue(SelectedContentTemplateProperty, value);
	}

	internal static DependencyProperty SelectedContentTemplateProperty { get; } = DependencyProperty.Register(
		nameof(SelectedContentTemplate),
		typeof(DataTemplate),
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
						m_cachedContent = oldValue;
						child.Visibility = Visibility.Collapsed;
						fInvalidationNeeded = false;
					}
				}
				else
				{
					fInvalidationNeeded = false;
				}
			}
			else if (oldValue is null && newValue is not null && m_cachedContent is not null)
			{
				var child = GetFirstChild();
				child.Visibility = Visibility.Visible;
				if (newValue.GetType() == m_cachedContent.GetType())
				{
					fInvalidationNeeded = false;
				}

				m_cachedContent = null;
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

		if (m_bInOnApplyTemplate)
		{
			base.ApplyTemplate(out addedVisuals);
			SubscribeToTemplateUpdates();

			goto Cleanup;
		}

		// Applying the template will not delete existing visuals. This will be done conditionally
		// when the template is invalidated.
		if (!HasTemplateChild())
		{
			var templatedParent = GetTemplatedParent() as ContentControl;
			if (templatedParent is not null)
			{
				bool needsRefresh = false;
				DependencyProperty dpTarget = ContentPresenter.SelectedContentTemplateProperty;
				if (this.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.SelectedContentTemplateProperty;
					SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				dpTarget = ContentPresenter.ContentTemplateProperty;
				if (this.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.ContentTemplateProperty;
					SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				dpTarget = ContentPresenter.ContentProperty;
				if (this.GetCurrentHighestValuePrecedence(dpTarget) == DependencyPropertyValuePrecedences.DefaultValue && !IsPropertyTemplateBound(dpTarget))
				{
					DependencyProperty dpSource = ContentControl.ContentProperty;
					SetTemplateBinding(dpTarget, dpSource);
					needsRefresh = true;
				}

				// Setting up the binding doesn't get you the values.  We need to call refresh to get the latest value
				// for m_pContentTemplate, SelectedContentTemplate and/or m_pContent for the tests below.
				if (needsRefresh)
				{
					//templatedParent.RefreshTemplateBindings(TemplateBindingsRefreshType.All);
				}
			}

			m_bInOnApplyTemplate = true;
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
					// TODO Uno: WinUI calls AddTemplateChild(pUI) here, and fails with E_INVALIDARG when the
					// element is already associated with another parent and does not allow multiple association.
					// Uno has neither AddTemplateChild nor IsAssociated / DoesAllowMultipleAssociation.
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
						//if (this.GetCurrentHighestValuePrecedence(ContentPresenter.OpticalMarginAlignmentProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = OpticalMarginAlignment; ;
						//	if (tempValue != OpticalMarginAlignment.None)
						//	{
						//		textBlockChildOfDefaultTemplate.OpticalMarginAlignment = tempValue;
						//	}
						//}
						//if (this.GetCurrentHighestValuePrecedence(ContentPresenter.TextLineBoundsProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = TextLineBounds;
						//	if (tempValue != TextLineBounds.Full)
						//	{
						//		textBlockChildOfDefaultTemplate.TextLineBounds = tempValue;
						//	}
						//}
						if (this.GetCurrentHighestValuePrecedence(ContentPresenter.TextWrappingProperty) == DependencyPropertyValuePrecedences.Local)
						{
							var tempValue = TextWrapping;
							if (tempValue != TextWrapping.NoWrap)
							{
								textBlockChildOfDefaultTemplate.TextWrapping = tempValue;
							}
						}
						//if (this.GetCurrentHighestValuePrecedence(ContentPresenter.LineStackingStrategyProperty) == DependencyPropertyValuePrecedences.Local)
						//{
						//	var tempValue = LineStackingStrategy;
						//	if (tempValue != LineStackingStrategy.MaxHeight)
						//	{
						//		textBlockChildOfDefaultTemplate.LineStackingStrategy = tempValue;
						//	}
						//}
						if (this.GetCurrentHighestValuePrecedence(ContentPresenter.MaxLinesProperty) == DependencyPropertyValuePrecedences.Local)
						{
							var tempValue = MaxLines;
							if (tempValue != 0)
							{
								textBlockChildOfDefaultTemplate.MaxLines = tempValue;
							}
						}
						//if (this.GetCurrentHighestValuePrecedence(ContentPresenter.LineHeightProperty) == DependencyPropertyValuePrecedences.Local)
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

			addedVisuals = GetFirstChildNoAddRef() is not null;
			SubscribeToTemplateUpdates();
		}
		else if (m_bDataContextInvalid)
		{
			TrySetDataContextFromContent(Content);
		}

	Cleanup:
		// Uno-specific
		ContentTemplateRoot = VisualTreeHelper.GetChild(this, 0) as View;
		m_bDataContextInvalid = false;
		m_bInOnApplyTemplate = false;
	}

	// Uno specific: keeps the materialized content in sync when a DataTemplate is hot-reloaded.
	private void SubscribeToTemplateUpdates()
	{
		if (TemplateManager.IsDataTemplateDynamicUpdateEnabled)
		{
			TemplateUpdateSubscription.Attach(this, GetTemplate() as DataTemplate, OnCurrentTemplateUpdated);
		}
	}

	private void OnCurrentTemplateUpdated() => Invalidate(clearChildren: true);

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
			m_cachedContent = null;

			IsUsingDefaultTemplate = false;
		}
		else
		{
			m_bDataContextInvalid = true;
		}

		InvalidateMeasure();
	}

	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

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

	internal TextBlock CreateDefaultContent()
	{
		// Uno specific: ImplicitTextBlock rather than TextBlock, so the generated text neither picks up
		// an implicit TextBlock style nor shows up as a tab stop or a separate UIA element.
		var textBlock = new ImplicitTextBlock(this);
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
		// WinUI binds Text to the DataContext, which SetDataContext has just filled with Content:
		//   var binding = new Binding() { Mode = BindingMode.OneWay };
		// Uno skips that push when Content itself comes from a source-less binding (see
		// TrySetDataContextFromContent), so bind to the presenter's Content directly - the same value
		// whenever the push does happen, and the right one when it does not.
		var binding = new Binding(nameof(Content))
		{
			Mode = BindingMode.OneWay,
			RelativeSource = RelativeSource.TemplatedParent,
		};
		textBlock.SetBinding(TextBlock.TextProperty, binding);
	}

	// MUX Reference: CContentPresenter::OnPropertyChanged forwards these to the TextBlock of the default
	// template, so a change after the template was expanded still reaches it.
	partial void OnTextWrappingChangedPartial()
	{
		if (GetTextBlockChildOfDefaultTemplate(allowNullContent: false) is { } textBlock)
		{
			textBlock.TextWrapping = TextWrapping;
		}
	}

	partial void OnMaxLinesChangedPartial()
	{
		if (GetTextBlockChildOfDefaultTemplate(allowNullContent: false) is { } textBlock)
		{
			textBlock.MaxLines = MaxLines;
		}
	}

	// Uno specific: WinUI's ContentPresenter has no TextTrimming or TextAlignment.
	partial void OnTextTrimmingChangedPartial()
	{
		if (GetTextBlockChildOfDefaultTemplate(allowNullContent: false) is { } textBlock)
		{
			textBlock.TextTrimming = TextTrimming;
		}
	}

	partial void OnTextAlignmentChangedPartial()
	{
		if (GetTextBlockChildOfDefaultTemplate(allowNullContent: false) is { } textBlock)
		{
			textBlock.TextAlignment = TextAlignment;
		}
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
