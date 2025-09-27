using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;

namespace DirectUI;

// UNO NOTE: due to language limitation,
// INPCListenerBase will be implemented as partial class instead.

/// <summary>
//	Defines the base class for the property access classes that  will listen for INPC changes
/// </summary>
internal partial interface INPCListenerBase // src\dxaml\xcp\dxaml\lib\INPCListenerBase.h
{
	//protected:
	//    INPCListenerBase();
	//    ~INPCListenerBase();

	//protected partial void AddPropertyChangedHandler(object pSource);
	//protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource);
	//protected partial void DisconnectPropertyChangedHandler(object pSource);

	//protected abstract void OnPropertyChanged();
	//protected abstract string GetPropertyName();

	//private:
	//private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs);

	//private:
	//private IDisposable m_epPropertyChangedHandler;
	//private int m_propertyNameLength;
}
partial interface INPCListenerBase // src\dxaml\xcp\dxaml\lib\INPCListenerBase.cpp
{
	//public INPCListenerBase()
	//{
	//	m_propertyNameLength = 0;
	//}

	//protected partial void AddPropertyChangedHandler(object pSource)
	//{
	//	if (m_epPropertyChangedHandler is not null) throw new InvalidOperationException();

	//	UpdatePropertyChangedHandler(null, pSource);
	//}

	//protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource)
	//{
	//	INotifyPropertyChanged spINPC;

	//	string buffer = this.GetPropertyName();
	//	if (string.IsNullOrEmpty(buffer)) throw new InvalidOperationException();
	//	m_propertyNameLength = buffer.Length;

	//	if (pOldSource is { })
	//	{
	//		DisconnectPropertyChangedHandler(pOldSource);
	//	}

	//	if (pNewSource is { })
	//	{
	//		if ((spINPC = pNewSource as INotifyPropertyChanged) is { })
	//		{
	//			spINPC.PropertyChanged += OnPropertyChanged;
	//			m_epPropertyChangedHandler = Disposable.Create(() =>
	//				spINPC.PropertyChanged -= OnPropertyChanged
	//			);
	//			void OnPropertyChanged(object sender, PropertyChangedEventArgs args) => OnPropertyChangedCallback(args);
	//		}
	//	}
	//}
	//protected partial void DisconnectPropertyChangedHandler(object pSource)
	//{
	//	if (m_epPropertyChangedHandler is { })
	//	{
	//		m_epPropertyChangedHandler.Dispose();
	//	}
	//}

	//private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs)
	//{
	//	bool propertyChanged = false;
	//	string strProperty;

	//	strProperty = pArgs.PropertyName;

	//	// If it is not then we need to compare the string vs. the string in the listener to see if the
	//	// change affects the listener
	//	if (strProperty != null)
	//	{
	//		int length = 0;
	//		string buffer = this.GetPropertyName();
	//		(string current, length) = (strProperty, strProperty.Length);
	//		propertyChanged = (length == m_propertyNameLength) && (current == buffer);
	//	}
	//	else
	//	{
	//		// If the property name is null, which means empty, then we want the change
	//		propertyChanged = true;
	//	}

	//	// Notify the class if the property we're listening to changed
	//	if (propertyChanged)
	//	{
	//		OnPropertyChanged();
	//	}
	//}
}
