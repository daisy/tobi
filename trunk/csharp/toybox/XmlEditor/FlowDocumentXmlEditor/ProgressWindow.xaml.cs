using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using urakawa.progress;

namespace FlowDocumentXmlEditor
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        private bool mCancelPressed = false;
        private string mActionDescription = "";

        public static void ExecuteProgressAction(ProgressAction action, out bool wasCancelled)
        {
            wasCancelled = false;
            ProgressWindow w = new ProgressWindow();
            action.progress += new System.EventHandler<urakawa.events.progress.ProgressEventArgs>(w.action_progress);
            action.finished += new System.EventHandler<urakawa.events.progress.FinishedEventArgs>(w.action_finished);
            action.cancelled += new System.EventHandler<urakawa.events.progress.CancelledEventArgs>(w.action_cancelled);
            w.mActionDescription = action.getShortDescription();
            w.Title = w.mActionDescription;
            Thread executeThread = new Thread(ExecuteWorker);
            executeThread.Start(action);
            bool? result = w.ShowDialog();
            if (result.HasValue) wasCancelled = !result.Value;
        }

        private static void ExecuteWorker(object o)
        {
            ProgressAction action = o as ProgressAction;
            if (action !=null) action.execute();
        }



        private void action_cancelled(object sender, urakawa.events.progress.CancelledEventArgs e)
        {
            DoClose();
        }

        private void action_finished(object sender, urakawa.events.progress.FinishedEventArgs e)
        {
            DoClose();
        }

        private void DoClose()
        {
            if (Dispatcher.CheckAccess())
            {
                if (IsLoaded)
                {
                    DialogResult = !mCancelPressed;
                    Close();
                }
                    
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(DoClose));
            }
        }

        private void action_progress(object sender, urakawa.events.progress.ProgressEventArgs e)
        {
            double val = e.Current;
            double max = e.Total;
            if (val != mVal || max != mMax || 0 != mMin)
            {
                mMin = 0;
                mVal = val;
                mMax = max;
                Debug.Print("Progress: Current={0:0}, Total={1:0}, IsCancelled={2}", val, max, e.IsCancelled);
                //Thread.Sleep(10);
                UpdateUI();
            }
            if (mCancelPressed) e.Cancel();
        }

        private double mVal = 0;
        private double mMin = 0;
        private double mMax = 0;

        private void UpdateUI()
        {
            if (mProgressBar.Dispatcher.CheckAccess())
            {
                mProgressBar.Value = mVal;
                mProgressBar.Minimum = mMin;
                mProgressBar.Maximum = mMax;
                this.Title = string.Format("\"{2}\" {0:0}/{1:0}", mVal, mMax, mActionDescription);
            }
            else
            {
                mProgressBar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(UpdateUI));
                return;
            }
        }

        private void mCancelButton_Click(object sender, RoutedEventArgs e)
        {
            mCancelPressed = true;
        }
    }
}
