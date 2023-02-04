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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Extensions;
using System.Linq.Expressions;

namespace Uno.Reflection
{
	internal class DefaultReflectionExtensions : IReflectionExtensions
	{
		#region IReflectionExtensions Members

		public virtual object Get(IReflectionExtensionPoint extensionPoint, string memberName)
		{
			return Get<object>(extensionPoint, memberName);
		}

		public virtual T Get<T>(IReflectionExtensionPoint extensionPoint, string memberName)
		{
			return (T)FindValueDescriptor(extensionPoint, memberName).GetValue(extensionPoint.ExtendedValue);
		}

		public virtual object Get(IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames)
		{
			return Get<object>(extensionPoint, memberNames);
		}

		public virtual T Get<T>(IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames)
		{
			return Get<T>(extensionPoint, GetDescriptors(extensionPoint, memberNames).Cast<IValueMemberDescriptor>());
		}

		public virtual object Get(IReflectionExtensionPoint extensionPoint,
								  IEnumerable<IValueMemberDescriptor> descriptors)
		{
			return Get<object>(extensionPoint, descriptors);
		}

		public virtual T Get<T>(IReflectionExtensionPoint extensionPoint,
								IEnumerable<IValueMemberDescriptor> descriptors)
		{
			var value = extensionPoint.ExtendedValue;

			descriptors.ForEach(item => value = item.GetValue(value));

			return (T)value;
		}

		public virtual void Set<T>(IReflectionExtensionPoint extensionPoint, string memberName, T value)
		{
			FindValueDescriptor(extensionPoint, memberName).SetValue(extensionPoint.ExtendedValue, value);
		}

		public virtual void Set<T>(IReflectionExtensionPoint extensionPoint, IEnumerable<string> memberNames, T value)
		{
			Set(extensionPoint, GetDescriptors(extensionPoint, memberNames).Cast<IValueMemberDescriptor>(), value);
		}

		public virtual void Set<T>(IReflectionExtensionPoint extensionPoint,
								   IEnumerable<IValueMemberDescriptor> descriptors, T value)
		{
			var allButLast = descriptors.Take(descriptors.Count() - 1);

			var last = descriptors.Last();

			var currentValue = extensionPoint.Get(allButLast);

			currentValue.Reflection().Set(last.MemberInfo.Name, value);
		}

		public virtual IEnumerable<IValueMemberDescriptor> GetValueDescriptors(IReflectionExtensionPoint extensionPoint,
																			   IEnumerable<string> memberNames)
		{
			return GetDescriptors(extensionPoint, memberNames).Cast<IValueMemberDescriptor>();
		}

		public virtual IEnumerable<IMemberDescriptor> GetDescriptors(IReflectionExtensionPoint extensionPoint,
																	 IEnumerable<string> memberNames)
		{
			IMemberDescriptor descriptor = null;

			foreach (var memberName in memberNames)
			{
				descriptor = descriptor == null
								 ? FindDescriptor(extensionPoint, memberName)
								 : FindDescriptor(descriptor, memberName);

				yield return descriptor;
			}
		}

		public virtual IMemberDescriptor GetDescriptor(IReflectionExtensionPoint extensionPoint)
		{
#if !WINDOWS_UWP
			return GetDescriptor(extensionPoint.ExtendedType);
#else
            return GetDescriptor(extensionPoint.ExtendedType.GetTypeInfo());
#endif
		}

		public virtual IMemberDescriptor GetDescriptor(IMemberDescriptor descriptor, string memberName)
		{
			var item = FindDescriptor(descriptor, memberName);

			return item.Validation().Found();
		}

		public virtual IMemberDescriptor FindDescriptor(IMemberDescriptor descriptor, string memberName)
		{
			var mi = descriptor.Type.FindMemberInfo(memberName, BindingContract.Default);

			return mi == null ? null : GetDescriptor(mi);
		}

		public virtual IValueMemberDescriptor GetValueDescriptor(IReflectionExtensionPoint extensionPoint,
																 string memberName)
		{
			var descriptor = FindValueDescriptor(extensionPoint, memberName);

			return descriptor.Validation().Found();
		}

		public virtual IValueMemberDescriptor FindValueDescriptor(IReflectionExtensionPoint extensionPoint,
																  string memberName)
		{
			return (IValueMemberDescriptor)FindDescriptor(extensionPoint, memberName);
		}

		public virtual IMemberDescriptor GetDescriptor(IReflectionExtensionPoint extensionPoint, string memberName)
		{
			var descriptor = FindDescriptor(extensionPoint, memberName);

			return descriptor.Validation().Found();
		}

		public virtual IMemberDescriptor FindDescriptor(IReflectionExtensionPoint extensionPoint, string memberName)
		{
			return FindDescriptor(extensionPoint, memberName, BindingContract.Default);
		}

		public virtual IMemberDescriptor FindDescriptor(IReflectionExtensionPoint extensionPoint, string memberName,
														BindingContract contract)
		{
			var mi = extensionPoint.ExtendedType.FindMemberInfo(memberName, contract);

			return mi == null ? null : GetDescriptor(mi);
		}

		public virtual IReflectionExtensionPoint Reflection(Type type)
		{
			return new ReflectionExtensionPoint<object>(type);
		}

		public virtual IReflectionExtensionPoint<T> Reflection<T>(T instance)
		{
			return new ReflectionExtensionPoint<T>(instance);
		}

		public virtual IDisposable Observe(IEventDescriptor descriptor, object publisher, object observer,
										   string methodName)
		{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
			var callback = Delegate.CreateDelegate(descriptor.Type, observer, methodName);

			descriptor.Add.Invoke(publisher, callback);

			Action dispose = () => descriptor.Remove.Invoke(publisher, callback);

			return dispose.ToDisposable();
#else
            throw new NotImplementedException();
#endif
		}

		public virtual IMemberDescriptor GetDescriptor(MemberInfo mi)
		{
			var descriptor = FindDescriptor(mi);

			if (descriptor == null)
			{
				throw new ArgumentException("Not Supported");
			}
			return descriptor;
		}

		public virtual IMemberDescriptor GetDescriptor<TArg1>(IReflectionExtensionPoint<TArg1> extensionPoint, Expression<Action<TArg1>> func)
		{
			return GetMemberInfo((LambdaExpression)func);
		}

		public virtual IMemberDescriptor GetDescriptor<TArg1, TResult>(IReflectionExtensionPoint<TArg1> extensionPoint, Expression<Func<TArg1, TResult>> func)
		{
			return GetMemberInfo((LambdaExpression)func);
		}


		#endregion

		// TODO: rewrite if-else statements
		protected virtual IMemberDescriptor FindDescriptor(MemberInfo mi)
		{
			var propertyInfo = mi as PropertyInfo;

			if (propertyInfo == null)
			{
				var fieldInfo = mi as FieldInfo;

				if (fieldInfo == null)
				{
					var methodInfo = mi as MethodInfo;

					if (methodInfo == null)
					{
						var eventInfo = mi as EventInfo;

						if (eventInfo == null)
						{
#if !WINDOWS_UWP && !HAS_CRIPPLEDREFLECTION
							var type = mi as Type;

							if (type == null)
							{
								return null;
							}
							else
							{
#if !SILVERLIGHT && !WINDOWS_PHONE
								if (type.IsNested)
								{
									return new NestedTypeDescriptor(type);
								}
								else
#endif
								{
									return new TypeDescriptor(type);
								}
							}
#else
                                return null;
#endif
						}
						else
						{
							return new EventDescriptor(eventInfo);
						}
					}
					else
					{
						return null;
					}
				}
				else
				{
#if !HAS_CRIPPLEDREFLECTION
					return new FieldDescriptor(fieldInfo);
#else
                    return null;
#endif
				}
			}
			else
			{
				return new PropertyDescriptor(propertyInfo);
			}
		}

		private IMemberDescriptor GetMemberInfo(LambdaExpression func)
		{
			MethodCallExpression call = func.Body as MethodCallExpression;

			MemberExpression member = func.Body as MemberExpression;

			if (member != null)
			{
				return FindDescriptor(member.Member);
			}

			throw new ArgumentException("The expression must call a method, a property or get a field at the first level.");
		}
	}
}
