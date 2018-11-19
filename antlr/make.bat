@echo off

REM rmdir generated /S /Q
call antlr4.bat -o generated/Java -lib generated/Java -Dlanguage=Java -visitor -no-listener ConfigReference*.g4
call antlr4.bat -o generated/CSharp -lib generated/Java -Dlanguage=CSharp -visitor -no-listener ConfigReference*.g4
javac ./generated/Java/*.java