@echo off
PUSHD generated\Java
cmd.exe /c ..\..\grun.bat ConfigReference fluff ..\..\tests\fluff.txt -encoding UTF8 -tokens
POPD