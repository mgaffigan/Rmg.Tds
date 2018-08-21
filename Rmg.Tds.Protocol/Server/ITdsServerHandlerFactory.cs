namespace Rmg.Tds.Protocol.Server
{
    public interface ITdsServerHandlerFactory
    {
        ITdsServerHandler CreateHandler();
    }
}