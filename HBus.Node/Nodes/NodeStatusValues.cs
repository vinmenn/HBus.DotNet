namespace HBus.Nodes
{
    public enum NodeStatusValues
    {
        Unknown = 0, //Node at startup
        Reset,       //Node after configuration
        Ready,       //After Start()
        Active,      //While doing something
        Error        //Error happened in last command
    }
}