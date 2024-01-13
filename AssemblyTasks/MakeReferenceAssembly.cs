using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Fayti1703.AssemblyTasks;

public class MakeReferenceAssembly : ITask  {
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
		foreach(TypeDefinition type in assembly.Modules.SelectMany(module => module.Types))
			EmptyTypeImplementation(type);

		MethodReference refAsmAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor([]));
		assembly.CustomAttributes.Add(new CustomAttribute(refAsmAttrCtor));

		string? dirPath = Path.GetDirectoryName(this.TargetFilePath);
		if(dirPath != null)
			Directory.CreateDirectory(dirPath);
		assembly.Write(this.TargetFilePath);
		this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
			"Wrote reference assembly to '{0}'",
			null!,
			nameof(MakeReferenceAssembly),
			MessageImportance.Low,
			DateTime.Now,
			this.TargetFilePath
		));
		return true;
	}

	private static readonly Collection<Instruction> emptyBody = [
		Instruction.Create(OpCodes.Ldnull),
		Instruction.Create(OpCodes.Throw)
	];

	private static void EmptyTypeImplementation(TypeDefinition type) {
		foreach(MethodDefinition method in type.Methods.Where(method => method.HasBody)) {
			method.Body = new MethodBody(method);
			foreach(Instruction instruction in emptyBody)
				method.Body.Instructions.Add(instruction);
		}

		foreach(TypeDefinition nestedType in type.NestedTypes) {
			EmptyTypeImplementation(nestedType);
		}
	}
}
