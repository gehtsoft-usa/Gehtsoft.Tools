@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.Tools.Profile.nuspec" -Version %nugetversion% -Properties cryptoversion=%cryptoversion%
