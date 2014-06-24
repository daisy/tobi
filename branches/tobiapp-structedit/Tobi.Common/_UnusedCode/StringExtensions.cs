namespace Tobi.Common._UnusedCode
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a byte array based
        /// on the given
        /// <see cref="System.Text.Encoding"/>.
        /// </summary>
        /// <typeparam name="TEncoding">The
        /// Encoding to use for conveting from the string of characters to bytes (e.g. ASCIIEncoding).</typeparam>
        /// <param name="source">The string to convert
        /// to a byte array.</param>
        /// <returns>The byte array corresponding to the string and based
        /// on the supplied
        /// <see cref="System.Text.Encoding"/>
        /// </returns>
        /// <exception cref="System.ArgumentNullException" />
        public static byte[] ToByteArray<TEncoding>(
            this string source)
            where TEncoding : System.Text.Encoding, new()
        {
            return new TEncoding().GetBytes(source.ToCharArray());
        }
    }
}