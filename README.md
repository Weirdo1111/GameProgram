# One Bullet

Unity vertical slice prototype based on the One Bullet design document.

## Play

Open `Assets/Scenes/SampleScene.unity` and press Play.

- Space / left click: launch or continue
- W / S: steer the bullet up and down
- A / D: steer the bullet left and right
- Shift: boost
- R: restart level

## Build

In Unity, use `Build > Build One Bullet Windows`.

Command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" -batchmode -quit -projectPath "c:\Users\ken\My project" -executeMethod BuildOneBullet.BuildWindowsCli -logFile "c:\Users\ken\My project\Logs\onebullet_build.log"
```

Output: `Builds/OneBullet-Windows/OneBullet.exe`
