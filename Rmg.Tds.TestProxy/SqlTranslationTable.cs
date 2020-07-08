using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Rmg.Tds.TestProxy
{
    internal class SqlTranslationTable
    {
        public SqlTranslationTable(string configDir)
        {
            var translations = new List<SqlTranslation>();
            foreach (var file in Directory.GetFiles(configDir, "*.sql", SearchOption.AllDirectories))
            {
                translations.Add(new SqlTranslation(file));
            }
            this.Translations = translations.AsReadOnly();
        }

        public IReadOnlyList<SqlTranslation> Translations { get; }

        public bool TryApply(string sql, out string result, out string replacementName)
        {
            foreach (var txl in Translations)
            {
                if (txl.TryApply(sql, out result))
                {
                    replacementName = txl.Name;
                    return true;
                }
            }

            result = sql;
            replacementName = null;
            return false;
        }
    }
}