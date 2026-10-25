# RealmShift

First playable blockout of a Banjo-Kazooie-style 3D platformer. Two scenes, flat-color placeholders, no combat or collectibles.

Unity **6000.3.27f1** (6000.3 LTS) with the Universal Render Pipeline.

## Open and play

1. Open this folder as a Unity project (6000.3.x). A Personal (or other valid) editor license has to be activated; batchmode exits immediately without one.
2. If `Assets/Scenes/UnstitchedPalace.unity` is not there yet, run **RealmShift > Build All** once. That writes both scenes, the URP asset, and the build settings.
3. Open `Assets/Scenes/UnstitchedPalace.unity`.
4. Press Play.

The player stands in the palace courtyard. Walk north across the bridge into the flickering hall. The east doorway labeled **Frostfell Fair** (the south-most of the four east doors) loads the fair. The other seven doorways are closed. At the fair, the **TO PALACE** arch beside the entrance gate loads the hall again, just inside that door.

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move | WASD or arrows | Left stick |
| Orbit camera | Mouse (click to lock, Esc to unlock) | Right stick |
| Sprint | Shift | LB or left-stick click |
| Jump | Space (release early for a shorter hop) | South button |

Movement is camera-relative. Falling below the level's kill height respawns at the last safe ground, or at the level spawn.

## Rebuild the scenes

Everything in the scenes is placed by editor scripts. Change sizes and positions in:

- `Assets/Scripts/Editor/HubLayout.cs`
- `Assets/Scripts/Editor/FrostfellLayout.cs`

Then use **RealmShift > Build Hub**, **RealmShift > Build Frostfell Fair**, or **RealmShift > Build All**.

From a shell, with the project closed:

```bash
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod RealmShift.EditorTools.RealmShiftBuild.BuildAllBatch \
  -logFile Logs/build.log
```

Scenes are saved as text (`Assets/Scenes/UnstitchedPalace.unity` and `Assets/Scenes/FrostfellFair.unity`).

## What is in the blockout

**Unstitched Palace** (~150 m): cobbled courtyard, three colored towers, a pond and a moat with a bridge, then the flickering hall (eight labeled world doors), collection room, throne room, remix gallery, and the round true-bodies room. A short iced side passage sits off the east courtyard.

**Frostfell Fair** (~200 m of rides inside a 240 m terrain bowl): entrance gate, tents, carousel with an ice ring, Ferris wheel, a static ice-luge track, a walkable 30 m ice maze, barker's stage, spiral skate channels, and food stalls around a fire pit. The mountain rim is the boundary. The night-carnival view is not built.
