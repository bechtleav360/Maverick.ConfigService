//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from ConfigReferenceLexer.g4 by ANTLR 4.7.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public partial class ConfigReferenceLexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		WHITESPACE=1, FLUFF=2, SINGLE_BRACES=3, REF_OPEN=4, REF_WHITESPACE=5, 
		REF_CMND_SEP=6, REF_CMND_NAME=7, REF_CMND_VAL=8, REF_CLOSE=9;
	public const int
		Reference=1;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE", "Reference"
	};

	public static readonly string[] ruleNames = {
		"WHITESPACE", "FLUFF", "SINGLE_BRACES", "REF_OPEN", "REF_WHITESPACE", 
		"REF_CMND_SEP", "REF_CMND_NAME", "REF_CMND_VAL", "REF_CLOSE", "VALUE", 
		"LOWERCASE", "UPPERCASE", "DIGIT", "QUOTES", "NEWLINE", "SEMICOLON", "COLON", 
		"REF_OPEN_BRACES", "REF_CLOSE_BRACES", "GER_UMLAUTS", "SP_CHARS"
	};


	public ConfigReferenceLexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public ConfigReferenceLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
	};
	private static readonly string[] _SymbolicNames = {
		null, "WHITESPACE", "FLUFF", "SINGLE_BRACES", "REF_OPEN", "REF_WHITESPACE", 
		"REF_CMND_SEP", "REF_CMND_NAME", "REF_CMND_VAL", "REF_CLOSE"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "ConfigReferenceLexer.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static ConfigReferenceLexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x2', '\v', '\x95', '\b', '\x1', '\b', '\x1', '\x4', '\x2', 
		'\t', '\x2', '\x4', '\x3', '\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', 
		'\x5', '\t', '\x5', '\x4', '\x6', '\t', '\x6', '\x4', '\a', '\t', '\a', 
		'\x4', '\b', '\t', '\b', '\x4', '\t', '\t', '\t', '\x4', '\n', '\t', '\n', 
		'\x4', '\v', '\t', '\v', '\x4', '\f', '\t', '\f', '\x4', '\r', '\t', '\r', 
		'\x4', '\xE', '\t', '\xE', '\x4', '\xF', '\t', '\xF', '\x4', '\x10', '\t', 
		'\x10', '\x4', '\x11', '\t', '\x11', '\x4', '\x12', '\t', '\x12', '\x4', 
		'\x13', '\t', '\x13', '\x4', '\x14', '\t', '\x14', '\x4', '\x15', '\t', 
		'\x15', '\x4', '\x16', '\t', '\x16', '\x3', '\x2', '\x3', '\x2', '\x3', 
		'\x2', '\x3', '\x2', '\x5', '\x2', '\x33', '\n', '\x2', '\x3', '\x2', 
		'\x3', '\x2', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x6', '\x3', '=', '\n', '\x3', '\r', '\x3', 
		'\xE', '\x3', '>', '\x3', '\x4', '\x3', '\x4', '\x3', '\x5', '\x3', '\x5', 
		'\x3', '\x5', '\x3', '\x5', '\x3', '\x6', '\x3', '\x6', '\x3', '\x6', 
		'\x3', '\x6', '\x3', '\a', '\x3', '\a', '\x3', '\b', '\x6', '\b', 'N', 
		'\n', '\b', '\r', '\b', '\xE', '\b', 'O', '\x3', '\b', '\x3', '\b', '\x3', 
		'\b', '\x6', '\b', 'U', '\n', '\b', '\r', '\b', '\xE', '\b', 'V', '\x3', 
		'\b', '\x6', '\b', 'Z', '\n', '\b', '\r', '\b', '\xE', '\b', '[', '\x3', 
		'\b', '\x3', '\b', '\x5', '\b', '`', '\n', '\b', '\x3', '\t', '\x3', '\t', 
		'\x6', '\t', '\x64', '\n', '\t', '\r', '\t', '\xE', '\t', '\x65', '\x3', 
		'\t', '\x3', '\t', '\x3', '\t', '\x6', '\t', 'k', '\n', '\t', '\r', '\t', 
		'\xE', '\t', 'l', '\x5', '\t', 'o', '\n', '\t', '\x3', '\n', '\x3', '\n', 
		'\x3', '\n', '\x3', '\n', '\x3', '\v', '\x3', '\v', '\x3', '\v', '\x3', 
		'\v', '\x3', '\v', '\x3', '\v', '\x5', '\v', '{', '\n', '\v', '\x3', '\f', 
		'\x3', '\f', '\x3', '\r', '\x3', '\r', '\x3', '\xE', '\x3', '\xE', '\x3', 
		'\xF', '\x3', '\xF', '\x3', '\x10', '\x3', '\x10', '\x3', '\x11', '\x3', 
		'\x11', '\x3', '\x12', '\x3', '\x12', '\x3', '\x13', '\x3', '\x13', '\x3', 
		'\x13', '\x3', '\x14', '\x3', '\x14', '\x3', '\x14', '\x3', '\x15', '\x3', 
		'\x15', '\x3', '\x16', '\x5', '\x16', '\x94', '\n', '\x16', '\x2', '\x2', 
		'\x17', '\x4', '\x3', '\x6', '\x4', '\b', '\x5', '\n', '\x6', '\f', '\a', 
		'\xE', '\b', '\x10', '\t', '\x12', '\n', '\x14', '\v', '\x16', '\x2', 
		'\x18', '\x2', '\x1A', '\x2', '\x1C', '\x2', '\x1E', '\x2', ' ', '\x2', 
		'\"', '\x2', '$', '\x2', '&', '\x2', '(', '\x2', '*', '\x2', ',', '\x2', 
		'\x4', '\x2', '\x3', '\r', '\x5', '\x2', '\v', '\v', '\xF', '\xF', '\"', 
		'\"', '\x4', '\x2', '\v', '\v', '\"', '\"', '\x4', '\x2', '}', '}', '\x7F', 
		'\x7F', '\x5', '\x2', '\v', '\f', '\xF', '\xF', '\"', '\"', '\x3', '\x2', 
		'\x63', '|', '\x3', '\x2', '\x43', '\\', '\x3', '\x2', '\x32', ';', '\x4', 
		'\x2', '$', '$', ')', ')', '\x4', '\x2', '\f', '\f', '\xF', '\xF', '\t', 
		'\x2', '\xC6', '\xC6', '\xD8', '\xD8', '\xDE', '\xDE', '\xE1', '\xE1', 
		'\xE6', '\xE6', '\xF8', '\xF8', '\xFE', '\xFE', '\t', '\x2', '#', '#', 
		'%', '\x31', '>', '\x42', ']', '\x62', '~', '~', '\xB2', '\xB2', '\xC4', 
		'\xC4', '\x2', '\x9A', '\x2', '\x4', '\x3', '\x2', '\x2', '\x2', '\x2', 
		'\x6', '\x3', '\x2', '\x2', '\x2', '\x2', '\b', '\x3', '\x2', '\x2', '\x2', 
		'\x2', '\n', '\x3', '\x2', '\x2', '\x2', '\x3', '\f', '\x3', '\x2', '\x2', 
		'\x2', '\x3', '\xE', '\x3', '\x2', '\x2', '\x2', '\x3', '\x10', '\x3', 
		'\x2', '\x2', '\x2', '\x3', '\x12', '\x3', '\x2', '\x2', '\x2', '\x3', 
		'\x14', '\x3', '\x2', '\x2', '\x2', '\x4', '\x32', '\x3', '\x2', '\x2', 
		'\x2', '\x6', '<', '\x3', '\x2', '\x2', '\x2', '\b', '@', '\x3', '\x2', 
		'\x2', '\x2', '\n', '\x42', '\x3', '\x2', '\x2', '\x2', '\f', '\x46', 
		'\x3', '\x2', '\x2', '\x2', '\xE', 'J', '\x3', '\x2', '\x2', '\x2', '\x10', 
		'_', '\x3', '\x2', '\x2', '\x2', '\x12', 'n', '\x3', '\x2', '\x2', '\x2', 
		'\x14', 'p', '\x3', '\x2', '\x2', '\x2', '\x16', 'z', '\x3', '\x2', '\x2', 
		'\x2', '\x18', '|', '\x3', '\x2', '\x2', '\x2', '\x1A', '~', '\x3', '\x2', 
		'\x2', '\x2', '\x1C', '\x80', '\x3', '\x2', '\x2', '\x2', '\x1E', '\x82', 
		'\x3', '\x2', '\x2', '\x2', ' ', '\x84', '\x3', '\x2', '\x2', '\x2', '\"', 
		'\x86', '\x3', '\x2', '\x2', '\x2', '$', '\x88', '\x3', '\x2', '\x2', 
		'\x2', '&', '\x8A', '\x3', '\x2', '\x2', '\x2', '(', '\x8D', '\x3', '\x2', 
		'\x2', '\x2', '*', '\x90', '\x3', '\x2', '\x2', '\x2', ',', '\x93', '\x3', 
		'\x2', '\x2', '\x2', '.', '\x33', '\t', '\x2', '\x2', '\x2', '/', '\x30', 
		'\a', '\xF', '\x2', '\x2', '\x30', '\x31', '\a', '$', '\x2', '\x2', '\x31', 
		'\x33', '\a', '+', '\x2', '\x2', '\x32', '.', '\x3', '\x2', '\x2', '\x2', 
		'\x32', '/', '\x3', '\x2', '\x2', '\x2', '\x33', '\x34', '\x3', '\x2', 
		'\x2', '\x2', '\x34', '\x35', '\b', '\x2', '\x2', '\x2', '\x35', '\x5', 
		'\x3', '\x2', '\x2', '\x2', '\x36', '=', '\x5', '\x16', '\v', '\x2', '\x37', 
		'=', '\x5', '$', '\x12', '\x2', '\x38', '=', '\x5', '\"', '\x11', '\x2', 
		'\x39', '=', '\x5', '\x1E', '\xF', '\x2', ':', '=', '\x5', ' ', '\x10', 
		'\x2', ';', '=', '\t', '\x3', '\x2', '\x2', '<', '\x36', '\x3', '\x2', 
		'\x2', '\x2', '<', '\x37', '\x3', '\x2', '\x2', '\x2', '<', '\x38', '\x3', 
		'\x2', '\x2', '\x2', '<', '\x39', '\x3', '\x2', '\x2', '\x2', '<', ':', 
		'\x3', '\x2', '\x2', '\x2', '<', ';', '\x3', '\x2', '\x2', '\x2', '=', 
		'>', '\x3', '\x2', '\x2', '\x2', '>', '<', '\x3', '\x2', '\x2', '\x2', 
		'>', '?', '\x3', '\x2', '\x2', '\x2', '?', '\a', '\x3', '\x2', '\x2', 
		'\x2', '@', '\x41', '\t', '\x4', '\x2', '\x2', '\x41', '\t', '\x3', '\x2', 
		'\x2', '\x2', '\x42', '\x43', '\x5', '&', '\x13', '\x2', '\x43', '\x44', 
		'\x3', '\x2', '\x2', '\x2', '\x44', '\x45', '\b', '\x5', '\x3', '\x2', 
		'\x45', '\v', '\x3', '\x2', '\x2', '\x2', '\x46', 'G', '\t', '\x5', '\x2', 
		'\x2', 'G', 'H', '\x3', '\x2', '\x2', '\x2', 'H', 'I', '\b', '\x6', '\x2', 
		'\x2', 'I', '\r', '\x3', '\x2', '\x2', '\x2', 'J', 'K', '\x5', '\"', '\x11', 
		'\x2', 'K', '\xF', '\x3', '\x2', '\x2', '\x2', 'L', 'N', '\x5', '\x16', 
		'\v', '\x2', 'M', 'L', '\x3', '\x2', '\x2', '\x2', 'N', 'O', '\x3', '\x2', 
		'\x2', '\x2', 'O', 'M', '\x3', '\x2', '\x2', '\x2', 'O', 'P', '\x3', '\x2', 
		'\x2', '\x2', 'P', 'Q', '\x3', '\x2', '\x2', '\x2', 'Q', 'R', '\x5', '$', 
		'\x12', '\x2', 'R', '`', '\x3', '\x2', '\x2', '\x2', 'S', 'U', '\x5', 
		'\x16', '\v', '\x2', 'T', 'S', '\x3', '\x2', '\x2', '\x2', 'U', 'V', '\x3', 
		'\x2', '\x2', '\x2', 'V', 'T', '\x3', '\x2', '\x2', '\x2', 'V', 'W', '\x3', 
		'\x2', '\x2', '\x2', 'W', 'Y', '\x3', '\x2', '\x2', '\x2', 'X', 'Z', '\a', 
		'\"', '\x2', '\x2', 'Y', 'X', '\x3', '\x2', '\x2', '\x2', 'Z', '[', '\x3', 
		'\x2', '\x2', '\x2', '[', 'Y', '\x3', '\x2', '\x2', '\x2', '[', '\\', 
		'\x3', '\x2', '\x2', '\x2', '\\', ']', '\x3', '\x2', '\x2', '\x2', ']', 
		'^', '\x5', '$', '\x12', '\x2', '^', '`', '\x3', '\x2', '\x2', '\x2', 
		'_', 'M', '\x3', '\x2', '\x2', '\x2', '_', 'T', '\x3', '\x2', '\x2', '\x2', 
		'`', '\x11', '\x3', '\x2', '\x2', '\x2', '\x61', '\x63', '\x5', '\x1E', 
		'\xF', '\x2', '\x62', '\x64', '\x5', '\x16', '\v', '\x2', '\x63', '\x62', 
		'\x3', '\x2', '\x2', '\x2', '\x64', '\x65', '\x3', '\x2', '\x2', '\x2', 
		'\x65', '\x63', '\x3', '\x2', '\x2', '\x2', '\x65', '\x66', '\x3', '\x2', 
		'\x2', '\x2', '\x66', 'g', '\x3', '\x2', '\x2', '\x2', 'g', 'h', '\x5', 
		'\x1E', '\xF', '\x2', 'h', 'o', '\x3', '\x2', '\x2', '\x2', 'i', 'k', 
		'\x5', '\x16', '\v', '\x2', 'j', 'i', '\x3', '\x2', '\x2', '\x2', 'k', 
		'l', '\x3', '\x2', '\x2', '\x2', 'l', 'j', '\x3', '\x2', '\x2', '\x2', 
		'l', 'm', '\x3', '\x2', '\x2', '\x2', 'm', 'o', '\x3', '\x2', '\x2', '\x2', 
		'n', '\x61', '\x3', '\x2', '\x2', '\x2', 'n', 'j', '\x3', '\x2', '\x2', 
		'\x2', 'o', '\x13', '\x3', '\x2', '\x2', '\x2', 'p', 'q', '\x5', '(', 
		'\x14', '\x2', 'q', 'r', '\x3', '\x2', '\x2', '\x2', 'r', 's', '\b', '\n', 
		'\x4', '\x2', 's', '\x15', '\x3', '\x2', '\x2', '\x2', 't', '{', '\x5', 
		'\x18', '\f', '\x2', 'u', '{', '\x5', '\x1A', '\r', '\x2', 'v', '{', '\x5', 
		'\x1C', '\xE', '\x2', 'w', '{', '\x5', '*', '\x15', '\x2', 'x', '{', '\x5', 
		',', '\x16', '\x2', 'y', '{', '\a', '\"', '\x2', '\x2', 'z', 't', '\x3', 
		'\x2', '\x2', '\x2', 'z', 'u', '\x3', '\x2', '\x2', '\x2', 'z', 'v', '\x3', 
		'\x2', '\x2', '\x2', 'z', 'w', '\x3', '\x2', '\x2', '\x2', 'z', 'x', '\x3', 
		'\x2', '\x2', '\x2', 'z', 'y', '\x3', '\x2', '\x2', '\x2', '{', '\x17', 
		'\x3', '\x2', '\x2', '\x2', '|', '}', '\t', '\x6', '\x2', '\x2', '}', 
		'\x19', '\x3', '\x2', '\x2', '\x2', '~', '\x7F', '\t', '\a', '\x2', '\x2', 
		'\x7F', '\x1B', '\x3', '\x2', '\x2', '\x2', '\x80', '\x81', '\t', '\b', 
		'\x2', '\x2', '\x81', '\x1D', '\x3', '\x2', '\x2', '\x2', '\x82', '\x83', 
		'\t', '\t', '\x2', '\x2', '\x83', '\x1F', '\x3', '\x2', '\x2', '\x2', 
		'\x84', '\x85', '\t', '\n', '\x2', '\x2', '\x85', '!', '\x3', '\x2', '\x2', 
		'\x2', '\x86', '\x87', '\a', '=', '\x2', '\x2', '\x87', '#', '\x3', '\x2', 
		'\x2', '\x2', '\x88', '\x89', '\a', '<', '\x2', '\x2', '\x89', '%', '\x3', 
		'\x2', '\x2', '\x2', '\x8A', '\x8B', '\a', '}', '\x2', '\x2', '\x8B', 
		'\x8C', '\a', '}', '\x2', '\x2', '\x8C', '\'', '\x3', '\x2', '\x2', '\x2', 
		'\x8D', '\x8E', '\a', '\x7F', '\x2', '\x2', '\x8E', '\x8F', '\a', '\x7F', 
		'\x2', '\x2', '\x8F', ')', '\x3', '\x2', '\x2', '\x2', '\x90', '\x91', 
		'\t', '\v', '\x2', '\x2', '\x91', '+', '\x3', '\x2', '\x2', '\x2', '\x92', 
		'\x94', '\t', '\f', '\x2', '\x2', '\x93', '\x92', '\x3', '\x2', '\x2', 
		'\x2', '\x94', '-', '\x3', '\x2', '\x2', '\x2', '\x10', '\x2', '\x3', 
		'\x32', '<', '>', 'O', 'V', '[', '_', '\x65', 'l', 'n', 'z', '\x93', '\x5', 
		'\b', '\x2', '\x2', '\a', '\x3', '\x2', '\x6', '\x2', '\x2',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}