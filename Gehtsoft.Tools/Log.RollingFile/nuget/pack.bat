@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
set /p "logversion=" < ..\..\Log\nuget\_version.txt
nuget pack "Gehtsoft.Tools.Log.RollingFile.nuspec" -Version %nugetversion% -Properties logversion=%logversion%
