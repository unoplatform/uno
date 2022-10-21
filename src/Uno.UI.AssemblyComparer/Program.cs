using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.AssemblyComparer
{
    class Program
    {
        static void Main(string[] args)
        {
			var source = ReadModule(args[0]);
			var target = ReadModule(args[1]);
			var outputFile = args[2];

			Console.WriteLine($"Comparing {source} and {target}");

			CompareTypes(outputFile, source.MainModule.GetTypes(), target.MainModule.GetTypes());

			Console.WriteLine($"Done comparing.");
		}

		private static void CompareTypes(string outputFile, IEnumerable<TypeDefinition> sourceTypes, IEnumerable<TypeDefinition> targetTypes)
		{
			Console.WriteLine($"Generating diff to {outputFile}");

			using (var writer = new StreamWriter(outputFile))
            {
                // Types only in target
                var q = from targetType in targetTypes
                        where !sourceTypes.Any(t => t.FullName == targetType.FullName && IsImplemented(t.CustomAttributes))
                        where targetType.IsPublic
						where targetType.Namespace.StartsWith("Windows.UI.Xaml", StringComparison.Ordinal)
                        group targetType by targetType.Namespace into namespaces
                        orderby namespaces.Key
                        select new {
							Types = namespaces,
							TypesForNamespace = targetTypes
								.Where(t => t.IsPublic)
								.Where(t => t.Namespace == namespaces.Key)
						};

                writer.WriteLine("Missing {0} types:", q.SelectMany(i => i.Types).Count());

                foreach (var newNamespace in q)
                {
					var allTypesCount = newNamespace.TypesForNamespace.Count();
					var existingTypes = allTypesCount - newNamespace.Types.Count();
					var percentDone = ((existingTypes / (float)allTypesCount) * 100.0);

					writer.WriteLine("+ {0} (Done {1} of {2}, {3:0.0}%)", newNamespace.Types.Key, existingTypes, allTypesCount, percentDone);

					foreach (var type in newNamespace.TypesForNamespace.Except(newNamespace.Types))
					{
						writer.WriteLine("\t= {0}", type.FullName);
					}

					foreach (var type in newNamespace.Types)
					{
						writer.WriteLine("\t+ {0}", type.FullName);
					}
				}

				writer.WriteLine("********************");

                // New properties only in target
                var q1 = from targetType in targetTypes
						 where targetType.Namespace.StartsWith("Windows.UI.Xaml", StringComparison.Ordinal)
						 let sourceType = sourceTypes.FirstOrDefault(t => t.FullName == targetType.FullName)
                         where sourceType != null
                         where targetType.IsPublic
                         select new { sourceType, targetType } into type
                         from targetProp in type.targetType.Properties
                         where !ContainsProperty(type.sourceType, targetProp.Name)
                         group targetProp by targetProp.DeclaringType.FullName into types
                         orderby types.Key
                         select types;

                writer.WriteLine("Found {0} missing properties in existing types:", q1.SelectMany(i => i).Count());

                foreach (var updatedType in q1)
                {
                    writer.WriteLine("+ {0}", updatedType.Key);
                    foreach (var property in updatedType)
                    {
                        writer.WriteLine("\t+ {0}", property.DeclaringType.FullName + "." + property.Name);
                    }
                }
            }
        }

		private static bool ContainsProperty(TypeDefinition sourceType, string name)
		{
			do
			{
				if (sourceType.Properties.Any(p => p.Name == name && IsImplemented(p.CustomAttributes)))
				{
					return true;
				}

				if (name.EndsWith("Property", StringComparison.Ordinal))
				{
					if (sourceType.Fields.Any(p => p.Name == name))
					{
						return true;
					}
				}

				if (!(sourceType.BaseType?.Scope.Name.StartsWith("Uno.UI,", StringComparison.Ordinal) ?? false))
				{
					return false;
				}

				sourceType = sourceType.BaseType?.Resolve();
			}
			while (sourceType != null);

			return false;
		}

		private static bool IsImplemented(Mono.Collections.Generic.Collection<CustomAttribute> attributes) 
			=> !(attributes?.Any(a => a.AttributeType.Name == "NotImplementedAttribute") ?? false);

		private static AssemblyDefinition ReadModule(string path)
        {
            var resolver = new DefaultAssemblyResolver();

            return AssemblyDefinition.ReadAssembly(path, new ReaderParameters() { AssemblyResolver = resolver });
        }
    }
}
