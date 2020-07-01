using Rmg.Tds.Protocol.Server;

namespace Rmg.Tds.TestProxy
{
    internal class ProxyHandlerFactory : ITdsServerHandlerFactory
    {
        private readonly SqlTranslationTable TranslationTable;

        public ProxyHandlerFactory()
        {
            this.TranslationTable = new SqlTranslationTable(@"C:\Users\mgaffigan\source\repos\Rmg.Tds\Rmg.Tds.TestProxy\Translations");
        }

        public ITdsServerHandler CreateHandler()
        {
            return new ProxyHandler(TranslationTable);
        }
    }
}
