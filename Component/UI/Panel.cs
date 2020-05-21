using System;

namespace DotNetConsoleSdk.Component.UI
{
    public abstract class Panel : UIElement
    {
        public Panel(
            Func<Panel, string> content,
            DrawStrategy drawStrategy
            ) : base(drawStrategy)
        {

        }
    }
}
