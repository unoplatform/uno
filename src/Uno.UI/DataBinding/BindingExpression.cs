using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.ComponentModel;
using Uno.Disposables;
using System.Windows.Input;
using Uno;
using Uno.UI.DataBinding;
using Uno.Presentation;
using Uno.Logging;
using System.Globalization;
using System.Reflection;
using Uno.UI;
using Uno.UI.Converters;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Data
{
	public partial class BindingExpression : IDisposable, IValueChangedListener
	{
		private readonly Type _boundPropertyType;
		private readonly ManagedWeakReference _view;
		private readonly Type _targetOwnerType;

		private ManagedWeakReference _dataContext;
		private SerialDisposable _subscription = new SerialDisposable();

		private BindingPath _bindingPath;
		private bool _disposed;
		private ManagedWeakReference _explicitSourceStore;
		private readonly bool _isCompiledSource;
		private readonly bool _isElementNameSource;
		private bool _isBindingSuspended;
		private ValueGetterHandler _valueGetter;
		private ValueSetterHandler _valueSetter;

		// These flags are set to guard against infinite loops in 2-way binding scenarios.
		private bool _IsCurrentlyPushingTwoWay;
		private bool _IsCurrentlyPushing;

		public Binding ParentBinding { get; }

		internal DependencyPropertyDetails TargetPropertyDetails { get; }

		private object ExplicitSource
		{
			get => _explicitSourceStore?.Target;
			set => _explicitSourceStore = WeakReferencePool.RentWeakReference(this, value);
		}

		private BindingPath[] _updateSources = null;

		public string TargetName => TargetPropertyDetails.Property.Name;

		public object DataContext
		{
			get => _isElementNameSource || ExplicitSource != null ? ExplicitSource : _dataContext?.Target;
			set
			{
				if (ExplicitSource == null && !_disposed && DependencyObjectStore.AreDifferent(_dataContext?.Target, value))
				{
					var previousContext = _dataContext;

					_dataContext = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, value);
					ApplyBinding();

					// Return the reference to the pool after it's been released from the underlying bindings.
					// Failing to do so makes the reference change without the bindings knowing about it,
					// making the reference comparison always equal.
					Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(this, previousContext);
				}
			}
		}

		public object DataItem => _bindingPath.DataItem;

		internal BindingExpression(
			ManagedWeakReference viewReference,
			DependencyPropertyDetails targetPropertyDetails,
			Binding binding
		)
		{
			ParentBinding = binding;

			// As bindings are only glue between layers, they must not prevent collection neither of View nor Binding Source
			// Keep only a weak reference on View in order break the circular reference between Source.ValueChanged event and SetValue on View
			// especially when Binding source is a StaticRessource which is never collected !
			// Note: Bindings should still be disposed in order to also remove reference on the Source.
			_view = viewReference;

			if(_view?.Target is AttachedDependencyObject ado)
			{
				// This case is used to process x:Bind compiled bindings, where the POCO is wrapped around an
				// AttachedDependencyObject instance to make it bindable.
				_view = ado.OwnerWeakReference;
			}

			_targetOwnerType = targetPropertyDetails.Property.OwnerType;
			TargetPropertyDetails = targetPropertyDetails;
			_bindingPath = new BindingPath(
				path: ParentBinding.Path,
				fallbackValue: ParentBinding.FallbackValue,
				precedence: null,
				allowPrivateMembers: ParentBinding.CompiledSource != null
			);
			_boundPropertyType = targetPropertyDetails.Property.Type;

			TryGetSource(binding);

			if (ParentBinding.CompiledSource != null)
			{
				_isCompiledSource = true;
				ExplicitSource = ParentBinding.CompiledSource;
			}

			if (ParentBinding.XBindPropertyPaths != null)
			{
				_updateSources = ParentBinding
					.XBindPropertyPaths
					.Select(p => new BindingPath(path: p, fallbackValue: null, precedence: null, allowPrivateMembers: true)
					{
					})
					.ToArray();
			}

			if (ParentBinding.ElementName != null)
			{
				_isElementNameSource = true;
			}

			if (!(GetWeakDataContext()?.IsAlive ?? false))
			{
				ApplyFallbackValue();
			}

			ApplyExplicitSource();
			ApplyElementName();
		}

		private ManagedWeakReference GetWeakDataContext()
			=> _isElementNameSource || (_explicitSourceStore?.IsAlive ?? false) ? _explicitSourceStore : _dataContext;

		/// <summary>
		/// Sends the current binding target value to the binding source property in TwoWay bindings.
		/// </summary>
		public void UpdateSource()
		{
			if (TryGetTargetValue(out var value))
			{
				UpdateSource(value);
			}
		}

		/// <summary>
		/// Sends the current binding target value to the binding source property in TwoWay bindings.
		/// </summary>
		/// <remarks>
		/// This method is not part of UWP contract
		/// </remarks>
		/// <param name="value">The expected current value of the target</param>
		public void UpdateSource(object value)
		{
			if (_IsCurrentlyPushing || _IsCurrentlyPushingTwoWay)
			{
				return;
			}

			if (ParentBinding.Mode != BindingMode.TwoWay)
			{
				// Calling this method does nothing if the Mode value of the binding is not TwoWay.
				// [Microsoft documentation https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.data.bindingexpression.updatesource#remarks]
				return;
			}

#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
			try
#endif
			{
				_IsCurrentlyPushingTwoWay = true;

				// Convert if necessary
				if (ParentBinding.Converter != null)
				{
					value = ParentBinding.Converter.ConvertBack(
						value,
						_bindingPath.ValueType,
						ParentBinding.ConverterParameter,
						GetCurrentCulture()
					);
				}

				if (ParentBinding.XBindBack != null)
				{
					try
					{
						ParentBinding.XBindBack(DataContext, value);
					}
					catch (Exception exception)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to set the source value for x:Bind path [{ParentBinding.Path}]", exception);
						}
					}
				}
				else
				{
					_bindingPath.Value = value;
				}
			}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
			finally
#endif
			{
				_IsCurrentlyPushingTwoWay = false;
			}
		}

		/// <summary>
		/// Sets the source value in the data context
		/// </summary>
		/// <param name="value"></param>
		public void SetSourceValue(object value)
		{
			if (_disposed)
			{
				return;
			}

			try
			{
				if (ParentBinding.Mode == BindingMode.TwoWay
					&& ResolveUpdateSourceTrigger() == UpdateSourceTrigger.PropertyChanged)
				{
					UpdateSource(value);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to set value [{0}] to [{1}]".InvariantCultureFormat(value, TargetPropertyDetails), e);
			}
		}

		/// <summary>
		/// Suspends the processing of the binding until <see cref="ResumeBinding"/> is called.
		/// </summary>
		internal void SuspendBinding()
		{
			if (!_isBindingSuspended)
			{
				_isBindingSuspended = true;
				_subscription.Dispose();
			}
		}

		/// <summary>
		/// Resumes the binding processing suspended by <see cref="SuspendBinding"/>
		/// </summary>
		internal void ResumeBinding()
		{
			if (_isBindingSuspended)
			{
				_isBindingSuspended = false;
				ApplyBinding();
			}
		}

		/// <summary>
		/// Turns UpdateSourceTrigger.Default to DependencyProperty's FrameworkPropertyMetadata.DefaultUpdateSourceTrigger
		/// </summary>
		/// <returns></returns>
		private UpdateSourceTrigger ResolveUpdateSourceTrigger()
		{
			if (ParentBinding.UpdateSourceTrigger == UpdateSourceTrigger.Default)
			{
				var metadata = TargetPropertyDetails.Property?.GetMetadata(_targetOwnerType) as FrameworkPropertyMetadata;
				return metadata?.DefaultUpdateSourceTrigger
					?? UpdateSourceTrigger.PropertyChanged;
			}
			else
			{
				return ParentBinding.UpdateSourceTrigger;
			}
		}

		internal void ApplyElementName()
		{
			if (ParentBinding.ElementName is ElementNameSubject elementNameSubject)
			{

				if (elementNameSubject.IsLoadTimeBound)
				{
					// ElementName references in outer scopes will be resolvable on loading
					var target = NameScope.FindInNamescopes(_view?.Target as DependencyObject, elementNameSubject.Name);
					ExplicitSource = target;
					ApplyExplicitSource();
				}
				else
				{
					void applySource()
					{
						ExplicitSource = elementNameSubject.ElementInstance;
						ApplyExplicitSource();
					}

					// The element name instance may already have been
					// set, in relation to the declaration order in the xaml file.
					if (elementNameSubject.ElementInstance == null)
					{
						elementNameSubject
							.ElementInstanceChanged += (s, elementNameInstance) => applySource();
					}
					else
					{
						applySource();
					}
				}
			}
		}

		private void ApplyFallbackValue()
		{
			if (ParentBinding.FallbackValue != null)
			{
				SetTargetValue(ConvertToBoundPropertyType(ParentBinding.FallbackValue));
			}
			else if (TargetPropertyDetails != null)
			{
				SetTargetValue(TargetPropertyDetails.Property.GetMetadata(_view.Target?.GetType()).DefaultValue);
			}
		}

		private void ApplyExplicitSource()
		{
			if (_isElementNameSource || ExplicitSource != null && !_isCompiledSource)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Applying explicit source {0} on {1}", ExplicitSource?.GetType(), _view.Target?.GetType());
				}

				ApplyBinding();
			}
		}

		internal void ApplyCompiledSource()
		{
			if (_isCompiledSource && ExplicitSource != null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Applying compiled source {0} on {1}", ExplicitSource.GetType(), _view.Target?.GetType());
				}

				ApplyBinding();
			}
		}

		private void TryGetSource(Binding binding)
		{
			if (binding.Source is ElementNameSubject sourceSubject)
			{
				void applySource()
				{
					ExplicitSource = sourceSubject.ElementInstance;
					ApplyExplicitSource();
				}

				// The element name instance may already have been
				// set, in relation to the declaration order in the xaml file.
				if (sourceSubject.ElementInstance == null)
				{
					sourceSubject
						.ElementInstanceChanged += (s, elementNameInstance) => applySource();
				}
				else
				{
					applySource();
				}
			}
			else
			{
				ExplicitSource = binding.Source;
			}
		}

		private bool TryGetTargetValue(out object value)
		{
			var viewTarget = _view.Target;

			if (viewTarget != null)
			{
				value = GetValueGetter()(viewTarget);
				return true;
			}
			else
			{
				Dispose(); // Self dispose if view is no more available

				value = default(object);
				return false;
			}
		}

		private void SetTargetValue(object value)
		{
			var viewTarget = _view.Target;

			if (viewTarget != null)
			{
				GetValueSetter()(viewTarget, value);
			}
			else
			{
				Dispose(); // Self dispose if view is no more available
			}
		}

		private ValueGetterHandler GetValueGetter()
		{
			if (_valueGetter == null)
			{
				_valueGetter = BindingPropertyHelper.GetValueGetter(_targetOwnerType, TargetPropertyDetails.Property.Name);
			}

			return _valueGetter;
		}

		private ValueSetterHandler GetValueSetter()
		{
			if (_valueSetter == null)
			{
				_valueSetter = BindingPropertyHelper.GetValueSetter(_targetOwnerType, TargetPropertyDetails.Property.Name, true);
			}

			return _valueSetter;
		}

		private void ApplyBinding()
		{
			var weakDataContext = GetWeakDataContext();
			if (weakDataContext?.IsAlive ?? false)
			{
				// Dispose the subscription first, otherwise the previous
				// registration may receive the new datacontext value.
				_subscription.Disposable = null;

				if (_updateSources != null)
				{
					foreach (var bindingPath in _updateSources)
					{
						bindingPath.ValueChangedListener = this;

						if (ParentBinding.CompiledSource != null)
						{
							bindingPath.DataContext = ParentBinding.CompiledSource;
						}
						else
						{
							bindingPath.SetWeakDataContext(weakDataContext);
						}
					}

					_subscription.Disposable = Actions.ToDisposable(() =>
					{
						foreach (var bindingPath in _updateSources)
						{
							bindingPath.ValueChangedListener = null;
						}
					});

				}
				else
				{
					_bindingPath.ValueChangedListener = this;
					_bindingPath.SetWeakDataContext(weakDataContext);
					_subscription.Disposable = Actions.ToDisposable(() => _bindingPath.ValueChangedListener = null);
				}
			}
			else
			{
				if (_updateSources != null)
				{
					foreach (var source in _updateSources)
					{
						source.DataContext = null;
					}
				}
				else
				{
					_bindingPath.DataContext = null;
				}

				if (ParentBinding.FallbackValue != null)
				{
					ApplyFallbackValue();
				}
				else
				{
					SetTargetValueSafe(null, useTargetNullValue: false);
				}

				_subscription.Disposable = null;
			}
		}

		void IValueChangedListener.OnValueChanged(object o)
		{
			if (ParentBinding.XBindSelector != null)
			{
				try
				{
					var canSetTarget = _updateSources?.None(s => s.ValueType == null) ?? true;

					if (canSetTarget)
					{
						SetTargetValueSafe(ParentBinding.XBindSelector(DataContext));
					}
				}
				catch (Exception e)
				{
					ApplyFallbackValue();

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
					{
						this.Log().Error("Failed to apply binding to property [{0}] on [{1}] ({2})".InvariantCultureFormat(TargetPropertyDetails, _targetOwnerType, e.Message), e);
					}
				}
			}
			else
			{
				SetTargetValueSafe(o);
			}
		}

		private void SetTargetValueSafe(object v)
		{
			if (_IsCurrentlyPushingTwoWay)
			{
				return;
			}

			SetTargetValueSafe(v, useTargetNullValue: true);
		}

		private void SetTargetValueSafe(object v, bool useTargetNullValue)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat(
					"{0}.SetTargetValueSafe({1}) (p:{2}, h:{3:X8})",
					_view.Target?.GetType() ?? typeof(object),
					v.SelectOrDefault(vv => vv.ToString(), "[null]"),
					_bindingPath.Path,
					_view.Target?.GetHashCode()
				);
			}

			try
			{
				if (v is UnsetValue)
				{
					ApplyFallbackValue();
				}
				else
				{
					_IsCurrentlyPushing = true;
					// Get the source value and place it in the target property
					var convertedValue = ConvertValue(v);

					if (convertedValue == DependencyProperty.UnsetValue)
					{
						ApplyFallbackValue();
					}
					else if (useTargetNullValue && convertedValue == null && ParentBinding.TargetNullValue != null)
					{
						SetTargetValue(ConvertValue(ParentBinding.TargetNullValue));
					}
					else
					{
						SetTargetValue(convertedValue);
					}
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().Error("Failed to apply binding to property [{0}] on [{1}] ({2})".InvariantCultureFormat(TargetPropertyDetails, _targetOwnerType, e.Message), e);
				}

				ApplyFallbackValue();
			}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
			finally
#endif
			{
				_IsCurrentlyPushing = false;
			}
		}

		private string GetCurrentCulture() => CultureInfo.CurrentCulture.ToString();


		private object ConvertValue(object value)
		{
			if (ParentBinding.Converter != null)
			{
				return ParentBinding.Converter.Convert(value, _boundPropertyType, ParentBinding.ConverterParameter, GetCurrentCulture());
			}
			else
			{
				return ConvertToBoundPropertyType(value);
			}
		}

		private object ConvertToBoundPropertyType(object value)
		{
			// _boundPropertyType can be null for properties not bound for the actual instance (no matching properties found)
			// Can be used for "multi-binding" / inheritance.
			if (value != null
				&& _boundPropertyType != null
				&& _boundPropertyType != typeof(object) // Always can assign to object
				&& !value.GetType().Is(_boundPropertyType))
			{
				value = BindingPropertyHelper.Convert(() => _boundPropertyType, value);
			}

			return value;
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_bindingPath.Dispose();
			_disposed = true;
		}
	}
}
