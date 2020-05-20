using System;

namespace DotNetConsoleSdk.Component
{
    public abstract class UIPanel : UIElement
    {
        public UIPanel(
            Func<UIPanel, string> content,
            DrawStrategy drawStrategy
            ) : base(drawStrategy)
        {

        }
    }
}
