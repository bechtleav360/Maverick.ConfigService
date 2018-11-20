@echo off
PUSHD generated\Java
cmd.exe /c ..\..\grun.bat ConfigReference input ..\..\tests\references.txt -encoding UTF8 -tokens
cmd.exe /c ..\..\grun.bat ConfigReference input ..\..\tests\references.txt -encoding UTF8 -gui
POPD