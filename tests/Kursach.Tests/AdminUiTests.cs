using System.Reflection;
using System.Windows;
using kursach;

namespace Kursach.Tests;

public sealed class AdminUiTests
{
    [Fact]
    public void AdminMode_TogglesPanels()
    {
        TestHelpers.RunSta(() =>
        {
            var root = TestHelpers.FindRepoRoot();
            Environment.CurrentDirectory = root;

            if (Application.Current is null)
            {
                _ = new Application();
            }

            var window = new MainWindow();

            var adminPanel = window.FindName("AdminPanel") as FrameworkElement;
            var contextPanel = window.FindName("ContextPanel") as FrameworkElement;

            Assert.NotNull(adminPanel);
            Assert.NotNull(contextPanel);

            var setAdmin = typeof(MainWindow).GetMethod("SetAdminMode", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(setAdmin);

            setAdmin!.Invoke(window, new object[] { true });
            Assert.Equal(Visibility.Visible, adminPanel!.Visibility);
            Assert.Equal(Visibility.Collapsed, contextPanel!.Visibility);

            setAdmin.Invoke(window, new object[] { false });
            Assert.Equal(Visibility.Collapsed, adminPanel.Visibility);
            Assert.Equal(Visibility.Visible, contextPanel.Visibility);

            window.Close();
        });
    }
}
