@echo off
PUSHD generated\Java
cmd.exe /c ..\..\grun.bat ConfigReference tokens ..\..\tests\complex.txt -encoding UTF8 -tokens
POPD