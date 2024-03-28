using System;
using System.ComponentModel;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Helpers.Xaml
{
	public static class ApplyExtensions
	{
		public delegate Binding BindingApplyHandler(Binding binding);
		public delegate Binding BindingApplyWithParamHandler(Binding binding, object that);

		/// <summary>
		/// Executes the provided apply handler on the binding instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Binding BindingApply(this Binding instance, BindingApplyHandler apply)
		{
			apply(instance);
			return instance;
		}

		/// <summary>
		/// Executes the provided apply handler on the binding instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Binding BindingApply(this Binding instance, object that, BindingApplyWithParamHandler apply)
		{
			apply(instance, that);
			return instance;
		}

		/// <summary>
		/// Executes the provided apply handler on the specified instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TType GenericApply<TType>(this TType instance, Action<TType> apply)
		{
			apply(instance);
			return instance;
		}

		/// <summary>
		/// Executes the provided apply handler on the specified instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TType GenericApply<TType, TArg1>(this TType instance, TArg1 arg1, Action<TType, TArg1> apply)
		{
			apply(instance, arg1);
			return instance;
		}

		/// <summary>
		/// Executes the provided apply handler on the specified instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TType GenericApply<TType, TArg1, TArg2>(this TType instance, TArg1 arg1, TArg2 arg2, Action<TType, TArg1, TArg2> apply)
		{
			apply(instance, arg1, arg2);
			return instance;
		}
	}
}

