@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.Tools.Log.nuspec" -Version %nugetversion%
