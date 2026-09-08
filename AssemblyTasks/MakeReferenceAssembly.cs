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
	public bool WithAllMembers { get; set; } = true;

	override protected void HandleAssembly(AssemblyDefinition assembly) {
		foreach(ModuleDefinition module in assembly.Modules) {
			this.FilterInternalAttributes(module.CustomAttributes);
			FilterCollection(module.Types, this.HandleType);
		}

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
		if(!this.WithAllMembers && IsPubliclyRemovable(type)) return false;

		this.FilterInternalAttributes(type.CustomAttributes);
		foreach(InterfaceImplementation iImpl in type.Interfaces) {
			this.FilterInternalAttributes(iImpl.CustomAttributes);
		}

		FilterCollection(type.NestedTypes, this.HandleType);
		FilterCollection(type.Methods, this.HandleMethod);
		FilterCollection(type.Fields, this.HandleField);
		return true;
	}

	private bool HandleMethod(MethodDefinition method) {
		if(
			!this.WithAllMembers &&
			(method.IsAssembly || method.IsFamilyAndAssembly || method.IsPrivate) &&
			method.Module.EntryPoint != method
		) return false;

		this.FilterInternalAttributes(method.CustomAttributes);

		if(!method.HasBody)
			return true;

		method.Body = new MethodBody(method);
		foreach(Instruction instruction in emptyBody)
			method.Body.Instructions.Add(instruction);

		return true;
	}

	private bool HandleField(FieldDefinition field) {
		if(
			!this.WithAllMembers &&
			(field.IsAssembly || field.IsFamilyAndAssembly || field.IsPrivate)
		) return false;

		this.FilterInternalAttributes(field.CustomAttributes);

		// do nothing; all we really do with fields is conditionally remove them
		return true;
	}

	private void FilterInternalAttributes(Collection<CustomAttribute> attributes) {
		if(this.WithAllMembers) return; /* no need to do anything */

		FilterCollection(attributes, attribute => {
			if(!IsInternalReference(attribute.AttributeType)) return true;

			TypeDefinition attrType = attribute.AttributeType.Resolve();
			if(CheckWithAncestry(attrType, IsPubliclyRemovable)) {
				/* attribute will be removed, get rid of the reference */
				return false;
			}
			return true;
		});
	}

	private static bool CheckWithAncestry(TypeDefinition type, Func<TypeDefinition, bool> check) {
		TypeDefinition? currentType = type;
		while(currentType != null) {
			if(check(currentType)) return true;
			currentType = currentType.DeclaringType;
		}
		return false;
	}

	private static bool IsPubliclyRemovable(TypeDefinition type) {
		MethodDefinition? entryPoint = type.Module.EntryPoint;
		bool containsEntryPoint = false;
		if(entryPoint != null) containsEntryPoint = CheckWithAncestry(entryPoint.DeclaringType, x => x != type);
		return
			!type.IsPublic &&
			!containsEntryPoint &&
			/* We need to preserve any compiler-injected types, which are marked with `[Embedded]`.
			 * (this is usually `EmbeddedAttribute` itself, `NullableAttribute` and `NullableContextAttribute`)
			 * Failing to do this breaks newer .NET assemblies that have these.
			 */
			type.CustomAttributes.All(x => x.AttributeType.FullName != "Microsoft.CodeAnalysis.EmbeddedAttribute")
		;
	}

	private static bool IsInternalReference(TypeReference reference) {
		/* see Mono.Cecil.MetadataResolver.Resolve */
		IMetadataScope scope = reference.Scope;
		return scope.MetadataScopeType switch {
			/* n.b. this is likely external, but double-checking regardless */
			/* n.b. `reference.Module` is the _declaring_ module. */
			MetadataScopeType.AssemblyNameReference => reference.Module.Assembly.FullName == ((AssemblyNameReference) scope).FullName,
			MetadataScopeType.ModuleReference or MetadataScopeType.ModuleDefinition => false,
			_ => throw new NotSupportedException() /* mirror Cecil */
		};
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
