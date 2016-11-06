namespace FastNats.Client
{
    /// <summary>
    /// State of the connection.
    /// </summary>
    public enum ConnState
    {
        DISCONNECTED = 0,
        CONNECTED,
        CLOSED,
        RECONNECTING,
        CONNECTING
    }
}
