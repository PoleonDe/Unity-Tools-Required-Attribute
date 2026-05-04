---
name: required-attribute
description: Use this skill when working with the Control Tools Required Attribute Unity package, especially when adding required-field validation, explaining package behavior, or changing its Unity Editor tooling.
---

# Control Tools Required Attribute

## When to Use This Plugin

Use `com.control-tools.required-attribute` when a Unity project needs editor-time validation for serialized fields that must be assigned before content is considered ready. It is useful for MonoBehaviours, prefabs, and ScriptableObjects where missing references, blank text, or empty collections should be visible in the Inspector and discoverable from editor UI.

Reach for this plugin when:

- A serialized object reference must be assigned in the Inspector.
- A serialized string must not be empty.
- A serialized array, list, or other collection must contain at least one element.
- A team wants missing setup to be surfaced during authoring instead of failing later at runtime.
- You need a lightweight validation attribute without adding runtime validation systems.

Do not use this plugin as the only guard for runtime-critical invariants. The package is intended for Unity Editor feedback; runtime code only defines the `RequiredAttribute`.

## Requirements

- Unity 6 / `6000.0` or newer.
- Add `using Control.Tools;` in scripts that use `[Required]`.
- The attribute must be applied to serialized fields.

## Basic Usage

```csharp
using Control.Tools;
using UnityEngine;

public sealed class RequiredExample : MonoBehaviour
{
    [Required]
    [SerializeField] private GameObject target;

    [Required]
    [SerializeField] private string displayName;
}
```

Public fields can also be annotated:

```csharp
[Required]
public GameObject target;
```

## What Gets Flagged

The editor validation treats these values as missing:

- `null` managed values.
- Missing `UnityEngine.Object` references.
- Empty strings.
- Empty arrays, lists, and collections.
- Empty enumerable values where the value can be inspected.
- Missing managed references and exposed references in serialized properties.

Nullable value types are not treated as missing by the object-value validator.

## Editor Behavior

The package includes editor-only tooling that:

- Draws a warning icon beside missing `[Required]` fields in the Inspector.
- Highlights a property when navigating to an issue.
- Scans loaded scenes, prefab stages, prefab assets, and ScriptableObject assets.
- Refreshes after hierarchy, scene, prefab stage, undo/redo, and property changes.
- Provides a refresh command at `Tools/Control/Required/Refresh Issues`.
- Shows issue counts through the Unity 6 toolbar or Scene View overlay depending on the Unity version.

## Implementation Notes

- Runtime API lives in `Runtime/RequiredAttribute.cs`.
- Editor validation and UI live under `Editor/`.
- The optional `[Required("message")]` constructor stores a message, but the current editor UI does not display it.
- Validation is compiled only for `UNITY_EDITOR` and Unity 6-or-newer symbols.

## When Editing This Plugin

Follow the package's existing split:

- Keep the runtime assembly minimal and free of UnityEditor references.
- Put inspector drawing, scanning, toolbar, popup, and asset database logic in the editor assembly.
- Preserve Unity version guards around toolbar APIs because Unity 6.0 and 6.3 use different integrations.
- Prefer `SerializedProperty` checks first, then reflection-backed value inspection for cases Unity's serialized property API cannot fully describe.
- Update `README.md`, `Documentation~/index.md`, or `Samples~/Basic Usage` when changing public usage or visible behavior.
