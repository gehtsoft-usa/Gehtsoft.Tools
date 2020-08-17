@echo off
if exist dst del dst\*.* /q /s > nul
if not exist dst md dst
if exist src del src\*.* /q /s > nul
if not exist src md src
"%sandcastle%\ProductionTools\MrefBuilder.exe" ..\..\Gehtsoft.ExpressionToJs\bin\Debug\net461\Gehtsoft.ExpressionToJs.dll  /out:doc-source.xml
if not exist src mkdir src
%docgen%\bin\docgen prepare.xml
