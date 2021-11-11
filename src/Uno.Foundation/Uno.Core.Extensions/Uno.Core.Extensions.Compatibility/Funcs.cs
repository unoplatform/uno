using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Extensions.ValueType;

namespace Uno.Core
{
	internal static class Funcs<T>
	{
		public static readonly Func<T, object> CastFrom = Funcs<T, object>.Cast;
		public static readonly Func<object, T> CastTo = Funcs<object, T>.Cast;
		public static readonly Func<T> CreateInstance = Funcs<T, T>.CreateInstance;
		public static readonly Func<T> Default = () => default(T);

		public static Func<T, bool> IsDefault
		{
			get { return item => Equals(item, default(T)); }
		}

		public static Func<T, bool> IsNotDefault
		{
			get { return IsDefault.Not(); }
		}
	}

	internal static class Funcs<TParam, TResult>
	{
		public static readonly Func<TParam, TResult> Cast = item => (TResult)(object)item;

		public static readonly Func<TParam, TResult> Convert = item => item.Conversion().To<TResult>();

		public static readonly Func<TParam> CreateInstance = () => typeof(TResult).New<TParam>();
	}
}
