@echo off
PUSHD generated\Java
cmd.exe /c ..\..\grun.bat ConfigReference reference ..\..\tests\reference.txt -encoding UTF8 -tokens
POPD