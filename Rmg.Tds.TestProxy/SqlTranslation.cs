using System;
using System.IO;
using System.Text;

namespace Rmg.Tds.TestProxy
{
    internal class SqlTranslation
    {
        enum ParseToken { Unknown = 0, SectionMatch, SectionReplace, SectionEnd };
        enum ParseSection { Prelude, Match, Interlude, Replace, Epilog };
        enum TSqlParseState { Body, SqlCmdVariable, BracketedIdentifier, QuotedIdentifier, StringLiteral, Parameter }

        public SqlTranslation(string file)
        {
            this.Name = Path.GetFileNameWithoutExtension(file);

            // TODO: Replace with Microsoft.SqlServer.Management.SqlParser and a proper parser
            var (other, match, replace) = SplitFileSections(file);

            // transform match parameters to sentinels, build name table
            var sbSentinelMatch = new StringBuilder(match.Length + 200);
            var sbName = new StringBuilder(200);
            var state = TSqlParseState.Body;
            for (int i = 0; i < match.Length; i++)
            {
                var c = match[i];
                var hasMore = i + 1 < match.Length;
                switch (state)
                {
                    case TSqlParseState.Body:
                        if (c == '$' && hasMore && )
                        if (c == '[')
                        {
                            state = TSqlParseState.BracketedIdentifier;
                        }
                        else if (c == '"')
                        {
                            state = TSqlParseState.QuotedIdentifier;
                        }
                        else if (c == '\'')
                        {
                            state = TSqlParseState.StringLiteral;
                        }
                }
            }
            // escape match regex
            // transform escaped match regex to actual regex
        }

        private static (StringBuilder, StringBuilder, StringBuilder) SplitFileSections(string file)
        {
            var fileSize = checked((int)new FileInfo(file).Length);
            var other = new StringBuilder(4096);
            var match = new StringBuilder(fileSize);
            var replace = new StringBuilder(fileSize);

            var section = ParseSection.Prelude;
            var currentAccumulator = other;
            int lineNo = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                lineNo += 1;

                switch (section)
                {
                    case ParseSection.Prelude:
                        if (IsToken(line, out var token))
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
                            other.AppendLine(line);
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
                            match.AppendLine(line);
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
                            other.AppendLine(line);
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
                            replace.AppendLine(line);
                        }
                        break;

                    case ParseSection.Epilog:
                        if (IsToken(line, out token))
                        {
                            throw new SqlTranslationParseException(file, lineNo, $"Unexpected token {line}, expected EOF");
                        }
                        else
                        {
                            other.AppendLine(line);
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

            return (other, match, replace);
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

        public string Name { get; }
    }
}