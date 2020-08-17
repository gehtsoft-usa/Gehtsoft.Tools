@echo off
if exist dst del dst\*.* /q /s > nul
if not exist dst md dst
if exist src del src\*.* /q /s > nul
if not exist src md src
"%sandcastle%\ProductionTools\MrefBuilder.exe" ..\..\Crypto\bin\Release\net45\Gehtsoft.Tools.Crypto.dll ..\..\FileUtils\bin\Release\net45\Gehtsoft.Tools.FileUtils.dll ..\..\Log\bin\Release\net45\Gehtsoft.Tools.Log.dll ..\..\ObjectPool\bin\Release\net45\Gehtsoft.Tools.ObjectPool.dll ..\..\Profile\bin\Release\net45\Gehtsoft.Tools.Profile.dll ..\..\Log.RollingFile\bin\Release\net45\Gehtsoft.Tools.Log.RollingFile.dll ..\..\Log.Serilog\bin\Release\net45\Gehtsoft.Tools.Log.Serilog.dll ..\..\Structures\bin\Release\net45\Gehtsoft.Tools.Structures.dll ..\..\TypeUtils\bin\Release\net45\Gehtsoft.Tools.TypeUtils.dll ..\..\IoC\bin\Release\net45\Gehtsoft.Tools.IoC.dll ..\..\CommandLine\bin\Release\net45\Gehtsoft.Tools.CommandLine.dll  /out:doc-source.xml
if not exist src mkdir src
%docgen%\bin\docgen prepare.xml
