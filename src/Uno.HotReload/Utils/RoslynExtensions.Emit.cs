using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	public static ValueTask<ImmutableDictionary<Project, EmitResult>> EmitAsync(this Workspace workspace, CancellationToken ct)
		=> workspace.CurrentSolution.EmitAsync(ct);

	public static async ValueTask<ImmutableDictionary<Project, EmitResult>> EmitAsync(this Solution solution, CancellationToken ct)
	{
		var results = ImmutableDictionary.CreateBuilder<Project, EmitResult>();
		foreach (var project in solution.Projects)
		{
			var compilation = await project.GetCompilationAsync(ct).ConfigureAwait(false);

			var dllPath = project.OutputFilePath ?? throw new InvalidOperationException("No OutputFilePath defined.");
			if (Path.GetDirectoryName(dllPath) is { } binDir)
			{
				Directory.CreateDirectory(binDir);
			}

			await using var dll = File.Create(dllPath);
			await using var pdb = File.Create(Path.ChangeExtension(dllPath, ".pdb"));

			results.Add(project, compilation?.Emit(peStream: dll, pdbStream: pdb, cancellationToken: ct) ?? throw new InvalidOperationException($"Failed to emit project {project.Name}."));
		}

		return results.ToImmutable();
	}

	private static readonly EmitOptions _intermediateOptions = new EmitOptions()
		.WithDebugInformationFormat(DebugInformationFormat.Embedded)
		.WithEmitMetadataOnly(false)
		.WithIncludePrivateMembers(true);

	public static ValueTask<ImmutableDictionary<Project, EmitResult>> EmitCompilationOutputAsync(this Workspace workspace, CancellationToken ct)
		=> workspace.CurrentSolution.EmitCompilationOutputAsync(ct);

	/// <summary>
	/// Emits the intermediate compilation output (the EnC baseline) of the projects of the solution.
	/// </summary>
	/// <param name="solution">The solution to emit.</param>
	/// <param name="ct">Cancellation.</param>
	/// <param name="onlyMissing">
	/// When <see langword="true"/>, projects whose assembly already exists on disk are left untouched:
	/// that file is a real build (the one the running application was built from, or referenced by it)
	/// and overwriting it with a fresh compile would change its MVID, making every EnC delta
	/// target a module the application never loaded.
	/// </param>
	public static async ValueTask<ImmutableDictionary<Project, EmitResult>> EmitCompilationOutputAsync(this Solution solution, CancellationToken ct, bool onlyMissing = false)
	{
		var results = ImmutableDictionary.CreateBuilder<Project, EmitResult>();
		foreach (var project in solution.Projects)
		{
			if (onlyMissing && File.Exists(project.CompilationOutputInfo.AssemblyPath))
			{
				continue;
			}

			var compilation = await project.GetCompilationAsync(ct).ConfigureAwait(false);

			var dllPath = project.CompilationOutputInfo.AssemblyPath ?? throw new InvalidOperationException("No CompilationOutputInfo.AssemblyPath defined.");
			if (Path.GetDirectoryName(dllPath) is { } objDir)
			{
				Directory.CreateDirectory(objDir);
			}
			await using var dll = File.Create(dllPath);

			results.Add(project, compilation?.Emit(peStream: dll, options: _intermediateOptions, cancellationToken: ct) ?? throw new InvalidOperationException($"Failed to emit project {project.Name}."));
		}

		return results.ToImmutable();
	}

	public static void EnsureSuccess(this ImmutableDictionary<Project, EmitResult> emitResult)
	{
		var failures = emitResult
			.Where(p => p.Value.Success is false)
			.Select(p => new CompilationErrorException($"Compilation of {p.Key.FilePath} failed.", p.Value.Diagnostics) as Exception)
			.ToArray();

		switch (failures.Length)
		{
			case 1: throw failures[0];
			case > 1: throw new AggregateException("Multiple compilation failures.", failures);
		}
	}
}
