# Required Attribute

## Setup

Install the package through Unity Package Manager with a Git URL or the local `file:` path used by the test project.

## Usage

Add `using Control.Tools;` and annotate serialized fields with `[Required]`. Missing object references, empty strings, and empty arrays or lists are highlighted in the inspector and summarized in the editor toolbar.

## Notes

Runtime code contains only the attribute definition. Validation, drawing, scanning, and toolbar integration compile in the Editor assembly only.
