namespace FastNats.Client
{
    public interface IChannel<T>
    {
        T Get(int timeout);
        void Add(T item);

        void Close();

        int Count { get; }
    }
}
