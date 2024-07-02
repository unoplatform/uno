using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Uno.Extensions;
using Uno.UI.DataBinding;

#if __ANDROID__
using _NativeObject = Android.Views.View;
#elif __IOS__
using _NativeObject = Foundation.NSObject;
#else
using _NativeObject = System.Object;
#endif

namespace Windows.UI.Xaml.Data
{

	public partial class Binding : BindingBase
	{
		/// <summary>
		/// A weak <see cref="Source"/> storage for controls bases on native elements.
		/// </summary>
		private WeakReference _weakSource;

#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
		/// <summary>
		/// On platforms which perform implicit and opaque pinning of native references, it
		/// is required to keep weak references to direct and indirect references to UIElement instances.
		/// Keeping weak references is costly,  so it's enabled only on select platforms.
		/// Note that this is needed only for x:Bind related operations, where the lifespan of
		/// the binding is explicitly tied to the object containing the weak references. This means
		/// that there's weak references will be kept alive properly.
		/// </summary>
		private ManagedWeakReference _compiledSource;
#endif

		/// <summary>
		/// A hard storage for other types of <see cref="Source"/> content.
		/// </summary>
		private object _source;

		/// <summary>
		/// Storage for the FallbackValue property
		/// </summary>
		private object _fallbackValue;

		/// <summary>
		/// A set of flags for this instance
		/// </summary>
		private BindingFlags _flags;

		public Binding()
		{

		}

		/// <summary>
		/// Internal method used to create paths. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="converter"></param>
		/// <param name="converterParameter"></param>
		internal Binding(PropertyPath path = default(PropertyPath), IValueConverter converter = null, object converterParameter = null)
		{
			// This method should not be made public, for API compatibility with Jupiter.

			Path = path ?? new PropertyPath(String.Empty);
			Converter = converter;
			ConverterParameter = converterParameter;
			Mode = BindingMode.OneWay;
		}

		public static implicit operator Binding(string path)
		{
			return new Binding(path);
		}

		/// <summary>
		/// Gets or sets the path to the binding source property.
		/// </summary>
		/// <value>The path.</value>
		public PropertyPath Path { get; set; }

		/// <summary>
		/// Gets or sets the converter object that is called by the binding engine to modify the data as it is passed between the source and target, or vice versa.
		/// </summary>
		/// <value>The converter.</value>
		public IValueConverter Converter { get; set; }

		/// <summary>
		/// Gets or sets a parameter that can be used in the Converter logic.
		/// </summary>
		/// <value>The converter parameter.</value>
		public object ConverterParameter { get; set; }

		/// <summary>
		/// Gets or sets a value that names the language to pass to any converter specified by the Converter property.
		/// </summary>
		/// <value>The converter language.</value>
		public string ConverterLanguage { get; set; }

		/// <summary>
		/// Gets or sets the name of the element to use as the binding source for the Binding.
		/// </summary>
		/// <value>The name of the element, or an ElementNameSubject instance that can be used to monitor the element name instance changes.</value>
		public object ElementName { get; set; }

		/// <summary>
		/// Gets or sets the value to use when the binding is unable to return a value.
		/// </summary>
		/// <value>The fallback value.</value>
		public object FallbackValue
		{
			get => _fallbackValue;
			set
			{
				_fallbackValue = value;

				// Mark the value as set, regardless of the value itself
				// so x:Bind can set that value to the target.
				_flags |= BindingFlags.FallbackValueSet;
			}
		}

		internal string FallbackValueThemeResource { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates the direction of the data flow in the binding.
		/// </summary>
		/// <value>The mode.</value>
		public BindingMode Mode { get; set; }

		/// <summary>
		/// Gets or sets the binding source by specifying its location relative to the position of the binding target. This is most often used in bindings within XAML control templates.
		/// </summary>
		/// <value>The relative source.</value>
		public RelativeSource RelativeSource { get; set; }

		/// <summary>
		/// Gets or sets the data source for the binding.
		/// </summary>
		/// <value>The source.</value>
		public object Source
		{
			get => _weakSource?.Target ?? _source;
			set
			{
				if (value != null)
				{
					// Native iOS and Android objects objects make for native GC loops
					// Break this cycle for these objects, assuming those objects are 
					// in the visual tree, where another GC root keeps them alive.
					// In this case, we create a weak reference so both object can be 
					// collected properly.
					// In the other case, we keep a hard reference to the source.

#if __ANDROID__ || __IOS__
					if (value is _NativeObject no)
					{
						_weakSource = new WeakReference(no);
						_source = null;
					}
					else
#endif
					{
						_weakSource = null;
						_source = value;
					}
				}
				else
				{
					_weakSource = null;
					_source = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value that is used in the target when the value of the source is null.
		/// </summary>
		/// <value>The target null value.</value>
		public object TargetNullValue { get; set; }

		internal string TargetNullValueThemeResource { get; set; }

		internal object ParseContext { get; set; }

		/// <summary>
		/// Gets or sets a value that determines the timing of binding source updates for two-way bindings.
		/// </summary>
		/// <value>The update source trigger.</value>
		public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

		/// <summary>
		/// Gets or sets the data source for a compiled binding (x:Bind).
		/// </summary>
		/// <value>The source.</value>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public object CompiledSource
#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
		{ get => _compiledSource?.Target; set => _compiledSource = WeakReferencePool.RentWeakReference(this, value); }
#else
		{ get; set; }
#endif

		/// <summary>
		/// Provides the method used in the context of x:Bind expressions to
		/// get the resulting value.
		/// </summary>
		internal Func<object, (bool, object)> XBindSelector
		{ get; private set; }

		/// <summary>
		/// Provides the method used to set the value back to the source.
		/// </summary>
		internal Action<object, object> XBindBack
		{ get; private set; }

		/// <summary>
		/// List of paths to observe in the context x:Bind expressions
		/// </summary>
		internal string[] XBindPropertyPaths { get; private set; }

		// Each of these values could be null and the Binding could still be an x:Bind, but they can't all be null
		internal bool IsXBind => XBindSelector is not null || XBindPropertyPaths is not null || CompiledSource is not null || XBindBack is not null;

		internal void SetBindingXBindProvider(object compiledSource, Func<object, (bool, object)> xBindSelector, Action<object, object> xBindBack, string[] propertyPaths = null)
		{
			CompiledSource = compiledSource;
			XBindSelector = xBindSelector;
			XBindPropertyPaths = propertyPaths;
			XBindBack = xBindBack;
		}

		/// <summary>
		/// Determines if the FallbackValue has been set
		/// </summary>
		/// <remarks>To be used for x:Bind only</remarks>
		internal bool IsFallbackValueSet
			=> _flags.HasFlag(BindingFlags.FallbackValueSet);

		[Flags]
		private enum BindingFlags
		{
			None = 0,

			/// <summary>
			/// Determines if the FallbackValue has been set.
			/// </summary>
			FallbackValueSet = 1,
		}
	}
}

