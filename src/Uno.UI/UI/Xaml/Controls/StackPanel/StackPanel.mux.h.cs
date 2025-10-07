// MUX Reference StackPanel.h

namespace Microsoft.UI.Xaml.Controls;

partial class StackPanel
{
	private float[] m_pIrregularSnapPointKeys;                  // Unique identifiers for irregular snap points (independent of snap point alignment)
	private double m_regularSnapPointKey = -1.0;                       // Unique identifier for regular snap points (independent of snap point alignment)
	private double m_lowerMarginSnapPointKey;                   // Top/left margin dimension used to compute regular and irregular snap points
	private double m_upperMarginSnapPointKey;                   // Bottom/right margin dimension used to compute regular and irregular snap points
	private bool m_bNotifyHorizontalSnapPointsChanges;      // True when NotifySnapPointsChanged needs to be called when horizontal snap points change
	private bool m_bNotifyVerticalSnapPointsChanges;        // True when NotifySnapPointsChanged needs to be called when vertical snap points change
	private bool m_bNotifiedHorizontalSnapPointsChanges;    // True when NotifySnapPointsChanged was already called once and horizontal snap points have not been accessed since
	private bool m_bNotifiedVerticalSnapPointsChanges;      // True when NotifySnapPointsChanged was already called once and vertical snap points have not been accessed since
	private bool m_bAreSnapPointsKeysHorizontal;            // True when the snap point keys are for horizontal snap points
	internal bool m_bAreScrollSnapPointsRegular;                 // Backing field for the AreScrollSnapPointsRegular dependency property.
	private int m_cIrregularSnapPointKeys = -1;
}
