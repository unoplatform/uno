#if UNO_HAS_ENHANCED_LIFECYCLE
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml;

internal struct DataContextChangedParams
{
	public DataContextChangedParams(FrameworkElement originalSource, DataContextChangedReason reason)
	{
		OriginalSource = originalSource;
		DataContextChangedReason = reason;
		NewDataContext = null;
		ResolvedNewDataContext = false;
	}

	public DataContextChangedParams(FrameworkElement originalSource, DataContextChangedReason reason, object newDataContext)
	{
		OriginalSource = originalSource;
		DataContextChangedReason = reason;
		NewDataContext = newDataContext;
		ResolvedNewDataContext = true;
	}

	public FrameworkElement OriginalSource { get; }
	public DataContextChangedReason DataContextChangedReason { get; }
	public object NewDataContext { get; set; }
	public bool ResolvedNewDataContext { get; set; }
}
#endif
