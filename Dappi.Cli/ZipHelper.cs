using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Dappi.Cli;

public static class ZipHelper
{
    public static void ExtractZipFile(string archiveFilenameIn, string outFolder, string? explicitFolderInZip = null)
    {
        Console.WriteLine($"Extracting Project Template Zip:{archiveFilenameIn}...");
        Console.WriteLine($"Extracting To:{outFolder}...");

        ZipFile? zf = null;
        try
        {
            var fs = File.OpenRead(archiveFilenameIn);
            zf = new ZipFile(fs);
            var firstZipEntry = zf[0];

            foreach (ZipEntry zipEntry in zf)
            {
                if (!zipEntry.IsFile)
                {
                    continue; // Ignore directories
                }

                var entryFileName = zipEntry.Name;
                // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                var buffer = new byte[4096]; // 4K is optimum
                var zipStream = zf.GetInputStream(zipEntry);

                // Manipulate the output filename here as desired.
                //remove first level folder
                if (firstZipEntry.IsDirectory)
                {
                    entryFileName = entryFileName.Substring(entryFileName.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1);
                }

                if (!string.IsNullOrWhiteSpace(explicitFolderInZip))
                {
                    if (!entryFileName.StartsWith(explicitFolderInZip.EnsureEndsWith('/')))
                    {
                        continue;
                    }

                    //remove explicitFolder level 
                    entryFileName = entryFileName.Substring(entryFileName.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1);
                }

                var fullZipToPath = Path.Combine(outFolder, entryFileName);
                var directoryName = Path.GetDirectoryName(fullZipToPath);
                if (directoryName?.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                using var streamWriter = File.Create(fullZipToPath);
                
                StreamUtils.Copy(zipStream, streamWriter, buffer);
            }
        }
        finally
        {
            if (zf is not null)
            {
                zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                zf.Close(); // Ensure we release resources
            }
        }
    }
}