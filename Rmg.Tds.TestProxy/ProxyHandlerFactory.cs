using Rmg.Tds.Protocol.Server;

namespace Rmg.Tds.TestProxy
{
    internal class ProxyHandlerFactory : ITdsServerHandlerFactory
    {
        private SqlTranslationTable TranslationTable;

        public ProxyHandlerFactory()
        {
            ReloadTranslationTable();
        }

        public SqlTranslationTable ReloadTranslationTable()
        {
            return this.TranslationTable = new SqlTranslationTable(@"C:\Users\mgaffigan\source\repos\Rmg.Tds\Rmg.Tds.TestProxy\Translations");
        }

        public ITdsServerHandler CreateHandler()
        {
            return new ProxyHandler(() => TranslationTable);
        }
    }
}
