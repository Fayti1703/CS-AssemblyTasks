using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace Fayti1703.AssemblyTasks;

public class PublishAllTypes : AssemblyTask {

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
