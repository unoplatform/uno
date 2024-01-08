using Java.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Controls;
using Java.Lang.Reflect;
using Microsoft.UI.Xaml;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a helper class to enable tools like Stetho to provide meaningful
	/// information about the .NET side of the controls.
	/// </summary>
	public class BinderDetails : Java.Lang.Object
	{
		private const string DataContextNativeField = "dataContext";
		private const string TemplatedParentNativeField = "templatedParent";
		private const string DependencyPropertiesNativeField = "dependencyProperties";

		private readonly DependencyProperty[] _props;
		private readonly DependencyObjectStore _store;
		private readonly DependencyObject _owner;

		private Field _dataContextNative;
		private Field _dependencyPropertiesNative;

		/// <summary>
		/// Determines if the binder details class monitoring is enabled by default.
		/// </summary>
		public static bool IsBinderDetailsEnabled { get; set; }

		public BinderDetails(DependencyObject owner)
		{
			this._store = ((IDependencyObjectStoreProvider)owner).Store;
			this._owner = owner;

			_store.RegisterPropertyChangedCallback(_store.DataContextProperty, Binder_DataContextChanged);

			_props = DependencyProperty.GetPropertiesForType(owner.GetType());

			DependencyObjectExtensions
				.RegisterDisposablePropertyChangedCallback(_owner, (instance, p, e) => UpdateProperties());
		}

		private void UpdateProperties()
		{
			if (_dependencyPropertiesNative == null)
			{
				_dependencyPropertiesNative = this.Class.GetField(DependencyPropertiesNativeField);
			}

			List<Java.Lang.Object> properties = new List<Java.Lang.Object>();

			foreach (var prop in _props)
			{
				var value = _owner.GetValue(prop);

				if (value is Java.Lang.Object)
				{
					properties.Add(new Android.Util.Pair(prop.Name, value as Java.Lang.Object));
				}
				else
				{
					properties.Add(prop.Name + ": " + (value?.ToString() ?? "<null>"));
				}
			}

			// Due to the nature of the ExportField behavior, it is required to update the variable
			// manually through reflection once the native class has been created.
			_dependencyPropertiesNative.Set(this, properties.ToArray());
		}

		private void Binder_DataContextChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (_dataContextNative == null)
			{
				_dataContextNative = this.Class.GetField(DataContextNativeField);
			}

			_dataContextNative.Set(this, _store.GetValue(_store.DataContextProperty)?.ToString() ?? "<null>");
		}

		[Java.Interop.ExportField(DataContextNativeField)]
		public string GetDataContext()
		{
			return null;
		}

		[Java.Interop.ExportField(TemplatedParentNativeField)]
		public string GetTemplatedParent()
		{
			return null;
		}

		[Java.Interop.ExportField(DependencyPropertiesNativeField)]
		public Java.Lang.Object[] GetDependencyPropertiesNativeField()
		{
			return null;
		}

		public override string ToString()
		{
			return _store.GetValue(_store.DataContextProperty)?.ToString();
		}
	}
}
