using CommunityToolkit.Mvvm.ComponentModel;

namespace IOC.App.ViewModels.Base;

public delegate void ConfirmationRequester(
    string title,
    string message,
    string warning,
    string subsystem,
    Func<Task> action,
    string actionText = "Xác Nhận Thực Hiện");

public abstract class ViewModelBase : ObservableObject
{
}

