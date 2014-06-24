using System.Windows;
using System.Windows.Documents;

namespace Tobi.Common._UnusedCode
{
    /*
     * Example of use:
     * 
     * 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Run curRun in WpfTreeHelper.GetChildren<Run>(flowRdr1.Document, true))
                curRun.MouseDown += new MouseButtonEventHandler(curRun_MouseDown);

        }
        void curRun_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = Mouse.GetPosition(this.flowRdr1);

           Run senderRun = (Run) sender;

            TextPointer ptr = RunExtensions.GetPositionFromPoint(senderRun, mousePosition);

            if (ptr != null)
            {

                string textAfterCursor = ptr.GetTextInRun(LogicalDirection.Forward);

                string textBeforeCursor = ptr.GetTextInRun(LogicalDirection.Backward);

                string[] textsAfterCursor = textAfterCursor.Split('.', ' ');

                string[] textsBeforeCursor = textBeforeCursor.Split('.', ' ');

                string currentWord = textsBeforeCursor[textsBeforeCursor.Length - 1] + textsAfterCursor[0];
            }

        }

     * 
     */
    public static class RunExtensions
    {
        public static TextPointer GetPositionFromPoint(Run thiz, Point searchForPoint)
        {
            TextPointer ptrCurCharcter = thiz.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
            TextPointer ptrNextCharcter = ptrCurCharcter.GetNextInsertionPosition(LogicalDirection.Forward);

            while (ptrNextCharcter != null)
            {
                Rect charcterInsertionPointRectangle = ptrCurCharcter.GetCharacterRect(LogicalDirection.Forward);
                Rect nextCharcterInsertionPointRectangle = ptrNextCharcter.GetCharacterRect(LogicalDirection.Backward);

                if (searchForPoint.X >= charcterInsertionPointRectangle.X
                    && searchForPoint.X <= nextCharcterInsertionPointRectangle.X
                    && searchForPoint.Y >= charcterInsertionPointRectangle.Top
                    && searchForPoint.Y <= charcterInsertionPointRectangle.Bottom)
                {
                    return ptrCurCharcter;
                }

                ptrCurCharcter = ptrCurCharcter.GetNextInsertionPosition(LogicalDirection.Forward);
                ptrNextCharcter = ptrNextCharcter.GetNextInsertionPosition(LogicalDirection.Forward);
            }
            return null;
        }
    }
}