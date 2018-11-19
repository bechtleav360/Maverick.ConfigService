parser grammar ConfigReferenceParser;

options {
    tokenVocab = ConfigReferenceLexer;
}

fluff : (FLUFF | SINGLE_BRACES)+;

reference:
    REF_OPEN (
        (REF_CMND_VAL)
        | (REF_CMND_NAME REF_CMND_VAL)
        | (REF_CMND_NAME REF_CMND_VAL REF_CMND_SEP (REF_CMND_NAME REF_CMND_VAL REF_CMND_SEP?)+)
    ) REF_CLOSE;
