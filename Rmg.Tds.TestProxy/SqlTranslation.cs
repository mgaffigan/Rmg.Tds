using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;
using System.Collections.ObjectModel;

namespace Rmg.Tds.TestProxy
{
    internal class SqlTranslation
    {

        public SqlTranslation(string file)
        {
            this.Name = Path.GetFileNameWithoutExtension(file);

            // TODO: Replace with Microsoft.SqlServer.Management.SqlParser and a proper parser
            // Split and extract SQLCMD variables
            var split = SplitTranslation.SplitFileSections(file);

            // extract declarations
            var declarations = TSqlMatchParser.GetDeclarations(split.Prelude.ToString());
            var variableCaptures = new List<ParameterCapture>();
            variableCaptures.AddRange(declarations.Select(d => new ParameterCapture(d.ParamName)));
            this.ParameterCaptures = variableCaptures.AsReadOnly();
            this.ParameterCaptureMap = variableCaptures.ToDictionary(d => d.ParameterName, d => d.Sentinel, StringComparer.OrdinalIgnoreCase);

            // erase SQLCMD with sentinels
            var sqlCmdCaptures = new List<SqlCmdVariableCapture>();
            foreach (var sqlcmdvar in split.SqlCmdVariableNames)
            {
                var cap = new SqlCmdVariableCapture(sqlcmdvar);
                split.Match.Replace($"$({sqlcmdvar})", cap.Sentinel);
                split.Replace.Replace($"$({sqlcmdvar})", cap.Sentinel);
                sqlCmdCaptures.Add(cap);
            }
            this.SqlCmdVariableCaptures = sqlCmdCaptures.AsReadOnly();

            // generate match regex
            var regex = TSqlMatchParser.GetMatchRegex(split.Match.ToString(), ParameterCaptureMap);

            // generate replace regex
            var replace = TSqlMatchParser.GetMatchReplace(split.Replace.ToString(), ParameterCaptureMap);

            // apply SQLCMD expressions
            foreach (var sqlcmdvar in SqlCmdVariableCaptures)
            {
                regex.Replace(sqlcmdvar.Sentinel, $"(?<{sqlcmdvar.Sentinel}>.*)");
                replace.Replace(sqlcmdvar.Sentinel, "${" + sqlcmdvar.Sentinel + "}");
            }

            // return
            this.MatchExpression = new Regex(regex.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.ReplaceExpression = replace.ToString();
        }

        public bool TryApply(string sql, out string result)
        {
            if (!MatchExpression.IsMatch(sql))
            {
                result = sql;
                return false;
            }

            result = MatchExpression.Replace(sql, ReplaceExpression);
            return true;
        }

        public string Name { get; }
        private readonly IReadOnlyList<ParameterCapture> ParameterCaptures;
        private readonly IReadOnlyDictionary<string, string> ParameterCaptureMap;
        private readonly IReadOnlyList<SqlCmdVariableCapture> SqlCmdVariableCaptures;
        private readonly Regex MatchExpression;
        private readonly string ReplaceExpression;

        private class SqlCmdVariableCapture
        {
            public string VariableName { get; }
            public string Sentinel { get; }

            public SqlCmdVariableCapture(string variableName)
            {
                this.VariableName = variableName ?? throw new ArgumentNullException(nameof(variableName));
                this.Sentinel = $"g{Guid.NewGuid():n}";
            }
        }

        private class ParameterCapture
        {
            public string ParameterName { get; }
            public string Sentinel { get; }

            public ParameterCapture(string paramName)
            {
                this.ParameterName = paramName ?? throw new ArgumentNullException(nameof(paramName));
                this.Sentinel = $"g{Guid.NewGuid():n}";
            }
        }
    }

    class TSqlMatchParser
    {
        public static StringBuilder GetMatchRegex(string sql, IReadOnlyDictionary<string, string> replaceVars)
        {
            var tokens = QueryTokenizer.MatchBatch.TryTokenize(sql);
            if (!tokens.HasValue)
            {
                throw new SqlTranslationParseException($"Failed to parse at {tokens.ErrorPosition}: {tokens.ToString()}");
            }

            const string
                WhitespaceMatch = @"\s*",
                Parameter = @"(?:@[a-zA-Z0-9_$@]+)",
                String = @"(?:N?'(?:''|[^'])*')",
                Binary = @"(?:0x[0-9a-f]*)",
                Number = @"(?:(?:[0-9]+(?:\.[0-9]*)?)|(?:\.[0-9]*))",
                ParameterOrLiteralMatch = Parameter + "|" + String + "|" + Binary + "|" + Number;
            var regex = new StringBuilder(sql.Length + 4096);
            regex.Append("^" + WhitespaceMatch);
            var lastIsWhitespace = true;
            foreach (var token in tokens.Value)
            {
                var stringValue = token.ToStringValue();
                switch (token.Kind)
                {
                    case QueryToken.Whitespace:
                        if (lastIsWhitespace)
                        {
                            break;
                        }
                        regex.Append(WhitespaceMatch);
                        lastIsWhitespace = true;
                        break;

                    case QueryToken.Body:
                    case QueryToken.BracketedIdentifier:
                    case QueryToken.QuotedIdentifier:
                    case QueryToken.StringLiteral:
                        regex.Append(Regex.Escape(stringValue));
                        lastIsWhitespace = false;
                        break;

                    case QueryToken.Parameter:
                        if (replaceVars.TryGetValue(stringValue, out var sentinel))
                        {
                            regex.Append($"(?<{sentinel}>{ParameterOrLiteralMatch})");
                        }
                        else
                        {
                            regex.Append(Regex.Escape(stringValue));
                        }
                        lastIsWhitespace = false;
                        break;

                    default: throw new NotImplementedException();
                }
            }
            regex.Append(WhitespaceMatch + "$");

            return regex;
        }

        public static StringBuilder GetMatchReplace(string sql, IReadOnlyDictionary<string, string> replaceVars)
        {
            var tokens = QueryTokenizer.MatchBatch.TryTokenize(sql);
            if (!tokens.HasValue)
            {
                throw new SqlTranslationParseException($"Failed to parse at {tokens.ErrorPosition}: {tokens.ToString()}");
            }

            var replace = new StringBuilder();
            void AppendEscapedReplacement(string s)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (c == '$')
                    {
                        replace.Append("$$");
                    }
                    else
                    {
                        replace.Append(c);
                    }
                }
            }
            foreach (var token in tokens.Value)
            {
                var stringValue = token.ToStringValue();
                switch (token.Kind)
                {
                    case QueryToken.Whitespace:
                    case QueryToken.Body:
                    case QueryToken.BracketedIdentifier:
                    case QueryToken.QuotedIdentifier:
                    case QueryToken.StringLiteral:
                        AppendEscapedReplacement(stringValue);
                        break;

                    case QueryToken.Parameter:
                        if (replaceVars.TryGetValue(stringValue, out var sentinel))
                        {
                            replace.Append("${" + sentinel + "}");
                        }
                        else
                        {
                            AppendEscapedReplacement(stringValue);
                        }
                        break;

                    default: throw new NotImplementedException();
                }
            }
            return replace;
        }

        public static ParsedDeclaration[] GetDeclarations(string sql)
        {
            var tokens = QueryTokenizer.PreludeTokenizer.TryTokenize(sql);
            if (!tokens.HasValue)
            {
                throw new SqlTranslationParseException($"Failed to tokenize at {tokens.ErrorPosition}: {tokens.ToString()}");
            }

            var parse = QueryTokenizer.PreludeParser(tokens.Value);
            if (!parse.HasValue)
            {
                throw new SqlTranslationParseException($"Failed to parse at {parse.ErrorPosition}: {parse.ToString()}");
            }

            return parse.Value;
        }

        class QueryTokenizer
        {
            static TextParser<Unit> DoubleEscapedLiteral(char start, char end) =>
                from open in Character.EqualTo(start)
                from content in Span.EqualTo("" + end + end).Value(Unit.Value).Try()
                    .Or(Character.Except(end).Value(Unit.Value))
                    .IgnoreMany()
                from close in Character.EqualTo(end)
                select Unit.Value;

            static TextParser<Unit> TSqlBracketedName { get; } = DoubleEscapedLiteral('[', ']');
            static TextParser<Unit> TSqlQuotedName { get; } = DoubleEscapedLiteral('"', '"');
            static TextParser<Unit> TSqlStringLiteral { get; } = DoubleEscapedLiteral('\'', '\'');

            static TextParser<Unit> Parameter { get; } =
                from open in Character.EqualTo('@')
                from content in Character.LetterOrDigit.Value(Unit.Value)
                    .Or(Character.In('_', '$', '@', '#').Value(Unit.Value))
                    .IgnoreMany()
                select Unit.Value;

            static TextParser<Unit> Literal { get; } =
                TSqlBracketedName
                .Or(TSqlQuotedName)
                .Or(TSqlStringLiteral)
                .Or(Parameter);

            static TextParser<Unit> Word { get; } = Character.Matching(c => !"['\"@".Contains(c) && char.IsLetterOrDigit(c), "letter or digit").AtLeastOnce().Value(Unit.Value);
            static TextParser<Unit> Body { get; } = Character.Matching(c => !"['\"@".Contains(c), "any character").Value(Unit.Value);
            static TextParser<Unit> PreludeBody { get; } = Character.Matching(c => !char.IsWhiteSpace(c) && !",=['\"@".Contains(c), "any character").Value(Unit.Value);

            public static Tokenizer<QueryToken> MatchBatch { get; } =
                new TokenizerBuilder<QueryToken>()
                    .Match(Span.WhiteSpace, QueryToken.Whitespace)
                    .Match(TSqlBracketedName, QueryToken.BracketedIdentifier)
                    .Match(TSqlQuotedName, QueryToken.QuotedIdentifier)
                    .Match(TSqlStringLiteral, QueryToken.StringLiteral)
                    .Match(Parameter, QueryToken.Parameter)
                    .Match(Word, QueryToken.Body)
                    .Match(Body, QueryToken.Body)
                    .Build();

            public static Tokenizer<QueryToken> PreludeTokenizer { get; } =
                new TokenizerBuilder<QueryToken>()
                    .Ignore(Span.WhiteSpace)
                    .Match(TSqlBracketedName, QueryToken.BracketedIdentifier)
                    .Match(TSqlQuotedName, QueryToken.QuotedIdentifier)
                    .Match(TSqlStringLiteral, QueryToken.StringLiteral)
                    .Match(Parameter, QueryToken.Parameter)
                    .Match(Span.EqualToIgnoreCase("DECLARE"), QueryToken.Declaration)
                    .Match(Character.EqualTo('='), QueryToken.Equals)
                    .Match(Character.EqualTo(','), QueryToken.Comma)
                    .Match(PreludeBody.Many(), QueryToken.Body)
                    .Build();

            public static TokenListParser<QueryToken, Token<QueryToken>> TypeParser { get; } =
                Token.EqualTo(QueryToken.Body)
                    .Or(Token.EqualTo(QueryToken.BracketedIdentifier))
                    .Or(Token.EqualTo(QueryToken.QuotedIdentifier));

            public static TokenListParser<QueryToken, Token<QueryToken>> LiteralParser { get; } =
                Token.EqualTo(QueryToken.Body)
                    .Or(Token.EqualTo(QueryToken.StringLiteral));

            public static TokenListParser<QueryToken, ParsedDeclaration> SingleDeclarationParser { get; } =
                from paramName in Token.EqualTo(QueryToken.Parameter)
                from typeName in TypeParser
                from defaultValue in Token.EqualTo(QueryToken.Equals).IgnoreThen(LiteralParser).OptionalOrDefault()
                select new ParsedDeclaration(paramName.ToStringValue(), typeName.ToStringValue());

            public static TokenListParser<QueryToken, ParsedDeclaration[]> DeclarationParser { get; } =
                from begin in Token.EqualTo(QueryToken.Declaration)
                from variables in SingleDeclarationParser
                    .ManyDelimitedBy(Token.EqualTo(QueryToken.Comma))
                select variables.ToArray();

            public static TokenListParser<QueryToken, ParsedDeclaration[]> UninterestingParser { get; } =
                Token.EqualTo(QueryToken.BracketedIdentifier)
                .Or(Token.EqualTo(QueryToken.QuotedIdentifier))
                .Or(Token.EqualTo(QueryToken.StringLiteral))
                .Or(Token.EqualTo(QueryToken.Parameter))
                .Or(Token.EqualTo(QueryToken.Equals))
                .Or(Token.EqualTo(QueryToken.Comma))
                .Or(Token.EqualTo(QueryToken.Body))
                .Select(n => new ParsedDeclaration[0]);

            public static TokenListParser<QueryToken, ParsedDeclaration[]> DeclarationOrUninteresting { get; } =
                DeclarationParser.Or(UninterestingParser);

            public static TokenListParser<QueryToken, ParsedDeclaration[]> PreludeParser { get; } =
                DeclarationOrUninteresting.Many().Select(d => d.SelectMany(a => a).ToArray());
        }

        public class ParsedDeclaration
        {
            public string ParamName { get; }
            public string TypeName { get; }

            public ParsedDeclaration(string paramName, string typeName)
            {
                this.ParamName = paramName ?? throw new ArgumentNullException(nameof(paramName));
                this.TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            }

            public override string ToString() => $"{ParamName} {TypeName}";
        }

        enum QueryToken
        {
            Body,
            BracketedIdentifier,
            QuotedIdentifier,
            StringLiteral,
            Parameter,
            Whitespace,

            // Only in declaration
            Declaration,
            Equals,
            Comma
        }
    }

    class SplitTranslation
    {
        enum ParseToken { Unknown = 0, SectionMatch, SectionReplace, SectionEnd, SqlCmdSetVar };
        enum ParseSection { Prelude, Match, Interlude, Replace, Epilog };
        enum TSqlParseState { Body, SqlCmdVariable, BracketedIdentifier, QuotedIdentifier, StringLiteral, Parameter }

        private SplitTranslation(int fileSize)
        {
            this.Prelude = new StringBuilder(4096);
            this.Match = new StringBuilder(fileSize);
            this.Replace = new StringBuilder(fileSize);
            this.SqlCmdVariableNames = new List<string>();
        }

        public StringBuilder Prelude { get; }
        public StringBuilder Match { get; }
        public StringBuilder Replace { get; }
        public List<string> SqlCmdVariableNames { get; }

        public static SplitTranslation SplitFileSections(string file)
        {
            var fileSize = checked((int)new FileInfo(file).Length);
            var result = new SplitTranslation(fileSize);

            var section = ParseSection.Prelude;
            int lineNo = 0;
            ParseToken token;
            foreach (var line in File.ReadAllLines(file))
            {
                lineNo += 1;

                switch (section)
                {
                    case ParseSection.Prelude:
                        // setvar must be at first line
                        if (line.StartsWith(":setvar") && result.Prelude.Length == 0)
                        {
                            var setvarParts = line.Split(new[] { ' ', '\t' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (setvarParts.Length != 3 || setvarParts[0] != ":setvar")
                            {
                                throw new SqlTranslationParseException(file, lineNo, $"Invalid format for :setvar '{line}'");
                            }
                            result.SqlCmdVariableNames.Add(setvarParts[1]);
                        }
                        else if (IsToken(line, out token))
                        {
                            if (token == ParseToken.SectionMatch)
                            {
                                section = ParseSection.Match;
                            }
                            else
                            {
                                throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected \"-- Match\"");
                            }
                        }
                        else
                        {
                            result.Prelude.AppendLine(line);
                        }
                        break;

                    case ParseSection.Match:
                        if (IsToken(line, out token))
                        {
                            if (token == ParseToken.SectionEnd)
                            {
                                section = ParseSection.Interlude;
                            }
                            else
                            {
                                throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected \"-- End\"");
                            }
                        }
                        else
                        {
                            result.Match.AppendLine(line);
                        }
                        break;

                    case ParseSection.Interlude:
                        if (IsToken(line, out token))
                        {
                            if (token == ParseToken.SectionReplace)
                            {
                                section = ParseSection.Replace;
                            }
                            else
                            {
                                throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected \"-- Replace\"");
                            }
                        }
                        else
                        {
                            //result.Interlude.AppendLine(line);
                        }
                        break;

                    case ParseSection.Replace:
                        if (IsToken(line, out token))
                        {
                            if (token == ParseToken.SectionEnd)
                            {
                                section = ParseSection.Epilog;
                            }
                            else
                            {
                                throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected \"-- End\"");
                            }
                        }
                        else
                        {
                            result.Replace.AppendLine(line);
                        }
                        break;

                    case ParseSection.Epilog:
                        if (IsToken(line, out token))
                        {
                            throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected EOF");
                        }
                        else
                        {
                            //result.Epilog.AppendLine(line);
                        }
                        break;
                }
            }

            switch (section)
            {
                case ParseSection.Prelude: throw new SqlTranslationParseException(file, "No \"-- Match\" section found");
                case ParseSection.Match: throw new SqlTranslationParseException(file, "No \"-- End\" section found in Match section");
                case ParseSection.Interlude: throw new SqlTranslationParseException(file, "No \"-- Replace\" section found");
                case ParseSection.Replace: throw new SqlTranslationParseException(file, "No \"-- End\" section found in Match section");
                case ParseSection.Epilog: /* Successful parse */ break;
                default: throw new NotImplementedException();
            }

            return result;
        }

        private static bool IsToken(string trimmedLine, out ParseToken token)
        {
            trimmedLine = trimmedLine.Trim().Replace(" ", "").Replace("\t", "");
            if (trimmedLine.Equals("--Match", System.StringComparison.CurrentCultureIgnoreCase))
            {
                token = ParseToken.SectionMatch;
                return true;
            }
            else if (trimmedLine.Equals("--Replace", System.StringComparison.CurrentCultureIgnoreCase))
            {
                token = ParseToken.SectionReplace;
                return true;
            }
            else if (trimmedLine.Equals("--End", System.StringComparison.CurrentCultureIgnoreCase))
            {
                token = ParseToken.SectionEnd;
                return true;
            }
            else
            {
                token = ParseToken.Unknown;
                return false;
            }
        }
    }
}