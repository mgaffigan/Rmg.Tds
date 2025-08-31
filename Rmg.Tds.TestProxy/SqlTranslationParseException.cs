using System;

namespace Rmg.Tds.TestProxy
{
    internal class SqlTranslationParseException : Exception
    {
        public int Line { get; }
        public string File { get; }

        public SqlTranslationParseException()
        {
        }

        public SqlTranslationParseException(string message) : base(message)
        {
        }

        public SqlTranslationParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SqlTranslationParseException(string file, string message) : this(file, -1, message)
        {
        }

        public SqlTranslationParseException(string file, int line, string message)
            : this($"Parse failure of {file} on line {line}: {message}")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            this.File = file ?? throw new ArgumentNullException(nameof(file));
            this.Line = line;
        }
    }
}