namespace HBus.Ports
{
  /// <summary>
  ///   HBus route information
  ///   Store hardware address and HBus addres binding
  /// </summary>
  public class RouteInfo
  {
    public RouteInfo()
    {
      Address = Address.Empty;
      HwAddress = string.Empty;
    }

    public RouteInfo(Address address, string hwAddress)
    {
      Address = address;
      HwAddress = hwAddress;
    }

    public Address Address { get; }
    public string HwAddress { get; }

    public override string ToString()
    {
      return string.Format("{0} : {1}", Address, HwAddress);
    }
  }
}