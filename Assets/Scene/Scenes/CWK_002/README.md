# CWK 002 Scenes

Create independent copies of the existing Unity scenes in this folder using the Unity Editor, then assign these scene names in `Assets/StreamingAssets/server_settings.json`:

- `CWK_002_LoadingScreen.unity`
- `CWK_002_VersionCheckScene.unity`
- `CWK_002_ConnectScreen.unity`
- `CWK_002_Login.unity`
- `CWK_002_FrontEndScene.unity`
- `CWK_002_BattleScene.unity`

After creating the scenes, add them to `ProjectSettings/EditorBuildSettings.asset`. Do not copy scene text files manually: the existing BattleScene and FrontEndScene are very large, and Unity should regenerate/maintain references safely.
