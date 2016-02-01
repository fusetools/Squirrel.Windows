@echo off
pushd "%~dp0"

REM VS Environment setup
if not defined VSINSTALLDIR (
	if defined VS140COMNTOOLS (
		call "%VS140COMNTOOLS%\vsvars32.bat"
		goto :BUILD
	)
)

:BUILD
msbuild /m /p:Configuration=Release Squirrel.sln || goto ERROR

:SUCCESS
popd && exit /b 0

:ERROR
pause
popd && exit /b 1
