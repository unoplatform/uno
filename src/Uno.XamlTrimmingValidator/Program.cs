using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Options;

string? pathToAssembly = null;
string? pathToHintsFile = null;

var options = new OptionSet();
options.Add("target-assembly=", "path to assembly", s => pathToAssembly = s);
options.Add("hints-file=", "path to hints file", s => pathToHintsFile = s);

try
{
	options.Parse(args);
}
catch (OptionException e)
{
	Console.WriteLine(e.Message);

	return -1;
}

if (!File.Exists(pathToAssembly!))
{
	Console.WriteLine("Target assembly doesn't exist.");

	return -1;
}

if (!File.Exists(pathToHintsFile!))
{
	Console.WriteLine("Hints file doesn't exist.");

	return -1;
}

using var assembly = AssemblyDefinition.ReadAssembly(pathToAssembly, new ReaderParameters { InMemory = true });

var linkerHintsFQN = $"{Path.GetFileNameWithoutExtension(pathToAssembly)}.__LinkerHints";

var hints =
	assembly
		.MainModule
		.GetType(linkerHintsFQN)
		.Properties
		.ToDictionary(p => p.Name, p => p.GetMethod.Body.Instructions[0].OpCode == OpCodes.Ldc_I4_1); // True is an I4 with a constant value of 1.

var hintsToValidate =
	File
		.ReadAllLines(pathToHintsFile)
		.Select(l =>
		{
			var tokens = l.Split('=');

			return (tokens[0], bool.Parse(tokens[1]));
		});

foreach (var (hint, value) in hintsToValidate)
{
	if (hints.TryGetValue(hint, out var v) && v == value)
	{
		continue;
	}

	Console.WriteLine($"[Fail] Hint {hint} is not found or has the wrong value.");

	return -1;
}

Console.WriteLine("[Info] Success.");

return 0;
