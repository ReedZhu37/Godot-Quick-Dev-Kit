# Quick Level Kit C# tests

Run from the repository with Godot .NET and the matching SDK:

```sh
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/run_csharp_tests.sh
```

For an offline/local package feed, also set `NUGET_CONFIG=/absolute/path/to/NuGet.Config`. The harness restores, builds with warnings as errors, and runs a headless editor import before executing scenes.

The dependency-free scene runner returns a nonzero exit status for any failed assertion or exception. The script builds the project and uses temporary HOME and XDG directories, so it does not overwrite normal game saves. It runs the main suite and then separate C# writer/reader Godot processes to verify persisted identifiers and nested custom state survive a process restart. Between the C# writer and reader, two GDScript processes read the C# save and rewrite the same schema. This verifies bidirectional progress compatibility without implying C# and GDScript resource scripts can be substituted. Both C# persistence processes also save/load a `.tres` catalog graph with typed definitions, scenes, metadata, shared flows, base and all five derived stage resources, and keyed routes. An uncached in-process graph load additionally checks independent editing. The tests use only native C# runtime types, not GDScript adapters.

Coverage: catalog navigation and validation; ordered and keyed routes; default flow; lifecycle hooks and signals; queued synchronous transitions; context and runtime isolation; reentrant stop/load; delayed preparation disconnection; progress deep copies, corrupt and wrong-shaped save data; manager advance/reload/retry/reset/continue; synchronous completion; editor catalog manipulation; plugin and example resources.

The same harness also runs all nine retained GDScript test scripts after migration.

Additional isolated checks:

```sh
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/run_isolated_csharp.sh
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/editor_lifecycle_tests.sh
```

The first copies only the C# module to a fresh project, builds Debug/ExportRelease, runs the runtime suite, and starts the sample with its real autoload. The second uses real headless editor enable/disable/restart cycles, foreign-autoload conflicts, language switching, and Inspector → installed Dock → resource-save/reload. Headless editor checks do not verify rendered layout or target-platform input.
