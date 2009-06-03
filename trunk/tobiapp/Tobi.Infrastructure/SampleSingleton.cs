namespace Tobi.Infrastructure
{
    public class SampleSingleton
    {
        SampleSingleton()
        {
        }

        public static SampleSingleton Instance
        {
            get
            {
                return Nested.INSTANCE;
            }
        }

        class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly SampleSingleton INSTANCE = new SampleSingleton();
        }
    }
}
