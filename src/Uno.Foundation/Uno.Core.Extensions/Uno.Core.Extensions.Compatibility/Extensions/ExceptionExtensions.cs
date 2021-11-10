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
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Uno.Extensions
{
	public static class ExceptionExtensions
	{
		public static void WriteToDebug(this Exception ex, string message)
		{
			var details = ex.GetDetails(message).ToString();
			Debug.WriteLine(details);
		}

		public static StringBuilder GetDetails(this Exception ex, string message)
		{
			var sb = new StringBuilder();
			sb.AppendLine(message);

			var mostInner = ex;
			while (mostInner.InnerException != null)
			{
				mostInner = mostInner.InnerException;
			}
			sb.Append("==>");
			sb.AppendLine(mostInner.Message);

			var inner = ex;
			int i = 1;
			while (inner != null)
			{
				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"{3}) {1}{0}Stack:{0}{2}",
					Environment.NewLine,
					inner.Message,
					inner.StackTrace,
					i++);
				inner = inner.InnerException;
			}
			return sb;
		}

		/// <summary>
		/// Determines whether the exception is of the specified type. 
		/// In the case of an <see cref="AggregateException"/>, it looks in the inner exceptions.
		/// </summary>
		/// <param name="exception">Exception to test</param>
		/// <typeparam name="TException">The type of the exception to look for</typeparam>
		/// <returns>The exception instance if found, null otherwise</returns>
		public static TException GetPossibleInnerException<TException>(this Exception exception)
			where TException : Exception
		{
			var typedException = exception as TException;
			if (typedException != null)
			{
				return typedException;
			}

			var aggregateException = exception as AggregateException;
			if (aggregateException != null)
			{
				foreach (var e in aggregateException.Flatten().InnerExceptions)
				{
					var innerTypeException = e as TException;
					if (innerTypeException != null)
					{
						return innerTypeException;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Determines whether the exception is of the specified type. 
		/// In the case of an <see cref="AggregateException"/>, it looks in the inner exceptions.
		/// </summary>
		/// <param name="exception">Exception to test</param>
		/// <param name="exceptionType">Type of exception to match</param>
		/// <returns>True if the exception is or contains an exception of the specified type</returns>
		public static bool IsOrContainsExceptionType(this Exception exception, Type exceptionType)
		{
			if (exception.GetType().Equals(exceptionType))
			{
				return true;
			}

			if (exception is AggregateException aggregateException)
			{
				foreach (var e in aggregateException.Flatten().InnerExceptions)
				{
					if (e.GetType().Equals(exceptionType))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether the exception is of the specified type. 
		/// In the case of an <see cref="AggregateException"/>, it looks in the inner exceptions .
		/// </summary>
		/// <typeparam name="TException">Type of exception to match</typeparam>
		/// <param name="exception">Exception to test</param>
		/// <returns>True if the exception is or contains an exception of the specified type</returns>
		public static bool IsOrContainsExceptionType<TException>(this Exception exception)
			where TException : Exception
		{
			var ex = exception.GetPossibleInnerException<TException>();
			return ex != null;
		}
	}
}
