namespace VinhKhanhTour;

public partial class App : Application
{
    public App() { InitializeComponent(); }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Navigation để chuyển trang mượt mà
        return new Window(new NavigationPage(new MainPage()));
    }
}