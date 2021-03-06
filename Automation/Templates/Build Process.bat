@echo off

rem Get our properties
set PROCESS_NAME=%1
set PROCESS_NAME=%PROCESS_NAME:"=%

set CONFIG_TYPE=%2
set CONFIG_TYPE=%CONFIG_TYPE:"=%

rem Update Nuget packages 
"%~dp0..\..\External\Nuget\nuget.exe" restore "%~dp0..\..\Processes\Shared"
"%~dp0..\..\External\Nuget\nuget.exe" restore "%~dp0..\..\Processes\%PROCESS_NAME%"

rem Set up
call "%~dp0..\Templates\Prepare Environment.bat"

rem Build the solution
msbuild /nologo "%~dp0..\..\Processes\%PROCESS_NAME%\%PROCESS_NAME%.sln" /t:Clean /p:Configuration=%CONFIG_TYPE%
msbuild /nologo "%~dp0..\..\Processes\%PROCESS_NAME%\%PROCESS_NAME%.sln" /t:Rebuild /p:Configuration=%CONFIG_TYPE%
