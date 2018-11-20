parser grammar ConfigReferenceParser;

options {
    tokenVocab = ConfigReferenceLexer;
}

input : (fluff | reference)+;

fluff : (FLUFF | SINGLE_BRACES)+;

reference : REF_OPEN reference_internals+ REF_CLOSE;

reference_internals:
    (REF_CMND_NAME REF_CMND_VAL REF_CMND_SEP reference) # FullRecursive
    | (REF_CMND_NAME REF_CMND_VAL REF_CMND_SEP?)        # FullReference
    | (REF_CMND_VAL REF_CMND_SEP reference)             # ValueRecursive
    | (REF_CMND_VAL REF_CMND_SEP?)                      # ValueReference
    | (REF_CMND_NAME REF_CMND_SEP reference)            # CommandRecursive
    | (REF_CMND_NAME REF_CMND_SEP?)                     # CommandReference;
