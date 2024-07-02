#nullable enable

#if !NETFX_CORE
using Uno.UI.DataBinding;
using Uno.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Uno.UI.DataBinding
{
	[DebuggerDisplay("Path={_path} DataContext={_dataContextWeakStorage?.Target}")]
	internal partial class BindingPath : IDisposable
	{
		private static List<IPropertyChangedRegistrationHandler> _propertyChangedHandlers = new List<IPropertyChangedRegistrationHandler>(2);
		private readonly string _path;

		// A BindingPath is a path to a property starting from the DataContext.
		// This is represented internally as a linked list (BindingItem) where the tail of the path is the target value.
		// _chain is the "head" of the list. _value is the tail i.e. the target.
		// Each node in the linked list will have a different DataContext to reflect its nesting level.
		// E.g. BindingPath("Level1.Level2.Level3") will be represented as
		//  BindingItem("Level1")   DataContext = the actual DC set on the BindingPath (i.e. BindingPath.DataContext)    <---- _chain
		//        |
		//        |
		//        v
		//  BindingItem("Level2")   DataContext = BindingPath.DataContext.Level1
		//        |
		//        |
		//        v
		//  BindingItem("Level3")   DataContext = BindingPath.DataContext.Level1.Level2                                  <---- _value

		private BindingItem? _chain;
		private BindingItem? _value;
		private ManagedWeakReference? _dataContextWeakStorage;
		private bool _disposed;

		/// <summary>
		/// Defines a interface that will allow for the creation of a registration on the specified dataContext
		/// for the specified propertyName.
		/// </summary>
		public interface IPropertyChangedRegistrationHandler
		{
			/// <summary>
			/// Registere a new <see cref="IPropertyChangedValueHandler"/> for the specified property
			/// </summary>
			/// <param name="dataContext">The datacontext to use</param>
			/// <param name="propertyName">The property in the datacontext</param>
			/// <param name="onNewValue">The action to execute when a new value is raised</param>
			/// <returns>A disposable that will cleanup resources.</returns>
			IDisposable? Register(ManagedWeakReference dataContext, string propertyName, IPropertyChangedValueHandler onNewValue);
		}

		/// <summary>
		/// PropertyChanged value handler.
		/// </summary>
		/// <remarks>
		/// This is an interface to avoid the use of delegates, and delegates type conversion as
		/// there are two available signatures. (<see cref="Action"/> and <see cref="DependencyPropertyChangedCallback"/>)
		/// </remarks>
		public interface IPropertyChangedValueHandler
		{
			/// <summary>
			/// Process a property changed using the <see cref="DependencyPropertyChangedCallback"/> signature.
			/// </summary>
			void NewValue(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args);

			/// <summary>
			/// Processa a property changed using <see cref="Action"/>-like signature (e.g. for <see cref="BindingItem"/>)
			/// </summary>
			void NewValue();
		}

		public BindingExpression? Expression { get; set; }

		static BindingPath()
		{
			RegisterPropertyChangedRegistrationHandler(new BindingPathPropertyChangedRegistrationHandler());
		}

		/// <summary>
		/// Creates a BindingPath for the specified path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fallbackValue">Provides the fallback value to apply when the source is invalid.</param>
		public BindingPath(string path, object? fallbackValue) :
			this(path, fallbackValue, null, false)
		{
		}

		/// <summary>
		/// Creates a BindingPath for the specified path using a specified DependencyProperty precedence.
		/// </summary>
		/// <param name="path">The path to the property</param>
		/// <param name="fallbackValue">Provides the fallback value to apply when the source is invalid.</param>
		/// <param name="precedence">The precedence value to manipulate if the path matches to a DependencyProperty.</param>
		/// <param name="allowPrivateMembers">Allows for the binding engine to include private properties in the lookup</param>
		internal BindingPath(string path, object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
		{
			_path = path ?? "";

			Parse(_path, fallbackValue, precedence, allowPrivateMembers, ref _chain, ref _value);

			if (_value != null)
			{
				_value.Path = this;
			}
		}

		/// <summary>
		/// Returns a chain of items composing the currently databound path.
		/// </summary>
		/// <returns>An enumerable of binding items</returns>
		/// <remarks>
		/// The DataContext and PropertyType of the descriptor may be null
		/// if the binding is incomplete (the DataContext may be null, or the path is invalid)
		/// </remarks>
		public IEnumerable<IBindingItem> GetPathItems()
		{
			return _chain?.Flatten(i => i.Next!) ?? Array.Empty<BindingItem>();
		}

		public (object DataContext, string PropertyName) GetTargetContextAndPropertyName()
		{
			var info = GetPathItems().Last();
			var propertyName = info.PropertyName
				.Split('.').Last()
				.Replace("(", "").Replace(")", "");

			return (info.DataContext, propertyName);
		}

		/// <summary>
		/// Checks the property path for members which may be shared resources (<see cref="Brush"/>es and <see cref="Transform"/>s) and creates a
		/// copy of them if need be (ie if not already copied). Intended to be used prior to animating the targeted property.
		/// </summary>
		internal void CloneShareableObjectsInPath()
		{
			foreach (BindingItem item in GetPathItems())
			{
				if (item.PropertyType == typeof(Brush) || item.PropertyType == typeof(GeneralTransform))
				{
					if (item.Value is IShareableDependencyObject shareable && !shareable.IsClone && item.DataContext is DependencyObject owner)
					{
						var clone = shareable.Clone();

						item.Value = clone;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Registers a property changed registration handler.
		/// </summary>
		/// <param name="handler">The handled to be called when a property needs to be observed.</param>
		/// <remarks>This method exists to provide layer separation,
		/// when BindingPath is in the presentation layer, and DependencyProperty is in the (some) Views layer.
		/// </remarks>
		public static void RegisterPropertyChangedRegistrationHandler(IPropertyChangedRegistrationHandler handler)
		{
			_propertyChangedHandlers.Add(handler);
		}

		public string Path
		{
			get
			{
				return _path;
			}
		}

		/// <summary>
		/// Provides the value of the <see cref="Path"/> using the
		/// current <see cref="DataContext"/> using the current precedence.
		/// </summary>
		public object? Value
		{
			get
			{
				if (_value != null)
				{
					return _value.Value;
				}
				else
				{
					return DataContext;
				}
			}
			set
			{
				if (!_disposed
					&& _value != null
					&& (
						!_value.IsDependencyProperty

						// Don't get the source value if we're not accessing a dependency property.
						// WinUI does not read the property value before setting the value for a
						// non-dependency property source.
						|| DependencyObjectStore.AreDifferent(value, _value.GetPrecedenceSpecificValue())
					))
				{
					_value.Value = value;
				}
			}
		}

		/// <summary>
		/// Name of the leaf property in the path.
		/// </summary>
		internal string? LeafPropertyName => _value?.PropertyName;

		/// <summary>
		/// Gets the value of the DependencyProperty with a
		/// precedence immediately below the one specified at the creation
		/// of the BindingPath.
		/// </summary>
		/// <returns>The lower precedence value</returns>
		public object? GetSubstituteValue()
		{
			if (_value != null)
			{
				return _value.GetSubstituteValue();
			}
			else
			{
				return DataContext;
			}
		}

		/// <summary>
		/// Sets the value of the <see cref="DependencyPropertyValuePrecedences.Local"/>
		/// </summary>
		/// <param name="value">The value to set</param>
		internal void SetLocalValue(object value)
		{
			if (!_disposed)
			{
				_value?.SetLocalValue(value);
			}
		}

		/// <summary>
		/// Clears the value of the current precedence.
		/// </summary>
		/// <remarks>After this call, the value returned
		/// by <see cref="Value"/> will be of the next available
		///  precedence.</remarks>
		public void ClearValue()
		{
			if (!_disposed)
			{
				_value?.ClearValue();
			}
		}

		public Type? ValueType => _value?.PropertyType;

		internal object? DataItem => _value?.DataContext;

		public object? DataContext
		{
			get => _dataContextWeakStorage?.Target;
			set => SetWeakDataContext(Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, value));
		}

		internal void SetWeakDataContext(ManagedWeakReference weakDataContext)
		{
			if (!_disposed)
			{
				var previousStorage = _dataContextWeakStorage;

				_dataContextWeakStorage = weakDataContext;
				OnDataContextChanged();

				// Return the reference to the pool after it's been released from the underlying BindingItem instances.
				// Failing to do so makes the reference change without the bindings knowing about it,
				// making the reference comparison always equal.
				Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(this, previousStorage);
			}
		}

		private void OnDataContextChanged()
		{
			if (_chain != null)
			{
				_chain.SetWeakDataContext(_dataContextWeakStorage);
			}
			else
			{
				// This is an empty path binding, raise the current value as changed.
				Expression?.OnValueChanged(Value);
			}
		}

		internal void OnValueChanged(object? o)
		{
			Expression?.OnValueChanged(o);
		}

		~BindingPath()
		{
			Dispose(false);
		}

		public bool PrefixOfOrEqualTo(BindingPath? that)
		{
			if (that == null || DependencyObjectStore.AreDifferent(this.DataContext, that.DataContext))
			{
				return false;
			}

			var currentLeft = this._chain;
			var currentRight = that._chain;
			while (currentLeft != null)
			{
				if (!currentLeft.Equals(currentRight))
				{
					return false;
				}
				currentLeft = currentLeft.Next;
				currentRight = currentRight.Next;
			}

			return true;
		}

		// We define Equals to be based on Path, so `RuntimeHelpers.GetHashCode(path)` doesn't work,
		// since we need the hashes must match if the two BindingPaths are Equal.
		public override int GetHashCode() => RuntimeHelpers.GetHashCode(Path);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			_disposed = true;

			if (disposing)
			{
				_value.SafeDispose();
				_chain.SafeDispose();
			}
		}

		/// <summary>
		/// Property changed registration handler for BindingPath.
		/// </summary>
		private class BindingPathPropertyChangedRegistrationHandler : IPropertyChangedRegistrationHandler
		{
			public IDisposable? Register(ManagedWeakReference dataContext, string propertyName, IPropertyChangedValueHandler onNewValue)
				=> SubscribeToNotifyPropertyChanged(dataContext, propertyName, onNewValue);
		}

		#region Miscs helpers
		/// <summary>
		/// Parse the given string path in parts and create the linked list of binding items in head and tail
		/// </summary>
		private static void Parse(
			string path,
			object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers,
			ref BindingItem? head, ref BindingItem? tail)
		{
			var propertyLength = 0;
			bool isInAttachedProperty = false, isInItemIndex = false;
			for (var i = path.Length - 1; i >= 0; i--)
			{
				var c = path[i];
				switch (c)
				{
					case ')':
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInAttachedProperty = true;
						propertyLength = 0;
						break;

					case '(' when isInAttachedProperty:
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInAttachedProperty = false;
						propertyLength = 0;
						break;

					case ']':
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInItemIndex = true;
						propertyLength = 1; // We include the brackets for itemIndex properties
						break;

					case '[' when isInItemIndex:
						// Note: We use 'start = i' and '++propertyLength' here for 'TryPrependItem' as we include the brackets for itemIndex properties
						TryPrependItem(path, i, ++propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInItemIndex = false;
						propertyLength = 0;
						break;

					case '.' when !isInAttachedProperty:
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						propertyLength = 0;
						break;

					default:
						if (propertyLength > 0 || !char.IsWhiteSpace(c)) // i.e. TrimEnd
						{
							propertyLength++;
						}
						break;
				}
			}

			TryPrependItem(path, 0, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
		}

		/// <summary>
		/// Prepends an item in the binding linked list if the string defined between by start and length is valid.
		/// </summary>
		private static void TryPrependItem(
			string path, int start, int length,
			object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers,
			ref BindingItem? head, ref BindingItem? tail)
		{
			if (length <= 0)
			{
				return;
			}

			// Trim start (trim end is achieved in the main Parse loop)
			var c = path[start];
			while (char.IsWhiteSpace(c) && length > 0)
			{
				start++;
				length--;
				c = path[start];
			}

			if (length <= 0)
			{
				return;
			}

			var itemPath = path.Substring(start, length);
			var item = new BindingItem(head, itemPath, precedence, allowPrivateMembers);

			head = item;
			tail ??= item;
		}

		private static IDisposable? SubscribeToNotifyCollectionChanged(BindingItem bindingItem)
		{
			if (!bindingItem.PropertyType.Is(typeof(INotifyCollectionChanged)) ||
				bindingItem.Next?.PropertyName.StartsWith('[') != true)
			{
				return null;
			}

			if ((INotifyCollectionChanged?)bindingItem.Value is not { } notify)
			{
				return null;
			}

			NotifyCollectionChangedEventHandler handler = (s, args) =>
			{
				BindingItem tail = bindingItem;
				while (tail.Next != null)
				{
					tail = tail.Next;
				}
				tail.Path?.OnValueChanged(bindingItem.Value);
			};

			notify.CollectionChanged += handler;

			return Disposable.Create(() =>
			{
				notify.CollectionChanged -= handler;
			});
		}

		/// <summary>
		/// Subscribes for updates to the INotifyPropertyChanged interface.
		/// </summary>
		private static IDisposable? SubscribeToNotifyPropertyChanged(ManagedWeakReference dataContextReference, string propertyName, IPropertyChangedValueHandler propertyChangedValueHandler)
		{
			// Attach to the Notify property changed events
			var notify = dataContextReference.Target as System.ComponentModel.INotifyPropertyChanged;

			if (notify != null)
			{
				if (propertyName.Length != 0 && propertyName[0] == '[')
				{
					propertyName = "Item" + propertyName;
				}

				var newValueActionWeak = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(null, propertyChangedValueHandler);

				System.ComponentModel.PropertyChangedEventHandler handler = (s, args) =>
				{
					if (args.PropertyName == propertyName || string.IsNullOrEmpty(args.PropertyName))
					{
						if (typeof(BindingPath).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							typeof(BindingPath).Log().Debug($"Property changed for {propertyName} on [{dataContextReference.Target?.GetType()}]");
						}

						if (!newValueActionWeak.IsDisposed
						&& newValueActionWeak.Target is IPropertyChangedValueHandler handler)
						{
							handler.NewValue();
						}
					}
				};

				notify.PropertyChanged += handler;

				return Disposable.Create(() =>
				{
					// This weak reference ensure that the closure will not link
					// the caller and the callee, in the same way "newValueActionWeak"
					// does not link the callee to the caller.
					var that = dataContextReference.Target as System.ComponentModel.INotifyPropertyChanged;

					if (that != null)
					{
						that.PropertyChanged -= handler;
					}

					Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(null, newValueActionWeak);
				});
			}

			return null;
		}

		internal bool OnlyLeafNodeNull()
		{
			var tail = _chain;
			while (tail?.Value != null)
			{
				tail = tail.Next;
			}

			return tail?.Value is null && tail?.Next == null;
		}
		#endregion
	}
}
#endif
