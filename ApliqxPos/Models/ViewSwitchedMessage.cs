using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ApliqxPos.Models;

/// <summary>
/// Message broadcasted globally when the user switches to a different view from the side menu.
/// ViewModels can register to this to automatically refresh their data.
/// </summary>
public class ViewSwitchedMessage : ValueChangedMessage<string>
{
    public ViewSwitchedMessage(string viewName) : base(viewName)
    {
    }
}
