# generating lexer / parser for ConfigService

this folder contains the Lexer & Parser grammer used by Antlr to generate a parser in a target-language.

## content

- generated
  - target-output
- tests
  - hand-written tests against which a grammar should be tested
- antlr4.bat
  - start antlr with the given args
- ConfigReferenceLexer.g4
  - the Lexer-grammar for ConfigService-References
- ConfigReferenceParser.g4
  - the Parser-grammar for ConfigService-References
- grun.bat
  - start antlr-gui with the given args
- make.bat
  - rebuild the current grammar and run all tests against it
- rebuild.bat
  - rebuild the current grammar for Java / C#
- test-*.bat
  - test the generated grammar against test-file "*" at ./tests

## how to use

to use these files to either generate new grammar (updated grammar, new antlr-version)
you need to follow these steps:
1. download antlr complete
2. put `antlr-{v.v}-complete.jar` at some location
3. add {some-location} to your classpath
   1. Permanently: Using System Properties dialog > Environment variables > Create or append to CLASSPATH variable
   2. Temporarily, at command line: `SET CLASSPATH=.;C:\Javalib\antlr-4.{v.v}-complete.jar;%CLASSPATH%`
4. go to ../ConfigService/antlr/
5. invoke make.bat or rebuild.bat

## misc

- to generate the C#-grammar antlr (apparently) needs the Java-grammar, so we create it before creating C#-Grammar
- make.bat will also automatically copy the generated .cs to ConfigService.Parsing\Antlr