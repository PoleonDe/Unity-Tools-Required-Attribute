# Control Tools - Required Attribute

Extended required field validation tools for Unity inspectors. Add a `[Required]` attribute to serialized fields and the editor highlights missing references, empty strings, and empty collections.

## Requirements

Unity 6 / 6000.0 or newer.

## Installation

Install from a Git URL:

```text
https://github.com/OWNER/com.control-tools.required-attribute.git#0.1.0
```

Install from a local file path during development:

```json
"com.control-tools.required-attribute": "file:../../com.control-tools.required-attribute"
```

## Basic Usage

```csharp
using Control.Tools;
using UnityEngine;

public sealed class RequiredExample : MonoBehaviour
{
    [Required]
    public GameObject target;
}
```

## Folder Structure

- `Runtime/`: the runtime `RequiredAttribute` API.
- `Editor/`: Unity Editor validation, toolbar, drawer, and icon tooling.
- `Tests/`: package test folders.
- `Samples~/Basic Usage/`: importable sample content.
- `Documentation~/`: setup and usage notes.

## Known Limitations

- Validation runs in the Unity Editor only.
- The optional message constructor is stored on the attribute but is not currently displayed in the editor UI.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
