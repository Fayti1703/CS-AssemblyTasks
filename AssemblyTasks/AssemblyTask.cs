using System.IO;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace Fayti1703.AssemblyTasks;

abstract public class AssemblyTask : ITask {
	public IBuildEngine BuildEngine { get; set; } = null!;
	public ITaskHost HostObject { get; set; } = null!;

	[Required]
	[UsedImplicitly]
	public string SourceFilePath { get; set; } = null!;
	[Required]
	[UsedImplicitly]
	public string TargetFilePath { get; set; } = null!;


	public bool Execute() {
		using AssemblyDefinition assembly = this.ReadAssembly();
		this.HandleAssembly(assembly);
		this.WriteAssembly(assembly);
		return true;
	}

	virtual protected AssemblyDefinition ReadAssembly() {
		return AssemblyDefinition.ReadAssembly(this.SourceFilePath);
	}

	abstract protected void HandleAssembly(AssemblyDefinition assembly);

	virtual protected void WriteAssembly(AssemblyDefinition assembly) {
		string? dirPath = Path.GetDirectoryName(this.TargetFilePath);
		if(dirPath != null)
			Directory.CreateDirectory(dirPath);
		assembly.Write(this.TargetFilePath);
	}
}
