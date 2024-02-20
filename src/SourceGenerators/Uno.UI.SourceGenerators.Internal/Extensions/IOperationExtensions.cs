using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal.Extensions;

internal static class IOperationExtensions
{
	public static IOperation WalkDownConversion(this IOperation operation)
	{
		while (operation is IConversionOperation conversionOperation)
		{
			operation = conversionOperation.Operand;
		}

		return operation;
	}
}
