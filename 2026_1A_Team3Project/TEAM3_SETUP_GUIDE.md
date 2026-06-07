# Team3 Unity Scene Guide

Do not use scene auto-generation anymore.

Use the scene files that already exist in `Assets/Scenes`.

## Main Scenes

- `Assets/Scenes/TitleScene.unity`
- `Assets/Scenes/ChapterSelectScene.unity`
- `Assets/Scenes/BattleScene.unity`
- `Assets/Scenes/GameOverScene.unity`

Ignore `Battle_Demo.unity`. It was only for early testing.

## Input Setting

The project input mode is set to `Both`.

This prevents the Unity UI error:

`You are trying to read Input using the UnityEngine.Input class...`

If Unity still shows that error, restart the Unity Editor once.

Manual check:

`Edit > Project Settings > Player > Active Input Handling > Both`

## Test Order

1. Open `TitleScene`.
2. Press Play.
3. Click `Game Start`.
4. Confirm it opens `ChapterSelectScene`.
5. Click `Chapter 1`.
6. Confirm it opens `BattleScene`.
7. In `BattleScene`, test `Merge`, `Craft`, `Play`, and `End Turn`.

## Scene Layout

`TitleScene`

- Fullscreen title image
- Bottom center buttons: Start, Settings, Quit

`ChapterSelectScene`

- Fullscreen chapter book image
- Chapter 1 button
- Chapter 2 and 3 locked placeholders

`BattleScene`

- Top center: enemy HP, weakness, shield
- Center: enemy character
- Bottom left: player HP and guard
- Bottom center: scroll hand
- Bottom right: merge resource storage
- Center bottom: oven craft area
- Bottom: Merge, Craft, Play, End Turn buttons

`GameOverScene`

- Fullscreen game over image
- Retry button
- Title button

## Minimum Submission Goal

1. Title goes to chapter select.
2. Chapter 1 goes to battle.
3. Battle buttons advance one turn.
4. Player HP, enemy HP, cost, hand, and resources visibly change.

