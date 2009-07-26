using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Tobi.Common._UnusedCode
{
    /// <summary>  
    /// This class provides the ability to easily attach Help functionality  
    /// to Framework elements. To use it, you need to  
    /// add a reference to the HelpProvider in your XAML. The FilenameProperty  
    /// is used to specify the name of the helpfile, and the KeywordProperty specifies  
    /// the keyword to be used with the search.  
    /// </summary>  
    /// <remarks>  
    /// The FilenameProperty can be at a higher level of the visual tree than  
    /// the KeywordProperty, so you don't need to set the filename each time.  
    /// </remarks>  
    public static class HelpProvider
    {
        /// <summary>  
        /// Initialize a new instance of <see cref="Help"/>.  
        /// </summary>  
        static HelpProvider()
        {
            // Rather than having to manually associate the Help command, let's take care  
            // of this here.  Note: ApplicationCommands.Help is bound to the F1 key by default
            CommandManager.RegisterClassCommandBinding(typeof(FrameworkElement),
                                                       new CommandBinding(ApplicationCommands.Help,
                                                                          new ExecutedRoutedEventHandler(Executed),
                                                                          new CanExecuteRoutedEventHandler(CanExecute)));
        }

        #region Filename

        /// <summary>  
        /// Filename Attached Dependency Property  
        /// </summary>  
        public static readonly DependencyProperty FilenameProperty =
            DependencyProperty.RegisterAttached("Filename", typeof(string), typeof(HelpProvider));

        /// <summary>  
        /// Gets the Filename property.  
        /// </summary>  
        public static string GetFilename(DependencyObject d)
        {
            return (string)d.GetValue(FilenameProperty);
        }

        /// <summary>  
        /// Sets the Filename property.  
        /// </summary>  
        public static void SetFilename(DependencyObject d, string value)
        {
            d.SetValue(FilenameProperty, value);
        }

        #endregion

        #region Keyword

        /// <summary>  
        /// Keyword Attached Dependency Property  
        /// </summary>  
        public static readonly DependencyProperty KeywordProperty =
            DependencyProperty.RegisterAttached("Keyword", typeof(string), typeof(HelpProvider));

        /// <summary>  
        /// Gets the Keyword property.  
        /// </summary>  
        public static string GetKeyword(DependencyObject d)
        {
            return (string)d.GetValue(KeywordProperty);
        }

        /// <summary>  
        /// Sets the Keyword property.  
        /// </summary>  
        public static void SetKeyword(DependencyObject d, string value)
        {
            d.SetValue(KeywordProperty, value);
        }
        #endregion

        #region Helpers
        private static void CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            FrameworkElement el = sender as FrameworkElement;
            if (el != null)
            {
                string fileName = FindFilename(el);
                if (!string.IsNullOrEmpty(fileName))
                    args.CanExecute = true;
            }
        }

        private static void Executed(object sender, ExecutedRoutedEventArgs args)
        {
            // Call ShowHelp.  
            DependencyObject parent = args.OriginalSource as DependencyObject;
            string keyword = GetKeyword(parent);
            if (!string.IsNullOrEmpty(keyword))
            {
                System.Windows.Forms.Help.ShowHelp(null, FindFilename(parent), keyword);
            }
            else
            {
                System.Windows.Forms.Help.ShowHelp(null, FindFilename(parent));
            }
        }

        private static string FindFilename(DependencyObject sender)
        {
            if (sender != null)
            {
                string fileName = GetFilename(sender);
                if (!string.IsNullOrEmpty(fileName))
                    return fileName;
                return FindFilename(VisualTreeHelper.GetParent(sender));
            }
            return null;
        }
        #endregion

    }
}