using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public static class NumberedProcedures
    {
        private static readonly Dictionary<int, string> _IdsToName;
        private static readonly Dictionary<string, int> _NamesToId;

        static NumberedProcedures()
        {
            _IdsToName = new Dictionary<int, string>()
            {
                { 01, "sp_cursor" },
                { 02, "sp_cursoropen" },
                { 03, "sp_cursorprepare" },
                { 04, "sp_cursorexecute" },
                { 05, "sp_cursorprepexec" },
                { 06, "sp_cursorunprepare" },
                { 07, "sp_cursorfetch" },
                { 08, "sp_cursoroption" },
                { 09, "sp_cursorclose" },
                { 10, "sp_executesql" },
                { 11, "sp_prepare" },
                { 12, "sp_execute" },
                { 13, "sp_prepexec" },
                { 14, "sp_prepexecrpc" },
                { 15, "sp_unprepare" },
            };
            _NamesToId = _IdsToName.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.InvariantCultureIgnoreCase);
        }

        public static string TryGetNameForProcedureId(int id)
        {
            string result;
            _IdsToName.TryGetValue(id, out result);
            return result;
        }

        public static int? TryGetIdForProcedureName(string name)
        {
            if (_NamesToId.TryGetValue(name, out int result))
            {
                return result;
            }

            return null;
        }
    }
}
