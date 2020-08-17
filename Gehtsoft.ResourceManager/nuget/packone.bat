@echo off
set nugetname=%1
set /p "nugetversion=" < _version.txt
echo Packing %1 %nugetversion%%2
nuget pack "%nugetname%.nuspec" -Version %nugetversion%%2