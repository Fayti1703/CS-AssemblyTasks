using System;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Fayti1703.AssemblyTasks;

[UsedImplicitly]
public class MakeReferenceAssembly : AssemblyTask {

	override protected void HandleAssembly(AssemblyDefinition assembly) {
		foreach(TypeDefinition type in assembly.Modules.SelectMany(module => module.Types))
			EmptyTypeImplementation(type);

		MethodReference refAsmAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor([]));
		assembly.CustomAttributes.Add(new CustomAttribute(refAsmAttrCtor));
	}

	override protected void WriteAssembly(AssemblyDefinition assembly) {
		base.WriteAssembly(assembly);
		this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
			"Wrote reference assembly to '{0}'",
			null!,
			nameof(MakeReferenceAssembly),
			MessageImportance.Low,
			DateTime.Now,
			this.TargetFilePath
		));
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
