// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
namespace Uno
{
	/// <summary>
	/// Static method to create an <see cref="Option{T}"/>
	/// </summary>
	internal abstract class Option
	{
		/// <summary>
		/// Creates an option which represent an absence of value.
		/// </summary>
		public static Option<T> None<T>() => Uno.None<T>.Instance;

		/// <summary>
		/// Creates an option for a given value.
		/// </summary>
		public static Some<T> Some<T>(T value) => new Some<T>(value);

		protected internal Option(OptionType type)
		{
			Type = type;
		}

		public OptionType Type { get; }

		/// <summary>
		/// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not.
		/// </summary>
		public bool MatchNone()
		{
			return Type == OptionType.None;
		}

		/// <summary>
		/// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not
		/// </summary>
		public bool MatchSome()
		{
			return Type == OptionType.Some;
		}

		/// <summary>
		/// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not and send back the value.
		/// </summary>
		public bool MatchSome(out object value)
		{
			value = Type == OptionType.Some ? GetValue() : default(object);

			return Type == OptionType.Some;
		}

		protected abstract object GetValue();
	}

	/// <summary>
	/// This is a base class for an option.
	/// </summary>
	/// <remarks>
	/// This is the implementation of a functional "Option Type" using F# semantic
	/// https://en.wikipedia.org/wiki/Option_type
	/// </remarks>
	internal abstract class Option<T> : Option
	{
		protected Option(OptionType type)
			: base(type)
		{
		}

		/// <summary>
		/// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not and send back the value.
		/// </summary>
		public bool MatchSome(out T value)
		{
			value = Type == OptionType.Some ? (T)GetValue() : default(T);

			return Type == OptionType.Some;
		}

		/// <summary>
		/// Implicit conversion from <see cref="Option{T}"/> to T.
		/// </summary>
		/// <remarks>
		/// `null` or `None` will become `default(T)`.
		/// </remarks>
		/// <param name="o"></param>
		public static implicit operator T(Option<T> o)
		{
			if (o == null || o.MatchNone())
			{
				return default(T);
			}
			return ((Some<T>)o).Value;
		}
		/// <summary>
		/// Implicit conversion of T to <see cref="Some{T}"/>
		/// </summary>
		public static implicit operator Option<T>(T o)
		{
			return Some(o);
		}
	}
}
