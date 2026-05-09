Plane Shooter Unity project archive

This folder contains the complete airplane shooter Unity project as split zip parts.

Files:
- GameProgram_Unity2D_SpaceShooter_test.zip.part01
- GameProgram_Unity2D_SpaceShooter_test.zip.part02
- GameProgram_Unity2D_SpaceShooter_test.zip.part03
- GameProgram_Unity2D_SpaceShooter_test.zip.part04
- GameProgram_Unity2D_SpaceShooter_test.zip.part05
- GameProgram_Unity2D_SpaceShooter_test.zip.part06
- GameProgram_Unity2D_SpaceShooter_test.zip.part07

To rebuild on Windows PowerShell from this folder:

Get-Content .\GameProgram_Unity2D_SpaceShooter_test.zip.part* -Encoding Byte -ReadCount 0 | Set-Content .\GameProgram_Unity2D_SpaceShooter_test.zip -Encoding Byte

Expected SHA256:
EA42E47620D500F5F3906E48075262122556E5DFE270ED68094D8881BFCC4D46

The rebuilt zip contains Assets, Packages, ProjectSettings, and solution/csproj files. Unity will regenerate Library, Temp, Logs, and UserSettings.