using System;
using System.IO;
using System.Text;
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
		try {
			using AssemblyDefinition assembly = this.ReadAssembly();
			this.HandleAssembly(assembly);
			this.WriteAssembly(assembly);
			return true;
		} catch(Exception e) {
			this.LogExceptionError(e);
			return false;
		}
	}

	protected void LogExceptionError(Exception exception, string? errorCode = null) {
		if(exception is AggregateException aggregate) {
			foreach(Exception inner in aggregate.Flatten().InnerExceptions) {
				this.LogExceptionError(inner);
			}
		}

		bool showTraces = Environment.GetEnvironmentVariable("FAY_TASK_EXCEPTION_TRACE") != null;
		StringBuilder messageBuilder = new(300);
		do {
			messageBuilder.Append(exception.GetType().Name).Append(": ").Append(exception.Message);
			if(showTraces)
				messageBuilder.Append("\n").Append(exception.StackTrace);
			if(exception.InnerException != null) {
				messageBuilder.Append("\nInner Exception:\n");
				exception = exception.InnerException;
			} else
				break;
		} while(exception != null);

		this.LogTaskError(
			null,
			errorCode ?? "FYA1000",
			messageBuilder.ToString(),
			null
		);
	}

	protected void LogTaskError(string? subCategory, string errorCode, string message, string? helpKeyword) {
		this.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(
			subCategory,
			errorCode,
			this.BuildEngine.ProjectFileOfTaskNode,
			this.BuildEngine.LineNumberOfTaskNode,
			this.BuildEngine.ColumnNumberOfTaskNode,
			0,
			0,
			message,
			helpKeyword,
			this.GetType().Name,
			DateTime.Now
		));
	}

	virtual protected AssemblyDefinition ReadAssembly() {
		ReaderParameters readerParams = this.GetAssemblyReadParameters();
		return AssemblyDefinition.ReadAssembly(this.SourceFilePath, readerParams);
	}

	virtual protected ReaderParameters GetAssemblyReadParameters() {
		return new ReaderParameters();
	}

	abstract protected void HandleAssembly(AssemblyDefinition assembly);

	virtual protected void WriteAssembly(AssemblyDefinition assembly) {
		string? dirPath = Path.GetDirectoryName(this.TargetFilePath);
		if(dirPath != null)
			Directory.CreateDirectory(dirPath);
		WriterParameters writerParams = this.GetAssemblyWriteParameters();
		assembly.Write(this.TargetFilePath, writerParams);
	}

	virtual protected WriterParameters GetAssemblyWriteParameters() {
		return new WriterParameters();
	}
}
