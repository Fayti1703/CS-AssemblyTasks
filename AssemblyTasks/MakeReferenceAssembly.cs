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
		foreach(ModuleDefinition module in assembly.Modules)
			FilterCollection(module.Types, this.HandleType);

		MethodReference refAsmAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor([]));
		assembly.CustomAttributes.Add(new CustomAttribute(refAsmAttrCtor));
	}

	private static void FilterCollection<T>(Collection<T> collection, Func<T, bool> body) {
		for(int i = 0; i < collection.Count; i++) {
			if(body(collection[i])) continue;

			collection.RemoveAt(i);
			i--;
		}
	}

	private bool HandleType(TypeDefinition type) {
		if(this.OnlyPublic) {
			if(
				!type.IsPublic &&
				/* We need to preserve any compiler-injected types, which are marked with `[Embedded]`.
				 * (this is usually `EmbeddedAttribute` itself, `NullableAttribute` and `NullableContextAttribute`)
				 * Failing to do this breaks newer .NET assemblies that have these.
				 */
				type.CustomAttributes.All(x => x.AttributeType.FullName != "Microsoft.CodeAnalysis.EmbeddedAttribute")
			) return false;
		}

		FilterCollection(type.NestedTypes, this.HandleType);
		FilterCollection(type.Methods, this.HandleMethod);
		FilterCollection(type.Fields, this.HandleField);
		return true;
	}

	private bool HandleMethod(MethodDefinition method) {
		if(this.OnlyPublic && !method.IsPublic) return false;

		if(!method.HasBody)
			return true;

		method.Body = new MethodBody(method);
		foreach(Instruction instruction in emptyBody)
			method.Body.Instructions.Add(instruction);

		return true;
	}

	private bool HandleField(FieldDefinition field) {
		if(this.OnlyPublic && !field.IsPublic) return false;

		// do nothing; all we really do with fields is conditionally remove them
		return true;
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
