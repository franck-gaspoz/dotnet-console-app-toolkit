namespace DotNetConsoleSdk.Component.UI
{
    public enum ViewResizeStrategy
    {
        /// <summary>
        /// does nothing, let's host terminal does anything it wants
        /// </summary>
        HostTerminalDefault,

        /// <summary>
        /// UI is cleared and elements are adapted to fit view (host terminal window) size when it changes
        /// </summary>
        FitViewSize,

        /// <summary>
        /// UI view size is fixed on startup. any changes would let host terminal to add scrollbars or resize content (depending on its own strategy)
        /// </summary>
        FixedViewSize
    }
}
