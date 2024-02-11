using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Pdb;

namespace Fayti1703.AssemblyTasks;

[UsedImplicitly]
public class PublishAllTypes : AssemblyTask {

	[UsedImplicitly]
	public string? SourcePdbPath { get; set; } = null;
	[UsedImplicitly]
	public string? TargetPdbPath { get; set; } = null;

	private Stream? inPdbStream = null;
	private Stream? outPdbStream = null;


	override protected bool PreExecute() {
		if((this.SourcePdbPath == null) != (this.TargetPdbPath == null)) {
			string propExists = this.SourcePdbPath != null ? "SourcePdbPath" : "TargetPdbPath";
			string propMissing = this.SourcePdbPath == null ? "SourcePdbPath" : "TargetPdbPath";
			this.LogTaskError(
				"config",
				"FYA1001",
				$"If one of `SourcePdbPath` or `TargetPdbPath` is provided, both must be provided. Specify {propMissing} or delete {propExists}.",
				null
			);
			return false;
		}

		return true;
	}

	override protected ReaderParameters GetAssemblyReadParameters() {
		if(this.SourcePdbPath != null) {
			this.inPdbStream = new FileStream(this.SourcePdbPath, FileMode.Open);
			return new ReaderParameters {
				ReadSymbols = true,
				SymbolStream = this.inPdbStream,
				SymbolReaderProvider = new PdbReaderProvider()
			};
		}

		return base.GetAssemblyReadParameters();
	}

	override protected WriterParameters GetAssemblyWriteParameters() {
		if(this.TargetPdbPath != null) {
			this.outPdbStream = new FileStream(this.TargetPdbPath, FileMode.Create);
			return new WriterParameters {
				WriteSymbols = true,
				SymbolStream = this.outPdbStream,
				SymbolWriterProvider = new PdbWriterProvider()
			};
		}

		return base.GetAssemblyWriteParameters();
	}

	override protected void Cleanup() {
		this.inPdbStream?.Dispose();
		this.outPdbStream?.Dispose();
	}


	override protected void HandleAssembly(AssemblyDefinition assembly) {
		foreach(TypeDefinition type in assembly.Modules.SelectMany(module => module.Types)) {
			type.IsPublic = true;
			PublishTypeContents(type);
		}
	}

	override protected void WriteAssembly(AssemblyDefinition assembly) {
		base.WriteAssembly(assembly);
		this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
			"Wrote all-public assembly to '{0}'",
			null!,
			nameof(PublishAllTypes),
			MessageImportance.Low,
			DateTime.Now,
			this.TargetFilePath
		));
	}

	private static void PublishTypeContents(TypeDefinition type)
	{
		foreach (FieldDefinition field in type.Fields) field.IsPublic = true;
		foreach (MethodDefinition method in type.Methods) method.IsPublic = true;
		foreach (TypeDefinition nestedType in type.NestedTypes) {
			nestedType.IsNestedPublic = true;
			PublishTypeContents(nestedType);
		}
	}
}
