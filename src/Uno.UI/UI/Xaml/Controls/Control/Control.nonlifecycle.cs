#if !UNO_HAS_ENHANCED_LIFECYCLE
namespace Windows.UI.Xaml.Controls;

partial class Control
{
	private bool _updateTemplate;

	/// <summary>
	/// Will be set to Template when it is applied
	/// </summary>
	private ControlTemplate _controlTemplateUsedLastUpdate;

	private bool _applyTemplateShouldBeInvoked;

	/// <summary>
	/// Defines a method that will request the update of the control's template and request layout update.
	/// </summary>
	/// <param name="forceUpdate">If true, forces an update even if the control has no parent.</param>
	internal void SetUpdateControlTemplate(bool forceUpdate = false)
	{
		if (
			!Uno.UI.FeatureConfiguration.Control.UseLegacyLazyApplyTemplate ||
			forceUpdate ||
			this.HasParent() ||
			CanCreateTemplateWithoutParent
		)
		{
			UpdateTemplate();
			this.InvalidateMeasure();
		}
	}

	internal void TryCallOnApplyTemplate()
	{
		if (_applyTemplateShouldBeInvoked)
		{
			_applyTemplateShouldBeInvoked = false;
			OnApplyTemplate();
		}
	}

	/// <summary>
	/// Loads the relevant control template so that its parts can be referenced.
	/// </summary>
	/// <returns>A value that indicates whether the visual tree was rebuilt by this call. True if the tree was rebuilt; false if the previous visual tree was retained.</returns>
	public bool ApplyTemplate()
	{
		var currentTemplateRoot = _templatedRoot;
		SetUpdateControlTemplate(forceUpdate: true);

		// When .ApplyTemplate is called manually, we should not defer the call to OnApplyTemplate
		TryCallOnApplyTemplate();

		return currentTemplateRoot != _templatedRoot;
	}

	private void UpdateTemplate()
	{
		// If TemplatedRoot is null, it must be updated even if the templates haven't changed
		if (TemplatedRoot == null)
		{
			_controlTemplateUsedLastUpdate = null;
		}

		if (_updateTemplate && !object.Equals(Template, _controlTemplateUsedLastUpdate))
		{
			_controlTemplateUsedLastUpdate = Template;

			if (Template != null)
			{
				TemplatedRoot = Template.LoadContentCached(templatedParent: this);
			}
			else
			{
				TemplatedRoot = null;
			}

			_updateTemplate = false;
		}
	}
}
#endif
