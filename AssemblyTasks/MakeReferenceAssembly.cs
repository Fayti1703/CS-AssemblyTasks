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
	
	[UsedImplicitly]
	public bool OnlyPublic { get; set; } = false;

	override protected void HandleAssembly(AssemblyDefinition assembly) {
		foreach (ModuleDefinition module in assembly.Modules)
			foreach (TypeDefinition type in module.Types.ToList())
				HandleType(type, module.Types);

		MethodReference refAsmAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor([]));
		assembly.CustomAttributes.Add(new CustomAttribute(refAsmAttrCtor));
	}

	private void HandleType(TypeDefinition type, Collection<TypeDefinition> collection) {
		if (this.OnlyPublic && !type.IsPublic) {
			collection.Remove(type);
			return;
		}

		foreach (TypeDefinition nestedType in type.NestedTypes.ToList())
			HandleType(nestedType, type.NestedTypes);
		
		foreach (MethodDefinition method in type.Methods.ToList())
			HandleMethod(method, type.Methods);
		
		foreach (FieldDefinition field in type.Fields.ToList())
			HandleField(field, type.Fields);
	}

	private void HandleMethod(MethodDefinition method, Collection<MethodDefinition> collection) {
		if (this.OnlyPublic && !method.IsPublic) {
			collection.Remove(method);
			return;
		}

		if (!method.HasBody)
			return;
		
		method.Body = new MethodBody(method);
		foreach (Instruction instruction in emptyBody)
			method.Body.Instructions.Add(instruction);
	}

	private void HandleField(FieldDefinition field, Collection<FieldDefinition> collection) {
		if (this.OnlyPublic && !field.IsPublic) {
			collection.Remove(field);
			return;
		}
		
		// do nothing; all we really do with fields is conditionally remove them
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
}
