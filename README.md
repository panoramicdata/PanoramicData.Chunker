[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[![NuGet version](https://img.shields.io/nuget/v/PanoramicData.Chunker.svg)](https://www.nuget.org/packages/PanoramicData.Chunker/)

[![Codacy Badge](https://app.codacy.com/project/badge/grade/PanoramicData.Chunker)](https://app.codacy.com/gh/panoramicdata/PanoramicData.Chunker/dashboard)

# PanoramicData.Chunker

> **Need help with APIs or .NET?** Panoramic Data are experts in APIs and .NET, and we can help with your development. [Contact us](https://www.panoramicdata.com/contact-us) or see our [software services](https://www.panoramicdata.com/services/software).

A chunker for common document formats.

Supports .NET 11.0.

## Breaking changes

- `EntityPatternLoader.DefaultPatternsPath` and `RelationshipPatternLoader.DefaultPatternsPath` are now static read-only properties instead of constants. Code compiled against earlier versions must be recompiled.
- The serialisation property order of `ChunkingStatistics` and `RelationshipMetadata` changed (the properties themselves did not).
- `HierarchyBuilder.ValidateHierarchy(chunks, out errors)` is replaced by `HierarchyBuilder.ValidateHierarchy(chunks)`, which returns an `IntegrityCheckResult` with `IsValid` and `Errors`. The `out` overload is removed.
- `Graph.Validate(out errors)` is replaced by `Graph.Validate()`, which returns an `IntegrityCheckResult` with `IsValid` and `Errors`. The `out` overload is removed.
