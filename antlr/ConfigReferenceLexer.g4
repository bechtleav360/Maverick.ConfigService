lexer grammar ConfigReferenceLexer;

WHITESPACE : ('\t' | ' ' | '\r' | '\r")') -> skip;

// curly-braces not included in FLUFF because otherwise it would also match references as FLUFF
FLUFF         : (VALUE | QUOTES | NEWLINE | ' ' | '\t')+;
SINGLE_BRACES : '{' | '}';

REF_OPEN : '{{' -> pushMode(Reference);

mode Reference;

REF_WHITESPACE : ('\t' | ' ' | '\r' | '\n') -> skip;
REF_CMND_SEP   : ';';
REF_CMND_NAME  : VALUE+ ':';
REF_CMND_VAL   : (QUOTES VALUE+ QUOTES) | (VALUE+);
REF_CLOSE      : '}}' -> popMode;

fragment VALUE: (
        LOWERCASE
        | UPPERCASE
        | DIGIT
        | GER_UMLAUTS
        | SP_CHARS
    );
fragment LOWERCASE : [a-z];
fragment UPPERCASE : [A-Z];
fragment DIGIT     : [0-9];
fragment QUOTES    : ['"];
fragment NEWLINE   : [\r\n];

// Ä, ä \u00c4, \u00e4 
// Ö, ö \u00d6, \u00f6 
// Ü, ü \u00dc, \u00fc 
// ß    \u00df
fragment GER_UMLAUTS:
    [\u00c4\u00e4\u00d6\u00f6\u00dc\u00fc\u00df];

fragment SP_CHARS:
    [!@#$%^&*=\\\-+|<>,./`°'_?:;]
    | '['
    | ']'
    | '('
    | ')';