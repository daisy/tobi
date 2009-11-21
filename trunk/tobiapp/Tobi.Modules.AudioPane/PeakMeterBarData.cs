namespace Tobi.Plugin.AudioPane
{
    public class PeakMeterBarData
    {
        //public delegate void PeakMeterRefreshDelegate();
        //private readonly PeakMeterRefreshDelegate m_PeakMeterRefreshDelegate;

        public PeakMeterBarData() //PeakMeterRefreshDelegate del)
        {
            //m_PeakMeterRefreshDelegate = del;

            PeakOverloadCount = 0;
            ValueDb = MinimumDb;
        }

        private double m_ValueDb;
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
                }
            }
        }

        private double m_MinimumDb = -72;
        public double MinimumDb
        {
            get
            {
                return m_MinimumDb;
            }
// ReSharper disable UnusedMember.Local
            private set
// ReSharper restore UnusedMember.Local
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
                    //m_PeakMeterRefreshDelegate();
                }
            }
        }

        public int PeakOverloadCount
        {
            get;
            set;
        }

        public double DbToPixels(double totalPixels)
        {
            double pixels;
            if (ValueDb < MinimumDb)
            {
                pixels = 0;
            }
            else if (ValueDb > 0)
            {
                pixels = totalPixels;
            }
            else
            {
                pixels = (MinimumDb - ValueDb) * totalPixels / MinimumDb;
            }
            return pixels;
        }
    }
}