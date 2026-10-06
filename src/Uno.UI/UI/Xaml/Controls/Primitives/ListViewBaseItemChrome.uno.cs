#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter
{
	/// <summary>
	/// Stops every running chrome animation and runs its completion action synchronously
	/// (e.g. removes a check box or selection indicator that was fading out).
	/// </summary>
	/// <remarks>Uno-specific: recycled containers leave the tree, so their animations cannot be left to complete.</remarks>
	internal void FlushChromeAnimations()
	{
		ClearAnimation(m_pointerPressedAnimation);

		if (m_reorderHintAnimation.tpStoryboard is not null)
		{
			OnReorderHintReturnCompleted(null, null);
		}

		ClearAnimation(m_dragDropAnimation);

		if (m_multiSelectAnimation.tpStoryboard is not null)
		{
			OnMultiSelectCompleted(null, null);
		}

		if (m_indicatorSelectAnimation.tpStoryboard is not null)
		{
			OnIndicatorSelectCompleted(null, null);
		}

		if (m_selectionIndicatorAnimation.tpStoryboard is not null)
		{
			OnSelectionIndicatorCompleted(null, null);
		}
	}
}
