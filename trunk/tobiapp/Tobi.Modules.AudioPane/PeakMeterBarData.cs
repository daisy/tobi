namespace Tobi.Modules.AudioPane
{
    public class PeakMeterBarData
    {
        public delegate void PeakMeterRefreshDelegate();

        private double m_ValueDb;
        private double m_MinimumDb = -72;
        private PeakMeterRefreshDelegate m_PeakMeterRefreshDelegate;
        private int m_PeakOverloadCount;

        public PeakMeterBarData(PeakMeterRefreshDelegate del)
        {
            m_PeakOverloadCount = 0;
            m_PeakMeterRefreshDelegate = del;
            ValueDb = MinimumDb;
            //ShownValueDb = MinimumDb;
            //m_FallbackSecondsPerDb = TimeSpan.Parse("00:00:00.5000000");
            //m_FallbackThread = new Thread(new ThreadStart(FallbackWorker));
        }

        public double ValueDb
        {
            get
            {
                return m_ValueDb;
            }
            set
            {
                double newValue;
                if (value > 0)
                {
                    newValue = 0;
                }
                else if (value < MinimumDb)
                {
                    newValue = MinimumDb;
                }
                else
                {
                    newValue = value;
                }
                if (newValue != m_ValueDb)
                {
                    m_ValueDb = newValue;

                    /*
                    if (!m_FallbackThread.IsAlive)
                    {
                        m_FallbackThread = new Thread(new ThreadStart(FallbackWorker));
                        m_FallbackThread.Start();
                    }*/
                }
            }
        }


        public double MinimumDb
        {
            get
            {
                return m_MinimumDb;
            }
            set
            {
                double newValue = value;
                if (newValue > -1)
                {
                    newValue = -1;
                }
                if (m_MinimumDb != newValue)
                {
                    m_MinimumDb = newValue;
                    ValueDb = ValueDb;
                    m_PeakMeterRefreshDelegate();
                    //ShownValueDb = ValueDb;
                }
            }
        }

        public int PeakOverloadCount
        {
            get
            {
                return m_PeakOverloadCount;
            }
            set
            {
                m_PeakOverloadCount = value;
            }
        }

        /*
         * 
        private TimeSpan m_FallbackSecondsPerDb;

        private Thread m_FallbackThread;
        private Mutex m_ValueDbMutex = new Mutex();

        private double m_ShownValueDb;
         
        public void ForceFullFallback()
        {
            if (m_FallbackThread.IsAlive)
            {
                m_FallbackThread.Abort();
            }
            ShownValueDb = ValueDb;
        }

    
        private double ShownValueDb
        {
            get
            {
                return m_ShownValueDb;
            }

            set
            {
                double newValue;
                if (value > 0)
                {
                    newValue = 0;
                }
                else if (value < MinimumDb)
                {
                    newValue = MinimumDb;
                }
                else
                {
                    newValue = value;
                }
                if (newValue != m_ShownValueDb)
                {
                    m_ShownValueDb = newValue;
                    m_PeakMeterRefreshDelegate();
                }
            }
        }

        private void FallbackWorker()
        {
            try
            {
                DateTime latestUpdateTime = DateTime.Now;
                while (true)
                {
                    TimeSpan timeSinceLatestUpdate = DateTime.Now.Subtract(latestUpdateTime);
                    double maxDiff = Double.PositiveInfinity;
                    if (m_FallbackSecondsPerDb.TotalMilliseconds > 0)
                    {
                        maxDiff = timeSinceLatestUpdate.TotalMilliseconds / m_FallbackSecondsPerDb.TotalMilliseconds;
                    }
                    latestUpdateTime += timeSinceLatestUpdate;
                    if (ValueDb < ShownValueDb - maxDiff)
                    {
                        ShownValueDb -= maxDiff;
                    }
                    else
                    {
                        ShownValueDb = ValueDb;
                    }
                    
                    if (ShownValueDb == ValueDb)
                    {
                        return;
                    }
                    m_ValueDbMutex.WaitOne();
                    try
                    {
                        if (ShownValueDb == ValueDb)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        m_ValueDbMutex.ReleaseMutex();
                    }
                     
                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }
         ~PeakMeterBarData()
        {
            if (m_FallbackThread.IsAlive) m_FallbackThread.Abort();
        }
         */

        public double DbToPixels(double totalPixels)
        {
            double h;
            if (ValueDb < MinimumDb)
            {
                h = 0;
            }
            else if (ValueDb > 0)
            {
                h = totalPixels;
            }
            else
            {
                h = (MinimumDb - ValueDb) * totalPixels / MinimumDb;
            }
            return h;
        }
    }
}