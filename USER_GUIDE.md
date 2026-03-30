# User Guide

## Overview

RAZ Outfit Studio helps you build a character outfit workflow around a root prefab, outfit prefabs, outfit data assets, and a generated runtime UI.

## Core Concepts

### Root Prefab

The root prefab is the main player character object. It keeps:
- the animator
- the base skeleton
- the `OutfitManager`
- the `OutfitRoot` where outfits are attached

### Default Outfit

The first or base character mesh can also be treated as an outfit. ROS auto-generates a default outfit from the root mesh so the system can treat the base look and custom outfits the same way.

### Outfit Prefab

An outfit prefab contains the visual outfit mesh data that gets equipped on the character.

### OutfitData

`OutfitData` is the asset used by the editor and UI. It stores:
- outfit name
- description
- preview sprite
- outfit prefab

## Typical Setup

1. Prepare a skinned mesh with a compatible skeleton.
2. Generate the root prefab from the mesh.
3. Let ROS create the default outfit from that root mesh.
4. Add more outfits as prefabs and create matching `OutfitData` assets.
5. Generate the UI.
6. Auto-link and test in Play mode.

## Generating the Root Prefab

Use the root generation tool to:
- create `CharacterName_Root.prefab`
- attach `OutfitManager`
- create `OutfitRoot`
- assign the base mesh
- auto-create `CharacterName_DefaultOutfit`

## Creating Outfits

For each outfit:
1. Create or import the outfit prefab.
2. Create an `OutfitData` asset.
3. Assign the outfit prefab.
4. Assign or capture a preview sprite.

## Generating the UI

The generated UI includes:
- left panel for current outfit display
- right panel for outfit library cards
- close and refresh buttons

Recommended flow:
1. Generate and show the UI.
2. Click `Auto-Link & Fix All UI`.
3. Run the detailed UI diagnostic if something looks wrong.

## How Equipping Works

When you click an outfit card:
- the UI passes the selected `OutfitData` to `OutfitManager`
- the outfit prefab is instantiated under `OutfitRoot`
- skinned mesh renderers are rebound to the character skeleton
- the selected outfit becomes the saved default outfit for the root prefab asset

## Spawned Player Support

In isolated test scenes, ROS can create or find a runtime player fallback.

In a larger game:
- your game can spawn the player normally
- the UI can auto-link to the spawned player's `OutfitManager`
- the saved root prefab default outfit still acts as the starting outfit

## Diagnostics

Use the diagnostic tools to inspect:
- root prefab references
- UI structure
- button container and prefab setup
- outfit asset counts
- generated card counts
- missing previews or missing outfit prefabs

## Known Manual Checks

- verify `CloseButton` and `RefreshButton` `OnClick` events in generated scene instances if needed
- verify preview sprites are assigned for each outfit
- confirm bone names match if an outfit binds incorrectly

## Recommended Production Workflow

- keep one clean root prefab per character
- treat the base mesh as the default outfit
- store all outfit prefabs and `OutfitData` assets under the ROS folders
- test each outfit in Play mode before shipping
- use diagnostics before moving the package into a larger project
