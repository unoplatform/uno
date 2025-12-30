using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Reflection.Emit;
using System.Reflection;

namespace Uno.UI
{
	public class FastInvokerBuilder
	{
		public delegate object FastInvokerHandler(object instance, object[] parms);

		[RequiresDynamicCode("From DynamicMethod: Creating a DynamicMethod requires dynamic code.")]
		public static FastInvokerHandler DynamicInvokerBuilder(System.Reflection.MethodInfo methodInfo)
		{
			DynamicMethod dynamicMethod = new DynamicMethod(
				string.Empty,
				typeof(object),
				new Type[] {
					typeof(object),
					typeof(object[])
				},
				methodInfo.DeclaringType.Module,
				true
			);

			ILGenerator il = dynamicMethod.GetILGenerator();
			ParameterInfo[] ps = methodInfo.GetParameters();

			Type[] paramTypes = new Type[ps.Length];
			for (int i = 0; i < paramTypes.Length; i++)
			{
				paramTypes[i] = ps[i].ParameterType;
			}

			LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];
			for (int i = 0; i < paramTypes.Length; i++)
			{
				locals[i] = il.DeclareLocal(paramTypes[i]);
			}

			for (int i = 0; i < paramTypes.Length; i++)
			{
				il.Emit(OpCodes.Ldarg_1);
				EmitFastInt(il, i);
				il.Emit(OpCodes.Ldelem_Ref);
				EmitCastToReference(il, paramTypes[i]);
				il.Emit(OpCodes.Stloc, locals[i]);
			}

			il.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < paramTypes.Length; i++)
			{
				il.Emit(OpCodes.Ldloc, locals[i]);
			}

			il.EmitCall(OpCodes.Call, methodInfo, null);

			if (methodInfo.ReturnType == typeof(void))
				il.Emit(OpCodes.Ldnull);
			else
				EmitBoxIfNeeded(il, methodInfo.ReturnType);

			il.Emit(OpCodes.Ret);

			return (FastInvokerHandler)dynamicMethod.CreateDelegate(typeof(FastInvokerHandler));
		}

		private static void EmitCastToReference(ILGenerator il, System.Type type)
		{
			if (type.IsValueType)
			{
				il.Emit(OpCodes.Unbox_Any, type);
			}
			else
			{
				il.Emit(OpCodes.Castclass, type);
			}
		}

		private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
		{
			if (type.IsValueType)
			{
				il.Emit(OpCodes.Box, type);
			}
		}

		private static void EmitFastInt(ILGenerator il, int value)
		{
			switch (value)
			{
				case -1:
					il.Emit(OpCodes.Ldc_I4_M1);
					return;
				case 0:
					il.Emit(OpCodes.Ldc_I4_0);
					return;
				case 1:
					il.Emit(OpCodes.Ldc_I4_1);
					return;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2);
					return;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3);
					return;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4);
					return;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5);
					return;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6);
					return;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7);
					return;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8);
					return;
			}

			if (value > -129 && value < 128)
			{
				il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4, value);
			}
		}

	}
}
