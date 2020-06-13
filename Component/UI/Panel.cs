using System;

namespace DotNetConsoleAppToolkit.Component.UI
{
    public abstract class Panel : UIElement
    {
        public Panel(
#pragma warning disable IDE0060 // Supprimer le paramètre inutilisé
            Func<Panel, string> content,
#pragma warning restore IDE0060 // Supprimer le paramètre inutilisé
            DrawStrategy drawStrategy
            ) : base(drawStrategy)
        {

        }
    }
}
