//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from ConfigReferenceParser.g4 by ANTLR 4.7.1

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
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public partial class ConfigReferenceParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		WHITESPACE=1, FLUFF=2, SINGLE_BRACES=3, REF_OPEN=4, REF_WHITESPACE=5, 
		REF_CMND_SEP=6, REF_CMND_NAME=7, REF_CMND_VAL=8, REF_CLOSE=9;
	public const int
		RULE_input = 0, RULE_fluff = 1, RULE_reference = 2, RULE_reference_internals = 3;
	public static readonly string[] ruleNames = {
		"input", "fluff", "reference", "reference_internals"
	};

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

	public override string GrammarFileName { get { return "ConfigReferenceParser.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static ConfigReferenceParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public ConfigReferenceParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public ConfigReferenceParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}
	public partial class InputContext : ParserRuleContext {
		public FluffContext[] fluff() {
			return GetRuleContexts<FluffContext>();
		}
		public FluffContext fluff(int i) {
			return GetRuleContext<FluffContext>(i);
		}
		public ReferenceContext[] reference() {
			return GetRuleContexts<ReferenceContext>();
		}
		public ReferenceContext reference(int i) {
			return GetRuleContext<ReferenceContext>(i);
		}
		public InputContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_input; } }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitInput(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public InputContext input() {
		InputContext _localctx = new InputContext(Context, State);
		EnterRule(_localctx, 0, RULE_input);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 10;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			do {
				{
				State = 10;
				ErrorHandler.Sync(this);
				switch (TokenStream.LA(1)) {
				case FLUFF:
				case SINGLE_BRACES:
					{
					State = 8; fluff();
					}
					break;
				case REF_OPEN:
					{
					State = 9; reference();
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				State = 12;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			} while ( (((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << FLUFF) | (1L << SINGLE_BRACES) | (1L << REF_OPEN))) != 0) );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class FluffContext : ParserRuleContext {
		public ITerminalNode[] FLUFF() { return GetTokens(ConfigReferenceParser.FLUFF); }
		public ITerminalNode FLUFF(int i) {
			return GetToken(ConfigReferenceParser.FLUFF, i);
		}
		public ITerminalNode[] SINGLE_BRACES() { return GetTokens(ConfigReferenceParser.SINGLE_BRACES); }
		public ITerminalNode SINGLE_BRACES(int i) {
			return GetToken(ConfigReferenceParser.SINGLE_BRACES, i);
		}
		public FluffContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_fluff; } }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFluff(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public FluffContext fluff() {
		FluffContext _localctx = new FluffContext(Context, State);
		EnterRule(_localctx, 2, RULE_fluff);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 15;
			ErrorHandler.Sync(this);
			_alt = 1;
			do {
				switch (_alt) {
				case 1:
					{
					{
					State = 14;
					_la = TokenStream.LA(1);
					if ( !(_la==FLUFF || _la==SINGLE_BRACES) ) {
					ErrorHandler.RecoverInline(this);
					}
					else {
						ErrorHandler.ReportMatch(this);
					    Consume();
					}
					}
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				State = 17;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,2,Context);
			} while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ReferenceContext : ParserRuleContext {
		public ITerminalNode REF_OPEN() { return GetToken(ConfigReferenceParser.REF_OPEN, 0); }
		public ITerminalNode REF_CLOSE() { return GetToken(ConfigReferenceParser.REF_CLOSE, 0); }
		public Reference_internalsContext[] reference_internals() {
			return GetRuleContexts<Reference_internalsContext>();
		}
		public Reference_internalsContext reference_internals(int i) {
			return GetRuleContext<Reference_internalsContext>(i);
		}
		public ReferenceContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_reference; } }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitReference(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ReferenceContext reference() {
		ReferenceContext _localctx = new ReferenceContext(Context, State);
		EnterRule(_localctx, 4, RULE_reference);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 19; Match(REF_OPEN);
			State = 21;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			do {
				{
				{
				State = 20; reference_internals();
				}
				}
				State = 23;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			} while ( _la==REF_CMND_NAME || _la==REF_CMND_VAL );
			State = 25; Match(REF_CLOSE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class Reference_internalsContext : ParserRuleContext {
		public Reference_internalsContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_reference_internals; } }
	 
		public Reference_internalsContext() { }
		public virtual void CopyFrom(Reference_internalsContext context) {
			base.CopyFrom(context);
		}
	}
	public partial class FullReferenceContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_NAME() { return GetToken(ConfigReferenceParser.REF_CMND_NAME, 0); }
		public ITerminalNode REF_CMND_VAL() { return GetToken(ConfigReferenceParser.REF_CMND_VAL, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public FullReferenceContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFullReference(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class ValueRecursiveContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_VAL() { return GetToken(ConfigReferenceParser.REF_CMND_VAL, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public ReferenceContext reference() {
			return GetRuleContext<ReferenceContext>(0);
		}
		public ValueRecursiveContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitValueRecursive(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class FullRecursiveContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_NAME() { return GetToken(ConfigReferenceParser.REF_CMND_NAME, 0); }
		public ITerminalNode REF_CMND_VAL() { return GetToken(ConfigReferenceParser.REF_CMND_VAL, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public ReferenceContext reference() {
			return GetRuleContext<ReferenceContext>(0);
		}
		public FullRecursiveContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitFullRecursive(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class CommandReferenceContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_NAME() { return GetToken(ConfigReferenceParser.REF_CMND_NAME, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public CommandReferenceContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCommandReference(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class CommandRecursiveContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_NAME() { return GetToken(ConfigReferenceParser.REF_CMND_NAME, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public ReferenceContext reference() {
			return GetRuleContext<ReferenceContext>(0);
		}
		public CommandRecursiveContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCommandRecursive(this);
			else return visitor.VisitChildren(this);
		}
	}
	public partial class ValueReferenceContext : Reference_internalsContext {
		public ITerminalNode REF_CMND_VAL() { return GetToken(ConfigReferenceParser.REF_CMND_VAL, 0); }
		public ITerminalNode REF_CMND_SEP() { return GetToken(ConfigReferenceParser.REF_CMND_SEP, 0); }
		public ValueReferenceContext(Reference_internalsContext context) { CopyFrom(context); }
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			IConfigReferenceParserVisitor<TResult> typedVisitor = visitor as IConfigReferenceParserVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitValueReference(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public Reference_internalsContext reference_internals() {
		Reference_internalsContext _localctx = new Reference_internalsContext(Context, State);
		EnterRule(_localctx, 6, RULE_reference_internals);
		int _la;
		try {
			State = 50;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,7,Context) ) {
			case 1:
				_localctx = new FullRecursiveContext(_localctx);
				EnterOuterAlt(_localctx, 1);
				{
				{
				State = 27; Match(REF_CMND_NAME);
				State = 28; Match(REF_CMND_VAL);
				State = 29; Match(REF_CMND_SEP);
				State = 30; reference();
				}
				}
				break;
			case 2:
				_localctx = new FullReferenceContext(_localctx);
				EnterOuterAlt(_localctx, 2);
				{
				{
				State = 31; Match(REF_CMND_NAME);
				State = 32; Match(REF_CMND_VAL);
				State = 34;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				if (_la==REF_CMND_SEP) {
					{
					State = 33; Match(REF_CMND_SEP);
					}
				}

				}
				}
				break;
			case 3:
				_localctx = new ValueRecursiveContext(_localctx);
				EnterOuterAlt(_localctx, 3);
				{
				{
				State = 36; Match(REF_CMND_VAL);
				State = 37; Match(REF_CMND_SEP);
				State = 38; reference();
				}
				}
				break;
			case 4:
				_localctx = new ValueReferenceContext(_localctx);
				EnterOuterAlt(_localctx, 4);
				{
				{
				State = 39; Match(REF_CMND_VAL);
				State = 41;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				if (_la==REF_CMND_SEP) {
					{
					State = 40; Match(REF_CMND_SEP);
					}
				}

				}
				}
				break;
			case 5:
				_localctx = new CommandRecursiveContext(_localctx);
				EnterOuterAlt(_localctx, 5);
				{
				{
				State = 43; Match(REF_CMND_NAME);
				State = 44; Match(REF_CMND_SEP);
				State = 45; reference();
				}
				}
				break;
			case 6:
				_localctx = new CommandReferenceContext(_localctx);
				EnterOuterAlt(_localctx, 6);
				{
				{
				State = 46; Match(REF_CMND_NAME);
				State = 48;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				if (_la==REF_CMND_SEP) {
					{
					State = 47; Match(REF_CMND_SEP);
					}
				}

				}
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x3', '\v', '\x37', '\x4', '\x2', '\t', '\x2', '\x4', '\x3', 
		'\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', '\x5', '\t', '\x5', '\x3', 
		'\x2', '\x3', '\x2', '\x6', '\x2', '\r', '\n', '\x2', '\r', '\x2', '\xE', 
		'\x2', '\xE', '\x3', '\x3', '\x6', '\x3', '\x12', '\n', '\x3', '\r', '\x3', 
		'\xE', '\x3', '\x13', '\x3', '\x4', '\x3', '\x4', '\x6', '\x4', '\x18', 
		'\n', '\x4', '\r', '\x4', '\xE', '\x4', '\x19', '\x3', '\x4', '\x3', '\x4', 
		'\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', 
		'\x3', '\x5', '\x3', '\x5', '\x5', '\x5', '%', '\n', '\x5', '\x3', '\x5', 
		'\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x5', '\x5', 
		',', '\n', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', '\x3', '\x5', 
		'\x3', '\x5', '\x5', '\x5', '\x33', '\n', '\x5', '\x5', '\x5', '\x35', 
		'\n', '\x5', '\x3', '\x5', '\x2', '\x2', '\x6', '\x2', '\x4', '\x6', '\b', 
		'\x2', '\x3', '\x3', '\x2', '\x4', '\x5', '\x2', '>', '\x2', '\f', '\x3', 
		'\x2', '\x2', '\x2', '\x4', '\x11', '\x3', '\x2', '\x2', '\x2', '\x6', 
		'\x15', '\x3', '\x2', '\x2', '\x2', '\b', '\x34', '\x3', '\x2', '\x2', 
		'\x2', '\n', '\r', '\x5', '\x4', '\x3', '\x2', '\v', '\r', '\x5', '\x6', 
		'\x4', '\x2', '\f', '\n', '\x3', '\x2', '\x2', '\x2', '\f', '\v', '\x3', 
		'\x2', '\x2', '\x2', '\r', '\xE', '\x3', '\x2', '\x2', '\x2', '\xE', '\f', 
		'\x3', '\x2', '\x2', '\x2', '\xE', '\xF', '\x3', '\x2', '\x2', '\x2', 
		'\xF', '\x3', '\x3', '\x2', '\x2', '\x2', '\x10', '\x12', '\t', '\x2', 
		'\x2', '\x2', '\x11', '\x10', '\x3', '\x2', '\x2', '\x2', '\x12', '\x13', 
		'\x3', '\x2', '\x2', '\x2', '\x13', '\x11', '\x3', '\x2', '\x2', '\x2', 
		'\x13', '\x14', '\x3', '\x2', '\x2', '\x2', '\x14', '\x5', '\x3', '\x2', 
		'\x2', '\x2', '\x15', '\x17', '\a', '\x6', '\x2', '\x2', '\x16', '\x18', 
		'\x5', '\b', '\x5', '\x2', '\x17', '\x16', '\x3', '\x2', '\x2', '\x2', 
		'\x18', '\x19', '\x3', '\x2', '\x2', '\x2', '\x19', '\x17', '\x3', '\x2', 
		'\x2', '\x2', '\x19', '\x1A', '\x3', '\x2', '\x2', '\x2', '\x1A', '\x1B', 
		'\x3', '\x2', '\x2', '\x2', '\x1B', '\x1C', '\a', '\v', '\x2', '\x2', 
		'\x1C', '\a', '\x3', '\x2', '\x2', '\x2', '\x1D', '\x1E', '\a', '\t', 
		'\x2', '\x2', '\x1E', '\x1F', '\a', '\n', '\x2', '\x2', '\x1F', ' ', '\a', 
		'\b', '\x2', '\x2', ' ', '\x35', '\x5', '\x6', '\x4', '\x2', '!', '\"', 
		'\a', '\t', '\x2', '\x2', '\"', '$', '\a', '\n', '\x2', '\x2', '#', '%', 
		'\a', '\b', '\x2', '\x2', '$', '#', '\x3', '\x2', '\x2', '\x2', '$', '%', 
		'\x3', '\x2', '\x2', '\x2', '%', '\x35', '\x3', '\x2', '\x2', '\x2', '&', 
		'\'', '\a', '\n', '\x2', '\x2', '\'', '(', '\a', '\b', '\x2', '\x2', '(', 
		'\x35', '\x5', '\x6', '\x4', '\x2', ')', '+', '\a', '\n', '\x2', '\x2', 
		'*', ',', '\a', '\b', '\x2', '\x2', '+', '*', '\x3', '\x2', '\x2', '\x2', 
		'+', ',', '\x3', '\x2', '\x2', '\x2', ',', '\x35', '\x3', '\x2', '\x2', 
		'\x2', '-', '.', '\a', '\t', '\x2', '\x2', '.', '/', '\a', '\b', '\x2', 
		'\x2', '/', '\x35', '\x5', '\x6', '\x4', '\x2', '\x30', '\x32', '\a', 
		'\t', '\x2', '\x2', '\x31', '\x33', '\a', '\b', '\x2', '\x2', '\x32', 
		'\x31', '\x3', '\x2', '\x2', '\x2', '\x32', '\x33', '\x3', '\x2', '\x2', 
		'\x2', '\x33', '\x35', '\x3', '\x2', '\x2', '\x2', '\x34', '\x1D', '\x3', 
		'\x2', '\x2', '\x2', '\x34', '!', '\x3', '\x2', '\x2', '\x2', '\x34', 
		'&', '\x3', '\x2', '\x2', '\x2', '\x34', ')', '\x3', '\x2', '\x2', '\x2', 
		'\x34', '-', '\x3', '\x2', '\x2', '\x2', '\x34', '\x30', '\x3', '\x2', 
		'\x2', '\x2', '\x35', '\t', '\x3', '\x2', '\x2', '\x2', '\n', '\f', '\xE', 
		'\x13', '\x19', '$', '+', '\x32', '\x34',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
