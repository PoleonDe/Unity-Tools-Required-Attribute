# Control Tools - Required Attribute

Extended required field validation tools for Unity inspectors. Add a `[Required]` attribute to serialized fields and the editor highlights missing references, empty strings, and empty collections.

## Requirements

Unity 6 / 6000.0 or newer.

## Installation

Install the package directly from GitHub with Unity Package Manager:

1. Open `Window > Package Manager` in Unity.
2. Select `+ > Install package from git URL...`.
3. Enter this URL:

```text
https://github.com/PoleonDe/Unity-Tools-Required-Attribute.git
```

You can also add the package to `Packages/manifest.json`:

```json
"com.control-tools.required-attribute": "https://github.com/PoleonDe/Unity-Tools-Required-Attribute.git"
```

To install a specific version later, create and push a Git tag, then append it to the URL, for example `#0.1.0`.

The Widget can be created by right clicking next to the play/pause widget in the top bar, and then selecting : "Control/GlobalMissingRequired".

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
- Git URL installation requires Git to be installed and available on the system path used by Unity.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
