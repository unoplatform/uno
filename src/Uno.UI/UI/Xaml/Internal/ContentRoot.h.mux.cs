using System.Collections.Generic;
using Uno.UI.DataBinding;

namespace Uno.UI.Xaml.Core;

internal record KACollectionAndRefCountPair(ManagedWeakReference KeyboardAcceleratorCollectionWeak, int Count);

internal class VectorOfKACollectionAndRefCountPair : List<KACollectionAndRefCountPair>
{
}
