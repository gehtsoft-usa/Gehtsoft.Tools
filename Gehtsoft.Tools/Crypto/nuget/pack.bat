@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.Tools.Crypto.nuspec" -Version %nugetversion%
