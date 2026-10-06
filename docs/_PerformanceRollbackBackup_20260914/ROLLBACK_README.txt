FATEFUL RUSH PERFORMANCE OPTIMIZATION - ROLLBACK

This folder contains the exact versions of every script before the optimization was applied.

To roll back:
1. Close Unity.
2. Copy the Assets folder from this backup folder into the project root.
3. Allow overwrite/replace for the matching files.
4. Open Unity again.

The optimization installer never places backup .cs files inside Assets, so the backup cannot create duplicate-class compile errors.

Changed areas:
- FatefulRushLocalizationRuntime: removed 20 Hz localization polling and 0.75 s scene rescans.
- LocalizedUILayoutPolish: removed permanent LateUpdate layout scanning; refreshes on scene/locale/options/result events.
- GameResultLocalizationGuard: removed 20 Hz localization polling and 1 s result scans.
- MissionBriefingLiveSync: removed permanent LateUpdate refresh.
- LevelSelectPanel: reuses a prewarmed button pool instead of Destroy/Instantiate on every page.
- PlayerSkinPanelUI: reuses PointerEventData instead of allocating one per swipe start.
- OptionsUI / GameResultUI / MissionBriefingPanelUI: explicit refresh events were added.
