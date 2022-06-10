@echo off
del *.nupkg
msbuild ../Gehtsoft.Tools2.sln /property:Configuration=Release
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.Tools2.nuspec" -Version %nugetversion%
