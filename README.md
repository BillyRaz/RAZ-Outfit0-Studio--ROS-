# RAZ Outfit Studio (ROS)

RAZ Outfit Studio is a Unity editor and runtime outfit workflow for building, previewing, generating, and swapping character outfits with a lightweight UI pipeline.

This project is designed for:
- generating a root character prefab from a mesh
- turning the base character mesh into a default outfit
- creating additional `OutfitData` assets and outfit prefabs
- generating a runtime outfit UI with outfit cards and preview support
- saving the selected default outfit back to the root prefab asset

## Creator

Created by `RAZ`.

## Version

Current public release: `v1.0`

## Main Features

- Root prefab generation from a skinned mesh
- Auto-generated default outfit from the root mesh
- Outfit prefab and `OutfitData` workflow
- Runtime outfit swapping through `OutfitManager`
- Runtime UI generation with outfit library cards
- Auto-link and repair tools for scene UI
- Deep scan and UI diagnostics
- Prefab asset syncing so selected default outfits persist
- Fallback runtime player creation for isolated test scenes

## Folder Layout

- `Assets/RAZ Outfit Studio/Characters`
- `Assets/RAZ Outfit Studio/Outfits`
- `Assets/RAZ Outfit Studio/OutfitData`
- `Assets/RAZ Outfit Studio/UI`
- `Assets/RAZ Outfit Studio/Scripts`

## Quick Start

1. Open the Unity project.
2. Open `Tools > RAZ Outfit Studio` or the matching editor window entry used in this project.
3. In the root setup area, choose your base skinned mesh and generate a root prefab.
4. Generate or create outfit prefabs and `OutfitData` assets.
5. Open the UI tab and generate the UI system.
6. Use `Auto-Link & Fix All UI`.
7. Enter Play mode and click an outfit card to equip it.
8. The selected outfit is saved as the root prefab's default outfit.

## Runtime Flow

- `OutfitManager` equips outfits under `OutfitRoot`
- outfit renderers are rebound to the character skeleton
- the selected outfit becomes the saved default outfit on the root prefab
- next time the character loads, the saved default outfit is used

## Public Repo Notes

- This is a `v1.0` public release
- `Close` and `Refresh` UI buttons may still need manual `OnClick` hookup in some generated scene setups
- outfit card clicks are wired at runtime, so they do not appear as persistent inspector events
- sample scene content may be project-specific and can be replaced in your own game

## Documentation

- [User Guide](./USER_GUIDE.md)
- [Changelog](./CHANGELOG.md)
- [License](./LICENSE)

## Known Limitations

- Spawn-system-specific integration is intentionally lightweight in this isolated version
- some projects may require manual UI button event verification after prefab regeneration
- runtime-generated scene objects are intended for testing; production games should connect their own player spawn flow

## Contributing

Bug reports, fixes, and improvements are welcome. If you fork this project, please keep creator credit to `RAZ`.
