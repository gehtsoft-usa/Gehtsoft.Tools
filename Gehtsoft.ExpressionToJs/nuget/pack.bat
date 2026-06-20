@echo off
cd ..
dotnet build Gehtsoft.ExpressionToJs.sln -c release
cd nuget
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.ExpressionToJs.nuspec" -Version %nugetversion%
