using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace Fayti1703.AssemblyTasks;

public class PublishAllTypes : ITask {
	public IBuildEngine BuildEngine { get; set; } = null!;
	public ITaskHost HostObject { get; set; } = null!;

	[Required]
	[UsedImplicitly]
	public string SourceFilePath { get; set; } = null!;
	[Required]
	[UsedImplicitly]
	public string TargetFilePath { get; set; } = null!;

	public bool Execute() {
		using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(this.SourceFilePath);
		foreach(TypeDefinition type in assembly.Modules.SelectMany(module => module.Types)) {
			type.IsPublic = true;
			PublishTypeContents(type);
		}

		string? dirPath = Path.GetDirectoryName(this.TargetFilePath);
		if(dirPath != null)
			Directory.CreateDirectory(dirPath);
		assembly.Write(this.TargetFilePath);
		this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
			"Wrote all-public assembly to '{0}'",
			null!,
			nameof(PublishAllTypes),
			MessageImportance.Low,
			DateTime.Now,
			this.TargetFilePath
		));
		return true;
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
