# Module Format V1

Status: V0.1 implementation, build/automatic checks and owner-confirmed manual acceptance completed on 2026-09-03. `ARCHITECTURE.md` remains the project architecture authority; its current implementation snapshot still describes the earlier Host baseline and was not changed without a separate governance request. This document and `IAddToolBoxModuleV1` describe the V1 wire/entry contract; [the development reference](MODULE_DEVELOPMENT_REFERENCE_V1.md) records measurements, not a second contract.

## Folder and metadata

One folder is one tool package. Its root contains `module.json`, its entry DLL, `.deps.json`, private managed/native dependencies, resources and licenses. The manifest is the **only metadata authority**:

```json
{
  "schemaVersion": "addtoolbox-module-v1",
  "id": "addtoolbox.background-remover",
  "displayName": "去背景",
  "version": "0.1.0",
  "kind": "tool",
  "entryAssembly": "AddToolBox.BackgroundRemover.dll",
  "entryType": "AddToolBox.BackgroundRemover.BackgroundRemoverModule"
}
```

Exactly these nonempty string fields are accepted. Unknown or duplicate fields, malformed JSON, unsupported schema/kind, unsafe Windows directory IDs, absolute/escaping entry paths, missing files, and symlinks/junctions are rejected. Paths are normalized with `Path.GetFullPath` and checked against the module root. IDs cannot contain `/`, `\`, `..`, invalid Windows filename characters, reserved device names or the `.importing-` prefix. IDs are case-sensitive in metadata and startup ordering; installation uniqueness is case-insensitive to match the Windows filesystem. An installed folder's name must exactly match its manifest ID.

## Entry contract

```csharp
public interface IAddToolBoxModuleV1
{
    Type ToolViewType { get; }
}
```

SDK targets net10.0 and references BCL only. Entry type must implement the shared SDK interface and have a public parameterless constructor. ToolViewType must be constructible with a parameterless constructor and yield a WPF FrameworkElement (not Window). No runtime Id/DisplayName/Version duplicates.

Host derives its immutable ToolDefinition projection from the manifest. A 64×64 generic tile is placed near the current viewport center using finite non-overlapping initial candidates. Placement is not a layout solver and does not affect later drag or zoom. The cached view is created only on the first Quick Click, shown in the existing Tool Host, detached on Back and reused on reopening.

## Build and dependency boundary

Modules are independent `net10.0-windows` WPF projects, outside the host solution; current package target is Windows x64. Set `EnableDynamicLoading=true`. SDK ProjectReference must use `Private=false` and `ExcludeAssets=runtime`. Never package a private SDK DLL. No App/Core/UI/Infrastructure reference is needed or allowed by this milestone.

Each module receives a collectible AssemblyLoadContext and AssemblyDependencyResolver for managed and native dependencies. SDK and installed .NET/WPF framework assemblies use the Default context. Private managed dependencies must resolve inside the module package; missing private dependencies fail explicitly. Standard resource culture probing and OS resolution of system native DLLs retain platform behavior. Collectible does **not** promise immediate unload: WPF, native dependencies and cached views can retain references. V1 has no hot unload UI.

## Import and discovery

Installed root: `%LOCALAPPDATA%\addToolBox\Modules\<id>`. Source and installed package are distinct.

Folder picker → validate → show in-process trust warning → user confirms → duplicate check → copy to `.importing-<guid>` → revalidate identical manifest → atomic directory move → load. Copy/validation failures delete the temporary directory; cleanup failures are reported with the path. Existing installations are never overwritten. If loading fails after the atomic move, the complete installed package remains and the user is explicitly told that installation succeeded but loading failed; there is no partial package or automatic deletion of installed user data.

Startup reads only immediate installed directories, ignores `.importing-*`, then loads valid manifests by ordinal ID. Failure of an individual discovery/load is collected in one startup summary. Import/open errors are shown at their operation boundary. A module can display its own recoverable image/inference/save failures.

## Trust and V1 limits

Import warning:

> 模组将在 addToolBox 进程内运行。模组代码可以访问当前用户权限范围内的资源。仅导入你信任的模组。

AssemblyLoadContext is **not a sandbox**. Host and module share process permissions, resources and native-crash risk. Recoverable loading exceptions can be reported; arbitrary malicious code, process exit, unhandled module callbacks or native crashes are not isolated by ALC. Only trusted modules may be imported.

One entry per module. No store, updates, signatures, permission model, dependency version resolver, widget/window contract, hot reload/uninstall UI, or layout persistence. Views and heavy sessions remain cached until process exit; memory lifecycle changes require future measured design work.

## Distribution Container / Transport Packaging

Owner direction recorded on 2026-09-04: `.atbmod` is a ZIP-compatible transport container for module distribution. Background Remover packaging support is **IMPLEMENTED**; Host `.atbmod` import is **NOT YET IMPLEMENTED**. This section adds transport packaging only and does not change Runtime Format V1 semantics.

An archive carries the complete folder package's relative file layout. `module.json` and the entry assembly are at archive root, with no `package-<version>/` or module-name wrapper directory. Entry paths use `/`, must be relative, and cannot be empty, duplicated (including Windows case aliases), contain a drive/absolute path or traverse via `..`. The existing resource/native dependency layout is preserved; a `runtimes/` directory is included only when the actual folder package uses one.

The Background Remover writer uses .NET `System.IO.Compression.ZipArchive`, `CompressionLevel.Optimal`, ordinal entry order and a fixed legal ZIP timestamp. It validates the explicit release asset list and model hash, reopens the container, then compares retained staging and extracted roundtrip files by path, count, size and SHA256. A separate `.atbmod.sha256` file describes the archive bytes. SHA verification detects content changes but does not establish publisher identity or trust.

Folder package = development artifact; `.atbmod` = distribution artifact. Runtime layout remains unchanged after extraction. `module.json` remains the SSOT. The transport container does not change Module Contract, SDK ABI, runtime permissions or the security boundary. Assemblies are not loaded directly from ZIP; existing AssemblyLoadContext / AssemblyDependencyResolver behavior is unchanged.

The current Host still imports a complete folder and has no `.atbmod` picker, installer or update flow. Packaging does not copy into `%LOCALAPPDATA%\addToolBox\Modules\<id>` or overwrite installed modules. An archive is not a security sandbox: module code still runs in the Host process with the current user's permissions. Future direct single-file import requires a separate authorized task.
