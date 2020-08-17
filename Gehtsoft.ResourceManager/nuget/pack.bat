rem @echo off
del *.nupkg > nul
call packone.bat Gehtsoft.ResourceManager
call packone.bat Gehtsoft.ResourceManager.Db
