#if UNO_HAS_ENHANCED_LIFECYCLE
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private bool _inOnApplyTemplate;
	private bool _dataContextInvalid = true;
	private const FrameworkPropertyMetadataOptions ContentPropertyOptions = FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure;

	private protected override FrameworkTemplate GetTemplate() => this.ResolveContentTemplate();

	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		addedVisuals = false;

		if (_inOnApplyTemplate)
		{
			base.ApplyTemplate(out addedVisuals);
			_dataTemplateUsedLastUpdate = GetTemplate();
			return;
		}

		if (VisualTreeHelper.GetChildrenCount(this) == 0)
		{
			_inOnApplyTemplate = true;
			TrySetDataContextFromContent(Content);

			base.ApplyTemplate(out addedVisuals);
			_dataTemplateUsedLastUpdate = GetTemplate();
		}
		else if (_dataContextInvalid)
		{
			TrySetDataContextFromContent(Content);
		}

		_dataContextInvalid = false;
		_inOnApplyTemplate = false;
	}

	private void Invalidate(bool clearChildren)
	{
		if (clearChildren)
		{
			ClearChildren();
		}
		else
		{
			_dataContextInvalid = true;
		}

		InvalidateMeasure();
	}

	internal override void Enter(EnterParams @params, int depth)
	{
		base.Enter(@params, depth);

		//if (ContentTemplateRoot == null)
		//{
		//	SetUpdateTemplate();
		//}

		// When the control is loaded, set the TemplatedParent
		// as it may have been reset during the last unload.
		//SynchronizeContentTemplatedParent();

#if !UNO_HAS_BORDER_VISUAL
		UpdateBorder();
#endif
	}

	public void UpdateContentTemplateRoot()
	{
	}
}
#endif
