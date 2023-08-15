using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace Joveler.DynLoader.Tests
{
    public class TestHelper
    {
        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }

        #region Temp Path
        private static int _tempPathCounter = 0;
        private static readonly object TempPathLock = new object();
        private static readonly RandomNumberGenerator SecureRandom = RandomNumberGenerator.Create();

        private static FileStream _lockFileStream = null;
        private static string _baseTempDir = null;
        public static string BaseTempDir()
        {
            lock (TempPathLock)
            {
                if (_baseTempDir != null)
                    return _baseTempDir;

                byte[] randBytes = new byte[4];
                string systemTempDir = Path.GetTempPath();

                do
                {
                    // Get 4B of random 
                    SecureRandom.GetBytes(randBytes);
                    uint randInt = BitConverter.ToUInt32(randBytes, 0);

                    _baseTempDir = Path.Combine(systemTempDir, $"JvlDynLoaderTests_{randInt:X8}");
                }
                while (Directory.Exists(_baseTempDir) || File.Exists(_baseTempDir));

                // Create base temp directory
                Directory.CreateDirectory(_baseTempDir);

                // Lock base temp directory
                string lockFilePath = Path.Combine(_baseTempDir, "f.lock");
                _lockFileStream = new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                return _baseTempDir;
            }
        }

        /// <summary>
        /// Delete BaseTempDir from disk. Call this method before termination of an application.
        /// </summary>
        public static void CleanBaseTempDir()
        {
            lock (TempPathLock)
            {
                if (_baseTempDir == null)
                    return;

                _lockFileStream?.Dispose();

                if (Directory.Exists(_baseTempDir))
                    Directory.Delete(_baseTempDir, true);
                _baseTempDir = null;
            }
        }

        /// <summary>
        /// Create temp directory with synchronization.
        /// Returned temp directory path is virtually unique per call.
        /// </summary>
        /// <remarks>
        /// Returned temp file path is unique per call unless this method is called uint.MaxValue times.
        /// </remarks>
        public static string GetTempDir()
        {
            // Never call BaseTempDir in the _tempPathLock, it would cause a deadlock!
            string baseTempDir = BaseTempDir();

            lock (TempPathLock)
            {
                string tempDir;
                do
                {
                    int counter = Interlocked.Increment(ref _tempPathCounter);
                    tempDir = Path.Combine(baseTempDir, $"d{counter:X8}");
                }
                while (Directory.Exists(tempDir) || File.Exists(tempDir));

                Directory.CreateDirectory(tempDir);
                return tempDir;
            }
        }

        /// <summary>
        /// Create temp file with synchronization.
        /// Returned temp file path is virtually unique per call.
        /// </summary>
        /// <remarks>
        /// Returned temp file path is unique per call unless this method is called uint.MaxValue times.
        /// </remarks>
        public static string GetTempFile(string ext = null)
        {
            return GetTempFile(null, ext);
        }

        /// <summary>
        /// Create temp file with synchronization.
        /// Returned temp file path is virtually unique per call.
        /// </summary>
        /// <remarks>
        /// Returned temp file path is unique per call unless this method is called uint.MaxValue times.
        /// </remarks>
        public static string GetTempFile(string baseName, string ext)
        {
            // Never call BaseTempDir in the _tempPathLock, it would cause a deadlock!
            string baseTempDir = BaseTempDir();

            // Use tmp by default / Remove '.' from ext
            if (ext == null)
                ext = "tmp";
            else
                ext = ext.Trim('.');

            lock (TempPathLock)
            {
                string tempFile;
                do
                {
                    int counter = Interlocked.Increment(ref _tempPathCounter);
                    string fileName;
                    if (baseName == null)
                        fileName = $"f{counter:X8}";
                    else
                        fileName = $"{baseName}_f{counter:X8}";
                    if (0 < ext.Length) // Not empty
                        fileName += $".{ext}";

                    tempFile = Path.Combine(baseTempDir, fileName);

                }
                while (Directory.Exists(tempFile) || File.Exists(tempFile));

                File.Create(tempFile).Dispose();
                return tempFile;
            }
        }

        /// <summary>
        /// Reserve temp file path with synchronization.
        /// Returned temp file path is virtually unique per call.
        /// </summary>
        /// <remarks>
        /// Returned temp file path is unique per call unless this method is called uint.MaxValue times.
        /// </remarks>
        public static string ReserveTempFile(string ext = null)
        {
            // Never call BaseTempDir in the _tempPathLock, it would cause a deadlock!
            string baseTempDir = BaseTempDir();

            // Use tmp by default / Remove '.' from ext
            ext = ext == null ? "tmp" : ext.Trim('.');

            lock (TempPathLock)
            {
                string tempFile;
                do
                {
                    int counter = Interlocked.Increment(ref _tempPathCounter);
                    tempFile = Path.Combine(baseTempDir, ext.Length == 0 ? $"f{counter:X8}" : $"f{counter:X8}.{ext}");
                }
                while (Directory.Exists(tempFile) || File.Exists(tempFile));
                return tempFile;
            }
        }
        #endregion
    }
}
