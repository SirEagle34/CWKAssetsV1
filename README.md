// Disable auto screen recording in PvP
PlayerPrefs.SetInt("cwk.screenRecordingEnabled", 0);
[![Unity](https://img.shields.io/badge/Unity-2022.3.16f1-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-Script-239120?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Status](https://img.shields.io/badge/Status-Active_Development-yellow)](https://github.com)

Main repository: `https://github.com/Lu2312/Cardwarskingdom-revived.git`

This project contains gameplay, assets, tooling, and mobile recording work for Card Wars Kingdom. The main goal of this README is to make team collaboration simple and safe, especially for teammates with little Git/GitHub experience.

Important: this project uses Git LFS for large assets. After cloning, run `git lfs pull` or textures/sprites may not load correctly.

## Table of Contents

- [Team Collaboration First](#team-collaboration-first)
- [Quick Start for New Teammates](#quick-start-for-new-teammates)
- [Daily Workflow (Recommended)](#daily-workflow-recommended)
- [Support Workflow (Lu2312 Sync from Sreagle)](#support-workflow-lu2312-sync-from-sreagle)
- [How to Resolve Merge Conflicts](#how-to-resolve-merge-conflicts)
- [Project Setup](#project-setup)
- [Current Technical Status](#current-technical-status)
- [Secondary Topic: Arabic Localization](#secondary-topic-arabic-localization)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Team Collaboration First

Collaboration rules for this repository:

- Never work directly on `main`.
- Use fixed personal branches:
	- `Lu2312` for Luis
	- `Sreagle` for teammate
- Keep commits small and descriptive.
- Open Pull Requests early when you need feedback.
- Merge only after review.

## Quick Start for New Teammates

### 1) Clone and prepare

```bash
git clone https://github.com/Lu2312/Cardwarskingdom-revived.git
cd Cardwarskingdom-revived
git lfs pull
```

### 2) Switch to your assigned branch

```bash
git checkout main
git pull origin main
git checkout Lu2312
git pull origin Lu2312
```

If you are the other teammate:

```bash
git checkout main
git pull origin main
git checkout Sreagle
git pull origin Sreagle
```

### 3) Save and upload your changes

```bash
git add .
git commit -m "feat: short description"
git push origin Lu2312
```

If you are the teammate in `Sreagle`, use the same flow:

```bash
git add .
git commit -m "feat: short description"
git push origin Sreagle
```

### 4) Open a Pull Request

1. Go to the repository on GitHub.
2. Click `Compare & pull request`.
3. Confirm base branch is `main` and compare branch is your personal branch (`Lu2312` or `Sreagle`).
4. Request review from a teammate.

## Daily Workflow (Recommended)

Use this every day before coding:

```bash
git checkout main
git pull origin main
git checkout Lu2312
git merge main
```

Teammate flow:

```bash
git checkout main
git pull origin main
git checkout Sreagle
git merge main
```

Before opening PR:

- Test in Unity.
- Verify no temporary files are included.
- Confirm only intended files are changed.

After PR merge:

```bash
git checkout main
git pull origin main
git branch -d branch-name-if-you-created-extra-branches
```

Note: do not delete `Lu2312` or `Sreagle`, because they are long-lived team branches.

## Support Workflow (Lu2312 Sync from Sreagle)

Since `Sreagle` is the most active branch and `Lu2312` is for support updates, use this safe sync process.

### Bring latest Sreagle changes into Lu2312

```bash
git checkout Lu2312
git pull origin Lu2312
git fetch origin
git merge origin/Sreagle
```

Then push your updated support branch:

```bash
git push origin Lu2312
```

If conflicts appear during merge:

```bash
git add .
git commit -m "fix: resolve Lu2312 merge from Sreagle"
git push origin Lu2312
```

Recommendation for your case:

- Prefer `merge` (not `rebase`) to keep the process simple and low-risk.
- Sync from `Sreagle` before starting support tasks.

## How to Resolve Merge Conflicts

If Git reports conflicts:

1. Open conflicted files.
2. Keep the correct final version of each block.
3. Save files.
4. Complete merge:

```bash
git add .
git commit -m "fix: resolve merge conflicts"
git push
```

## Project Setup

### Requirements

- Unity `2022.3.16f1`
- Git
- Git LFS
- Windows PowerShell 5.1+ (optional)

### Open in Unity

1. Open Unity Hub.
2. Select `Add project from disk`.
3. Choose the repository folder.
4. Open with Unity `2022.3.16f1`.

## Current Technical Status

- Unity version: `2022.3.16f1`
- Android recording has stability improvements for Android 14+ consent token behavior
- Recording service supports start/stop actions in a reusable session flow
- Replay lookup includes fallback folders for reliability

## Secondary Topic: Arabic Localization

Arabic localization is currently a secondary workstream.

Current files and components:

- `Assets/Resources/Languages/AR_Sheet1.bytes`
- `Assets/ArabicWriter/`
- `Assets/Scripts/Assembly-CSharp/ArabicLanguageRuntimeChecker.cs`

## Troubleshooting

### Missing textures after clone

```bash
git lfs pull
```

### Push rejected

Common fixes:

```bash
git pull --rebase origin main
git add .
git commit -m "fix: update after rebase"
git push origin Lu2312
```

If you are on `Sreagle`:

```bash
git add .
git commit -m "fix: update after rebase"
git push origin Sreagle
```

### Accidentally staged too many files

```bash
git reset
git add path/to/needed/files
```

## Contributing

1. Work on your assigned branch (`Lu2312` or `Sreagle`).
2. Keep changes focused and test them.
3. Open a PR to `main`.
4. Add screenshots/logs when relevant.

## License

This project modifies Card Wars Kingdom. Rights for the original game belong to Cartoon Network and the original developers. This repository is for fan-driven, non-commercial work.

Project status: Active development  
Last updated: March 2026  
Version: 5.0.3

## Screen Recording Status (March 2026)

The current Android PvP recording implementation is stable with recent fixes for Android 14+ behavior.

### Current Status

- Android 14+ `MediaProjection` consent tokens are treated as one-shot.
- After `ScreenRecorderNative.startRecording()`, cached projection state is cleared (`permissionGranted`, `resultCode`, `data`) to avoid second-recording failures.
- `RecordingService` supports session-style start/stop flow (`ACTION_START` / `ACTION_STOP`) without fully restarting the service every battle.
- Replay file lookup includes fallback folders:
	- `persistentDataPath/ExportedReplays`
	- `Movies/CardWarsKingdom`
	- `Movies/com.cwkrevived.cwkingdom`
- Runtime kill-switch is available through `PlayerPrefs` key: `cwk.screenRecordingEnabled`.

### How to Enable Screen Recording

Use the following values for `cwk.screenRecordingEnabled`:

- `0` (default): automatic PvP screen recording is disabled.
- `1`: automatic PvP screen recording is enabled.

Example:

```csharp
// Enable auto screen recording in PvP
PlayerPrefs.SetInt("cwk.screenRecordingEnabled", 1);
PlayerPrefs.Save();
```

Disable again:

```csharp
// Disable auto screen recording in PvP
PlayerPrefs.SetInt("cwk.screenRecordingEnabled", 0);
PlayerPrefs.Save();
```

### Recommended Usage

- Keep it disabled by default for normal players.
- Enable it for QA/testing sessions when validating replay capture behavior.
