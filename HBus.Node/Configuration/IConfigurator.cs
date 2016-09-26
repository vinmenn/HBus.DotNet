using HBus.Nodes.Hardware;

namespace HBus.Nodes.Configuration
{
    /// <summary>
    /// Node configurator interface
    /// This is the common interface
    /// for HBus node configurators.
    /// </summary>
    public interface INodeConfigurator
    {
        //Global objects
        /// <summary>
        /// Hardware Abstraction Layer
        /// </summary>
        IHardwareAbstractionLayer Hal { get; }
        /// <summary>
        /// HBus controller
        /// </summary>
        BusController Bus { get; }
        /// <summary>
        /// Node scheduler
        /// </summary>
        Scheduler Scheduler { get; }
        /// <summary>
        /// Configured node
        /// </summary>
        Node Node { get; }

        //Public methods
        /// <summary>
        /// Initial configuration of node with persistent data
        /// </summary>
        /// <param name="defaultConfig">Use default configuration</param>
        void Configure(bool defaultConfig);
        /// <summary>
        /// Load configuration after initial configuration
        /// </summary>
        /// <param name="defaultconfig"></param>
        /// <returns></returns>
        bool LoadConfiguration(bool defaultconfig, Node node);
        /// <summary>
        /// Save current configuration
        /// </summary>
        /// <param name="defaultconfig"></param>
        /// <returns></returns>
        bool SaveConfiguration(bool defaultconfig);
    }
}