using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Data;

partial class CollectionViewGroup
{
	private ManagedWeakReference m_spOwnerRef;
	private IObservableVector<object> m_tpObservableItems;
	private object m_tpGroup;
	private object m_tpGroupItems;

	ctl::EventPtr<VectorChangedEventCallback> m_epVectorChangedHandler;
}
