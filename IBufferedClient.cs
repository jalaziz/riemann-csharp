namespace Riemann
{
    public interface IBufferedClient : IClient
    {
        int BufferSize { get; set; }

        void Flush();
    }
}
