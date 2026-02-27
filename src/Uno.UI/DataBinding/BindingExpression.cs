using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Data
{
	public partial class BindingExpression : IDisposable
	{
		private readonly Type _boundPropertyType;
		private readonly ManagedWeakReference _view;
		private readonly Type _targetOwnerType;

		private ManagedWeakReference _dataContext;
		private readonly SerialDisposable _subscription = new SerialDisposable();

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

		private BindingPath[] _updateSources;

		public string TargetName => TargetPropertyDetails.Property.Name;

		public object DataContext
		{
			get
			{
				if (ParentBinding.IsTemplateBinding)
				{
					return (_view?.Target as IDependencyObjectStoreProvider)?.Store.GetTemplatedParent2();
				}
				if (_isElementNameSource || ExplicitSource != null)
				{
					return ExplicitSource;
				}

				return _dataContext?.Target;
			}
			set
			{
				if (!_disposed &&
					!ParentBinding.IsTemplateBinding &&
					ExplicitSource == null &&
					DependencyObjectStore.AreDifferent(_dataContext?.Target, value))
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

		internal bool IsExplicitlySourced => _isElementNameSource || (_explicitSourceStore?.IsAlive ?? false);

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

			if (_view?.Target is AttachedDependencyObject ado)
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
				forAnimations: false,
				allowPrivateMembers: ParentBinding.IsXBind
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
					.Select(p => new BindingPath(path: p, fallbackValue: null, forAnimations: false, allowPrivateMembers: true)
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

			ApplyTemplateBindingParent();
			ApplyExplicitSource();
			ApplyElementName();
		}

		private ManagedWeakReference GetWeakTemplatedParent()
		{
			return (_view?.Target as IDependencyObjectStoreProvider)?.Store.GetTemplatedParentWeakRef();
		}

		private ManagedWeakReference GetWeakDataContext()
		{
			if (IsExplicitlySourced)
			{
				return _explicitSourceStore;
			}
			if (ParentBinding.IsTemplateBinding)
			{
				return GetWeakTemplatedParent();
			}

			return _dataContext;
		}

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
			if ((_IsCurrentlyPushing || _IsCurrentlyPushingTwoWay))
			{
				return;
			}

			if (ParentBinding.Mode != BindingMode.TwoWay)
			{
				// Calling this method does nothing if the Mode value of the binding is not TwoWay.
				// [Microsoft documentation https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.data.bindingexpression.updatesource#remarks]
				return;
			}

			try
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
					UpdateSourceOnXBindBack(value);
				}
				else
				{
					_bindingPath.Value = value;
				}
			}
			finally
			{
				_IsCurrentlyPushingTwoWay = false;
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateSourceOnXBindBack(object value)
		{
			try
			{
				if (FrameworkTemplatePool.IsRecycling && _view.TryGetTarget<IFrameworkTemplatePoolAware>(out _))
				{
					return;
				}

				ParentBinding.XBindBack(DataContext, value);
			}
			catch (Exception exception)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error($"Failed to set the source value for x:Bind path [{ParentBinding.Path}]", exception);
				}
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
				if (FrameworkTemplatePool.IsRecycling && _view.TryGetTarget<IFrameworkTemplatePoolAware>(out _))
				{
					return;
				}

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
				_subscription.Disposable = null;
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
		/// Refreshes the value to the target, as the bound source may not be observable
		/// </summary>
		internal void RefreshTarget()
		{
			ApplyElementName();

			if (
				// If a listener is set, ApplyBindings has been invoked
				_bindingPath.Expression is not null

				// If this is not an x:Bind
				&& _updateSources is null

				// If there's a valid DataContext
				&& GetWeakDataContext() is { IsAlive: true } weakDataContext)
			{
				// Apply the source on the target again (e.g. to reevaluate converters)
				_bindingPath.SetWeakDataContext(weakDataContext);
			}
		}

		/// <summary>
		/// Turns UpdateSourceTrigger.Default to PropertyChanged, except for TextBox.TextProperty it's Explicit
		/// </summary>
		/// <remarks>
		/// For TextBox.TextProperty, it should be LostFocus, but for now, it's Explicit and we are getting the
		/// same behavior by explicitly updating the binding on losing focus in TextBox code.
		/// </remarks>
		private UpdateSourceTrigger ResolveUpdateSourceTrigger()
		{
			if (ParentBinding.UpdateSourceTrigger == UpdateSourceTrigger.Default)
			{
				return TargetPropertyDetails.Property == TextBox.TextProperty ? UpdateSourceTrigger.Explicit : UpdateSourceTrigger.PropertyChanged;
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

		private void ApplyFallbackValue(bool useTypeDefaultValue = true)
		{
			if (ParentBinding.IsXBind && DataContext is null)
			{
				// On WinUI, the generated code for x:Bind doesn't do anything if the DC is null.
				// It doesn't even set the fallback value.
				return;
			}
			if (ParentBinding.FallbackValue != null
				|| (ParentBinding.CompiledSource != null && ParentBinding.IsFallbackValueSet))
			{
				SetTargetValue(ConvertToBoundPropertyType(ParentBinding.FallbackValue));
			}
			else if (useTypeDefaultValue && TargetPropertyDetails != null)
			{
				var viewTarget = _view.Target;
				SetTargetValue(TargetPropertyDetails.Property.GetDefaultValue(viewTarget as DependencyObject, viewTarget?.GetType()));
			}
		}

		private void ApplyExplicitSource()
		{
			if (_isElementNameSource || ExplicitSource != null && !_isCompiledSource)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Applying compiled source {0} on {1}", ExplicitSource.GetType(), _view.Target?.GetType());
				}

				ApplyBinding();
			}
		}
		internal void SuspendCompiledSource()
		{
			if (_isCompiledSource && ExplicitSource != null)
			{
				SuspendBinding();
			}
		}

		internal void ApplyTemplateBindingParent()
		{
			if (ParentBinding.IsTemplateBinding)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Applying template binding parent {0} on {1}", GetWeakTemplatedParent()?.Target?.GetType(), _view.Target?.GetType());
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
				if (ParentBinding.RelativeSource?.Mode == RelativeSourceMode.TemplatedParent)
				{
					// Very hacky workaround. In WinUI, setting a local value *after* animation value will
					// cause the local value to take precedence, and we aligned this behavior in Uno.
					// However, when TemplateBinding is involved, things go wrong in Uno.
					// This is due to lifecycle differences where we are setting Animations value first, then Local value from TemplateBinding
					// while the order should be the opposite.
					// It may be related to https://github.com/unoplatform/uno/issues/190
					try
					{
						ModifiedValue.SuppressLocalCanDefeatAnimations();
						GetValueSetter()(viewTarget, value);
					}
					finally
					{
						ModifiedValue.ContinueLocalCanDefeatAnimations();
					}
				}
				else
				{
					GetValueSetter()(viewTarget, value);
				}
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
						bindingPath.Expression = this;

						if (ParentBinding.CompiledSource != null)
						{
							bindingPath.DataContext = ParentBinding.CompiledSource;
						}
						else
						{
							bindingPath.SetWeakDataContext(weakDataContext);
						}
					}

					_subscription.Disposable = new DisposableAction(() =>
					{
						foreach (var bindingPath in _updateSources)
						{
							bindingPath.Expression = null;
						}
					});

				}
				else
				{
					_bindingPath.Expression = this;
					_bindingPath.SetWeakDataContext(weakDataContext);
					_subscription.Disposable = new DisposableAction(() => _bindingPath.Expression = null);
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

			_isBindingSuspended = false;
		}

		internal void OnValueChanged(object o)
		{
			if (ParentBinding.XBindSelector != null)
			{
				SetTargetValueForXBindSelector();
			}
			else
			{
				SetTargetValueSafe(o);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SetTargetValueForXBindSelector()
		{
			void SetTargetValue()
			{
				if (DataContext is null)
				{
					// On WinUI, the generated code for x:Bind doesn't do anything if the DC is null.
					// It doesn't even set the fallback value.
					return;
				}

				var canSetTarget = _updateSources?.None(s => s.ValueType == null) ?? true;
				if (canSetTarget)
				{
					var (isResolved, value) = ParentBinding.XBindSelector(DataContext);
					if (isResolved)
					{
						SetTargetValueSafe(value);
					}
					else
					{
						// x:Bind failed bindings don't change the target value
						// if no FallbackValue was specified.
						ApplyFallbackValue(useTypeDefaultValue: false);
					}
				}
				else
				{
					// x:Bind failed bindings don't change the target value
					// if no FallbackValue was specified.
					ApplyFallbackValue(useTypeDefaultValue: false);

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Binding path does not provide a value [{TargetPropertyDetails}] on [{_targetOwnerType}], using fallback value");
					}
				}
			}

			if (FeatureConfiguration.BindingExpression.HandleSetTargetValueExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void SetTargetValueWithTry()
				{
					try
					{
						SetTargetValue();
					}
					catch (Exception e)
					{
						ApplyFallbackValue();

						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							this.Log().Error("Failed to apply binding to property [{0}] on [{1}] ({2})".InvariantCultureFormat(TargetPropertyDetails, _targetOwnerType, e.Message), e);
						}
					}
				}

				SetTargetValueWithTry();
			}
			else
			{
				SetTargetValue();
			}
		}

		private void SetTargetValueSafe(object v)
		{
			if (_IsCurrentlyPushingTwoWay && ParentBinding.CompiledSource == null)
			{
				// For normal bindings, the source update through a converter
				// is ignored. Therefore, only the ConvertBack method is invoked as
				// the UpdateSource method is ignored because a two-way binding is
				// in progress.
				//
				// For x:Bind, a source update through the converter raises
				// a property change, which is in turn sent back to the target
				// after another Convert invocation.
				//
				// There is no loop happening because the binding engine is ignoring
				// the UpdateSource invocation from the target as a two-way binding
				// is still happening.

				return;
			}

			SetTargetValueSafe(v, useTargetNullValue: true);
		}

		private void SetTargetValueSafe(object v, bool useTargetNullValue)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat(
					"{0}.SetTargetValueSafe({1}) (p:{2}, h:{3:X8})",
					_view.Target?.GetType() ?? typeof(object),
					v.SelectOrDefault(vv => vv.ToString(), "[null]"),
					_bindingPath.Path,
					_view.Target?.GetHashCode()
				);
			}

			if (FeatureConfiguration.BindingExpression.HandleSetTargetValueExceptions)
			{
				try
				{
					InnerSetTargetValueSafe(v, useTargetNullValue);

					// Avoid using the finally clause, which on wasm
					// causes a transition to the interpreter. Exceptions here
					// are caught entirely, and not forwarded to the caller, which
					// allows for resetting the value in both the normal and exceptional flow.
					_IsCurrentlyPushing = false;
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						this.Log().Error("Failed to apply binding to property [{0}] on [{1}] ({2})".InvariantCultureFormat(TargetPropertyDetails, _targetOwnerType, e.Message), e);
					}

					try
					{
						ApplyFallbackValue();
					}
					catch (Exception e2)
					{
						// We ensure that _IsCurrentlyPushing can properly be reset, even
						// if `ApplyFallbackValue` fails to execute.

						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							this.Log().Error("Failed to apply fallback value to property [{0}] on [{1}] ({2})".InvariantCultureFormat(TargetPropertyDetails, _targetOwnerType, e2.Message), e2);
						}
					}

					_IsCurrentlyPushing = false;
				}
			}
			else
			{
				InnerSetTargetValueSafe(v, useTargetNullValue);
				_IsCurrentlyPushing = false;
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerSetTargetValueSafe(object v, bool useTargetNullValue)
		{
			if (v == DependencyProperty.UnsetValue)
			{
				ApplyFallbackValue();
			}
			else
			{
				_IsCurrentlyPushing = true;
				// Get the source value and place it in the target property

				// Only call the converted with null if the final segment (i.e. the tail of the chain) is null
				// In other words, if Path == "Outer.Inner" and Outer is null, don't call the converter.
				// Only call the converter with null if Outer is not null and Inner is null.
				// If the Binding path is empty and DataContext is null, the converter is still NOT called
				// https://github.com/unoplatform/uno/issues/16016
				var convertedValue = DataContext is { } && (v is { } || _bindingPath.OnlyLeafNodeNull()) ? ConvertValue(v) : DependencyProperty.UnsetValue;

				if (convertedValue == DependencyProperty.UnsetValue)
				{
					ApplyFallbackValue();
				}
				else if (useTargetNullValue && convertedValue == null && ParentBinding.TargetNullValue != null)
				{
					// The TargetNullValue is only used when the "leaf node" is null. Meaning
					// 1. binding to anything with a null DataContext does NOT use TargetNullValue
					// 2. binding to OuterNode.LeafNode with DC != null && DC.OuterNode == null does NOT use TargetNullValue
					// 3. binding to OuterNode.LeafNode with DC.OuterNode != null && DC.OuterNode.LeafNode == null will use TargetNullValue
					SetTargetValue(ConvertValue(ParentBinding.TargetNullValue));
				}
				else
				{
					SetTargetValue(convertedValue);
				}
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

		[UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "Types manipulated here have been marked earlier")]
		private object ConvertToBoundPropertyType(object value)
		{
			// _boundPropertyType can be null for properties not bound for the actual instance (no matching properties found)
			// Can be used for "multi-binding" / inheritance.
			if (value != null
				&& _boundPropertyType != null
				&& _boundPropertyType != typeof(object) // Always can assign to object
				&& !value.GetType().Is(_boundPropertyType))
			{
				value = BindingPropertyHelper.Convert(_boundPropertyType, value);
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
