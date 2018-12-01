namespace TinyJitHook.Core.Extensions
{
    public static class PathHelper
    {
        /// <summary>
        /// Get a custom branded file path, defaults to _dumped
        /// </summary>
        /// <param name="filePath">The filepath to brand.</param>
        /// <param name="brand">The brand to give it (goes after the file name, but before the extension.</param>
        /// <returns>A full file path with the custom brand (C:\something_brandhere.exe).</returns>
        public static string GetFilePath(this string filePath, string brand = "_dumped")
        {
            string outFile = filePath;
            int index = outFile.LastIndexOf('.');
            if (index != -1)
            {
                outFile = outFile.Insert(index, brand);
            }
            return outFile;
        }
    }
}
