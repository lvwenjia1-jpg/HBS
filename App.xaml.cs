using System.Windows;
using TestProject1.ViewModels.Factories;

namespace TestProject1;

public partial class App : Application
{
    public IWorkspaceViewModelFactory ViewModelFactory { get; } = new WorkspaceViewModelFactory();
}
