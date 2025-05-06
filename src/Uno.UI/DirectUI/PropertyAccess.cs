using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CClassInfo = System.Type; // fixme@xy: is that the equivalent or suitable here

namespace DirectUI;

//  public abstract:
//      Defines the interfaces for accessing a single property
//      public abstracts away the differences between the types of properties
//      supported.

internal abstract class PropertyAccess // : public ctl::WeakReferenceSource
{
	//public:
	public abstract object GetValue();
	public abstract void SetValue(object pValue);
	public abstract CClassInfo GetType2();
	public abstract object GetSource();
	public abstract void SetSource(object pSource, bool fListenToChanges);
	public abstract CClassInfo GetSourceType();
	public abstract void DisconnectEventHandlers();
	public abstract bool TryReconnect(object pSource, bool fListenToChanges, out CClassInfo pResolvedType);
	public abstract bool IsConnected();
}

internal interface IPropertyAccessHost
{
	void SourceChanged();
	string GetPropertyName();
}
internal interface IIndexedPropertyAccessHost : IPropertyAccessHost
{
	string GetIndexedPropertyName();
};
