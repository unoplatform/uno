#if !UNO_HAS_ENHANCED_LIFECYCLE
namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter : IFrameworkTemplatePoolAware
{
	private bool _firstLoadResetDone;
	private bool _appliedTemplate;
	private const FrameworkPropertyMetadataOptions ContentPropertyOptions = FrameworkPropertyMetadataOptions.AffectsMeasure;

	void IFrameworkTemplatePoolAware.OnTemplateRecycled()
	{
		// This needs to be cleared on recycle, to prevent
		// SetUpdateTemplate from being skipped in OnLoaded.
		_firstLoadResetDone = false;
	}

	internal DataTemplate SelectedContentTemplate => _dataTemplateUsedLastUpdate;

	protected virtual void OnContentChanged(object oldValue, object newValue)
	{
		if (oldValue is View || newValue is View)
		{
			// Make sure not to reuse the previous Content as a ContentTemplateRoot (i.e., in case there's no data template)
			// If setting Content to a new View, recreate the template
			ContentTemplateRoot = null;
		}

		TrySetDataContextFromContent(newValue);

		TryRegisterNativeElement(oldValue, newValue);

		SetUpdateTemplate();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Applying the template will not delete existing visuals. This will be done conditionally
		// when the template is invalidated.
		// Uno specific: since we don't call this early enough, we have to comment out the condition
		// if (GetChildren().Count == 0)
		{
			ContentControl pTemplatedParent = GetTemplatedParent() as ContentControl;

			// Only ContentControl has the two properties below.  Other parents would just fail to bind since they don't have these
			// two content related properties.
			if (pTemplatedParent != null
#if ANDROID || __IOS__
				&& this is not NativeCommandBarPresenter // Uno specific: NativeCommandBarPresenter breaks if you inherit from the TP
#endif
				)
			{
				// bool needsRefresh = false;
				DependencyProperty pdpTarget;

				// By default Content and ContentTemplate are template are bound.
				// If no template binding exists already then hook them up now
				// pdpTarget = GetPropertyByIndexInline(KnownPropertyIndex::ContentPresenter_SelectedContentTemplate);
				// IFCEXPECT(pdpTarget);
				// if (IsPropertyDefault(pdpTarget) && !IsPropertyTemplateBound(pdpTarget))
				// {
				// 	const CDependencyProperty* pdpSource = pTemplatedParent->GetPropertyByIndexInline(KnownPropertyIndex::ContentControl_SelectedContentTemplate);
				// 	IFCEXPECT(pdpSource);
				//
				// 	IFC(SetTemplateBinding(pdpTarget, pdpSource));
				// 	needsRefresh = true;
				// }

				// UNO Specific: SelectedContentTemplate is not implemented, we hook ContentTemplateSelector instead
				pdpTarget = ContentPresenter.ContentTemplateSelectorProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				var store = ((IDependencyObjectStoreProvider)this).Store;
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentTemplateSelectorProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				pdpTarget = ContentPresenter.ContentTemplateProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentTemplateProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				pdpTarget = ContentPresenter.ContentProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				// Uno specific: uno bindings don't work this way
				// Setting up the binding doesn't get you the values.  We need to call refresh to get the latest value
				// for m_pContentTemplate, SelectedContentTemplate and/or m_pContent for the tests below.
				// if (needsRefresh)
				// {
				// 	IFC(pTemplatedParent->RefreshTemplateBindings(TemplateBindingsRefreshType::All));
				// }
			}
		}
	}

	private bool ResetDataContextOnFirstLoad()
	{
		if (!_firstLoadResetDone)
		{
			_firstLoadResetDone = true;

			// This test avoids the ContentPresenter from resetting
			// the DataContext to null (or the inherited value) and then back to
			// the content and have two-way bindings propagating the null value
			// back to the source.
			if (!ReferenceEquals(DataContext, Content))
			{
				// On first load UWP clears the local value of a ContentPresenter.
				// The reason for this behavior is unknown.
				this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);

				TrySetDataContextFromContent(Content);
			}

			return true;
		}

		return false;
	}

	private void SetUpdateTemplate()
	{
		UpdateContentTemplateRoot();
		SetUpdateTemplatePartial();
	}

	partial void SetUpdateTemplatePartial();

	public void UpdateContentTemplateRoot()
	{
		if (Visibility == Visibility.Collapsed)
		{
			return;
		}

		//If ContentTemplateRoot is null, it must be updated even if the templates haven't changed
		if (ContentTemplateRoot == null)
		{
			_dataTemplateUsedLastUpdate = null;
		}

		//ContentTemplate/ContentTemplateSelector will only be applied to a control with no Template, normally the innermost element
		var dataTemplate = this.ResolveContentTemplate();

		//Only apply template if it has changed
		if (!object.Equals(dataTemplate, _dataTemplateUsedLastUpdate))
		{
			_dataTemplateUsedLastUpdate = dataTemplate;
			ContentTemplateRoot = dataTemplate?.LoadContentCached() ?? Content as View;
			if (ContentTemplateRoot != null)
			{
				IsUsingDefaultTemplate = false;
			}
		}

		if (Content != null
			&& !(Content is View)
			&& ContentTemplateRoot == null
		)
		{
			// Use basic default root for non-View Content if no template is supplied
			SetContentTemplateRootToPlaceholder();
		}

		if (ContentTemplateRoot == null && Content is View contentView && dataTemplate == null)
		{
			// No template and Content is a View, set it directly as root
			ContentTemplateRoot = contentView as View;
		}

		IsUsingDefaultTemplate = ContentTemplateRoot is ImplicitTextBlock;
	}

	private void SetContentTemplateRootToPlaceholder()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("No ContentTemplate was specified for {0} and content is not a UIView, defaulting to TextBlock.", GetType().Name);
		}

		var textBlock = new ImplicitTextBlock(this);

		void setBinding(DependencyProperty property, string path)
			=> textBlock.SetBinding(
				property,
				new Binding
				{
					Path = new PropertyPath(path),
					Source = this,
					Mode = BindingMode.OneWay
				}
			);

		if (!IsNativeHost)
		{
			setBinding(TextBlock.TextProperty, nameof(Content));
			setBinding(TextBlock.HorizontalAlignmentProperty, nameof(HorizontalContentAlignment));
			setBinding(TextBlock.VerticalAlignmentProperty, nameof(VerticalContentAlignment));
			setBinding(TextBlock.TextWrappingProperty, nameof(TextWrapping));
			setBinding(TextBlock.MaxLinesProperty, nameof(MaxLines));
			setBinding(TextBlock.TextAlignmentProperty, nameof(TextAlignment));
		}

		ContentTemplateRoot = textBlock;
		IsUsingDefaultTemplate = true;
	}
}
#endif
