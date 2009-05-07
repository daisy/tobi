using System.Windows;

namespace Tobi.Infrastructure.Commanding
{
    public class HelloWorldCommand : CommandExtension<HelloWorldCommand>
    {
        public override void Execute(object parameter)
        {
            MessageBox.Show("Hello world.");
        }
    }
}