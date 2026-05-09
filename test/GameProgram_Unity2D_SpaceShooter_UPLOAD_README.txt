GameProgram Unity 2D Space Shooter project archive

The complete Unity project archive is split into 7 parts because GitHub rejects large single-file uploads through the web/API path.

Files:
- GameProgram_Unity2D_SpaceShooter_test.zip.part01
- GameProgram_Unity2D_SpaceShooter_test.zip.part02
- GameProgram_Unity2D_SpaceShooter_test.zip.part03
- GameProgram_Unity2D_SpaceShooter_test.zip.part04
- GameProgram_Unity2D_SpaceShooter_test.zip.part05
- GameProgram_Unity2D_SpaceShooter_test.zip.part06
- GameProgram_Unity2D_SpaceShooter_test.zip.part07

To rebuild the zip on Windows PowerShell from this folder:

Get-Content .\GameProgram_Unity2D_SpaceShooter_test.zip.part* -Encoding Byte -ReadCount 0 | Set-Content .\GameProgram_Unity2D_SpaceShooter_test.zip -Encoding Byte

Expected SHA256 for the rebuilt zip:
EA42E47620D500F5F3906E48075262122556E5DFE270ED68094D8881BFCC4D46

The archive contains Assets, Packages, ProjectSettings, and the solution/csproj files. Unity Library, Temp, Logs, and UserSettings are intentionally excluded because Unity regenerates them.