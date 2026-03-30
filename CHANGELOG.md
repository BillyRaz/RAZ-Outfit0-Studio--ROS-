# Changelog

## v1.0 - 2026-03-30

Initial public release of RAZ Outfit Studio.

### Added

- Root prefab generation from a skinned mesh
- Auto-generated default outfit from the root mesh
- Outfit prefab and `OutfitData` asset workflow
- Runtime `OutfitManager` equipping and skeleton rebinding
- Runtime outfit UI generation
- Outfit card generation with preview images and names
- Auto-link and fix tools for generated UI
- UI detailed diagnostics and deep scan improvements
- Fallback runtime player creation for isolated scenes
- Prefab asset syncing for saved default outfits

### Improved

- Better outfit card creation and container refresh behavior
- Better close and refresh button recovery in UI auto-fix
- Better outfit persistence to the root prefab asset
- Better handling of generated default/base outfits

### Fixed

- UI button generation issues
- Missing outfit card generation in the runtime UI
- Scene UI reference repair flow
- Duplicate outfit card refresh issues
- Skeleton rebinding for equipped outfit meshes
- Prefab asset save errors when saving selected outfits
- Root prefab visual syncing for the selected default outfit
- Startup behavior that previously overwrote the saved outfit with the old default

### Known Issues

- `CloseButton` and `RefreshButton` may still require manual `OnClick` verification in some generated scene setups
- Some sample-scene-only content may not match a production spawn flow
