# Fayti1703's AssemblyTasks

Various MSBuild tasks to alter assemblies.

## Tasks

### PublishAllTypes

```xml
<Fayti1703.AssemblyTasks.PublishAllTypes
	SourceFilePath="path/to/assembly.dll"
	TargetFilePath="lib/public/assembly.dll"
/>
```

Reads an assembly from `SourceFilePath`, and writes a version with all
types, fields, properties and methods set to `public` at `TargetFilePath`.

### MakeReferenceAssembly

```xml
<Fayti1703.AssemblyTasks.MakeReferenceAssembly
	SourceFilePath="path/to/assembly.dll"
	TargetFilePath="lib/ref/assembly.dll"
/>
```

Create a 'full' [Reference Assembly](https://learn.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies) (also known as a "metadata-only assembly")
from an existing assembly.

All method implementations are replaced with `throw null;`.
