using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DirectUI;

internal record struct TypeName(string Name, TypeKind Kind); // wrt::TypeKind
[Flags] internal enum TypeKind { Primitive = 0, Metadata = 1, Custom = 2 }

internal partial class DependencyObjectPropertyAccess
{
	internal static PropertyAccess CreateInstance(PropertyAccessPathStep propertyAccessPathStep, object spSourceForDP, Type pSourceType, DependencyProperty m_pDP, bool fListenToChanges) => throw new NotImplementedException();
	internal static PropertyAccess CreateInstance(PropertyAccessPathStep propertyAccessPathStep, object spSourceForDP, Type pSourceType, bool fListenToChanges) => throw new NotImplementedException();
}
internal partial class PropertyInfoPropertyAccess
{
	//internal static PropertyAccess CreateInstance(PropertyAccessPathStep propertyAccessPathStep, object spInsp, Type pSourceType, bool fListenToChanges) => throw new NotImplementedException();
}
internal partial class MapPropertyAccess
{
	internal static PropertyAccess CreateInstance(PropertyAccessPathStep propertyAccessPathStep, IDictionary<string, object> spMap, bool fListenToChanges) => throw new NotImplementedException();
	internal static PropertyAccess CreateInstance(StringIndexerPathStep stringIndexerPathStep, IDictionary<string, object> spMap, bool m_fListenToChanges) => throw new NotImplementedException();
}

internal partial class PropertyProviderPropertyAccess
{
	internal static PropertyAccess CreateInstance(PropertyAccessPathStep propertyAccessPathStep, ICustomPropertyProvider spPropertyProvider, bool fListenToChanges) => throw new NotImplementedException();
}
internal partial class IndexerPropertyAccess
{
	internal static PropertyAccess CreateInstance(IntIndexerPathStep intIndexerPathStep, ICustomPropertyProvider spProvider, TypeName sTypeName, object spIndex, bool m_fListenToChanges) => throw new NotImplementedException();
	internal static PropertyAccess CreateInstance(StringIndexerPathStep stringIndexerPathStep, ICustomPropertyProvider spPropertyProvider, TypeName sTypeName, object spIndex, bool m_fListenToChanges) => throw new NotImplementedException();
}
internal partial class BindableObservableVectorWrapper
{
	internal static void CreateInstance(IList spBindableVector, INotifyCollectionChanged spINCC, IVector<object> bindableVectorWrapper) => throw new NotImplementedException();
}

internal partial class CCustomProperty
{
#if !!true
	internal CCustomProperty GetXamlPropertyNoRef() => this;
	internal void SetValue(object source, object value) { }
	internal object GetValue(object source) => null;
	internal Type GetPropertyType() => GetType();
	internal bool IsValid() => false;
#endif
}

internal partial class AsdAsdAsd002;
internal partial class AsdAsdAsd002;
internal partial class AsdAsdAsd003;
internal partial class AsdAsdAsd004;
internal partial class AsdAsdAsd005;
internal partial class AsdAsdAsd006;
