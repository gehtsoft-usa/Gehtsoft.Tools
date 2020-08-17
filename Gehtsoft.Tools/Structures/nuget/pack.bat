@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
set /p "typeutilsversion=" < ..\..\TypeUtils\nuget\_version.txt
nuget pack "Gehtsoft.Tools.Structures.nuspec" -Version %nugetversion% -Properties typeutilsversion=%typeutilsversion%
