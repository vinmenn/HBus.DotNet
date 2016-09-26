namespace HBus.Nodes.Pins
{
    /// <summary>
    /// Pin sub types
    /// </summary>
    public enum PinSubTypes
    {
        None,
        InputLow,
        InputHigh,
        InputHighLow,
        InputLowHigh,
        //Numeric conditions
        InputBelow,
        InputBeyond,
        InputBetween,
        InputOutside,
        //General conditions
        InputChanged,
        InputEqualTo,
        OutputLow,
        OutputHigh,
        OutputToggle,
        OutputTimedHigh,
        OutputTimedLow,
        OutputDelayHigh,
        OutputDelayLow,
        OutputPulseHigh,
        OutputPulseLow,
        OutputDelayToggle,
        //Numeric types
        OutputSetValue,
        OutputAddValue
    }
}