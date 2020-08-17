@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.ExpressionToJs.nuspec" -Version %nugetversion%
