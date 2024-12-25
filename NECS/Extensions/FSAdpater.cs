using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class DirectoryAdapter
    {
        #if GODOT
        private static Godot.Directory _godotDirectory = new Godot.Directory();
        #endif

        public static bool Exists(string path)
        {
            #if NET
                return Directory.Exists(path);
            #elif GODOT
                return _godotDirectory.DirExists(path);
            #elif UNITY
                return System.IO.Directory.Exists(path);
            #endif
        }

        public static DirectoryInfo CreateDirectory(string path)
        {
            #if NET
                return Directory.CreateDirectory(path);
            #elif GODOT
                var error = _godotDirectory.MakeDirRecursive(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to create directory: {error}");
                return new DirectoryInfo(path);
            #elif UNITY
                return System.IO.Directory.CreateDirectory(path);
            #endif
        }

        public static void Delete(string path)
        {
            #if NET
                Directory.Delete(path);
            #elif GODOT
                var error = _godotDirectory.Remove(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to delete directory: {error}");
            #elif UNITY
                System.IO.Directory.Delete(path);
            #endif
        }

        public static void Delete(string path, bool recursive)
        {
            #if NET
                Directory.Delete(path, recursive);
            #elif GODOT
                if (recursive)
                {
                    DeleteDirectoryRecursive(path);
                }
                else
                {
                    Delete(path);
                }
            #elif UNITY
                System.IO.Directory.Delete(path, recursive);
            #endif
        }

        public static string[] GetFiles(string path)
        {
            #if NET
                return Directory.GetFiles(path);
            #elif GODOT
                var files = new List<string>();
                var error = _godotDirectory.Open(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string fileName = _godotDirectory.GetNext();
                while (fileName != "")
                {
                    if (!_godotDirectory.CurrentIsDir())
                    {
                        files.Add(Path.Combine(path, fileName));
                    }
                    fileName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return files.ToArray();
            #elif UNITY
                return System.IO.Directory.GetFiles(path);
            #endif
        }

        public static string[] GetDirectories(string path)
        {
            #if NET
                return Directory.GetDirectories(path);
            #elif GODOT
                var directories = new List<string>();
                var error = _godotDirectory.Open(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string dirName = _godotDirectory.GetNext();
                while (dirName != "")
                {
                    if (_godotDirectory.CurrentIsDir())
                    {
                        directories.Add(Path.Combine(path, dirName));
                    }
                    dirName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return directories.ToArray();
            #elif UNITY
                return System.IO.Directory.GetDirectories(path);
            #endif
        }

        public static string GetParent(string path)
        {
            #if NET
                return Directory.GetParent(path)?.FullName;
            #elif GODOT
                return System.IO.Path.GetDirectoryName(path);
            #elif UNITY
                return System.IO.Directory.GetParent(path)?.FullName;
            #endif
        }

        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            #if NET
                return Directory.EnumerateDirectories(path);
            #elif GODOT
                var directories = new List<string>();
                var error = _godotDirectory.Open(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string dirName = _godotDirectory.GetNext();
                while (dirName != "")
                {
                    if (_godotDirectory.CurrentIsDir())
                    {
                        directories.Add(Path.Combine(path, dirName));
                    }
                    dirName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return directories;
            #elif UNITY
                return System.IO.Directory.EnumerateDirectories(path);
            #endif
        }

        public static void Move(string sourceDirName, string destDirName)
        {
            #if NET
                Directory.Move(sourceDirName, destDirName);
            #elif GODOT
                // Godot doesn't have a direct move directory method, so we implement it
                if (!Exists(sourceDirName))
                    throw new DirectoryNotFoundException($"Source directory not found: {sourceDirName}");
                if (Exists(destDirName))
                    throw new IOException($"Destination directory already exists: {destDirName}");

                var error = _godotDirectory.Open(System.IO.Path.GetDirectoryName(sourceDirName));
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open source directory: {error}");

                error = _godotDirectory.Rename(sourceDirName, destDirName);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to move directory: {error}");
            #elif UNITY
                System.IO.Directory.Move(sourceDirName, destDirName);
            #endif
        }

        #if GODOT
        private static void DeleteDirectoryRecursive(string path)
        {
            var error = _godotDirectory.Open(path);
            if (error != Godot.Error.Ok)
                throw new IOException($"Failed to open directory: {error}");

            _godotDirectory.ListDirBegin(true);
            string fileName = _godotDirectory.GetNext();
            while (fileName != "")
            {
                string fullPath = Path.Combine(path, fileName);
                if (_godotDirectory.CurrentIsDir())
                {
                    DeleteDirectoryRecursive(fullPath);
                }
                else
                {
                    error = _godotDirectory.Remove(fullPath);
                    if (error != Godot.Error.Ok)
                        throw new IOException($"Failed to delete file: {error}");
                }
                fileName = _godotDirectory.GetNext();
            }
            _godotDirectory.ListDirEnd();
            
            error = _godotDirectory.Remove(path);
            if (error != Godot.Error.Ok)
                throw new IOException($"Failed to delete directory: {error}");
        }
        #endif
    }

public class FileAdapter
{
    public static bool Exists(string path)
    {
        #if NET
            return File.Exists(path);
        #elif GODOT
            var file = new Godot.File();
            bool exists = file.FileExists(path);
            file.Dispose();
            return exists;
        #elif UNITY
            return UnityEngine.Windows.File.Exists(path);
        #endif
    }

    public static void Copy(string sourceFileName, string destFileName)
    {
        #if NET
            File.Copy(sourceFileName, destFileName);
        #elif GODOT
            var file = new Godot.File();
            var dest = new Godot.File();
            
            if (file.Open(sourceFileName, Godot.File.ModeFlags.Read) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open source file: " + sourceFileName);
                file.Close();
                dest.Close();
                file.Dispose();
                dest.Dispose();
                return;
            }

            if (dest.Open(destFileName, Godot.File.ModeFlags.Write) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open target file: " + destFileName);
                file.Close();
                dest.Close();
                file.Dispose();
                dest.Dispose();
                return;
            }

            dest.StoreBuffer(file.GetBuffer(Convert.ToInt64(file.GetLen())));
            
            file.Close();
            dest.Close();
            file.Dispose();
            dest.Dispose();
        #elif UNITY
            UnityEngine.Windows.File.Copy(sourceFileName, destFileName);
        #endif
    }

    public static void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
        #if NET
            File.Copy(sourceFileName, destFileName, overwrite);
        #elif GODOT
            if (!overwrite && Exists(destFileName))
                return;
            Copy(sourceFileName, destFileName);
        #elif UNITY
            UnityEngine.Windows.File.Copy(sourceFileName, destFileName, overwrite);
        #endif
    }

    public static void Delete(string path)
    {
        #if NET
            File.Delete(path);
        #elif GODOT
            var file = new Godot.File();
            if (file.FileExists(path))
            {
                new Godot.Directory().Remove(path);
                Godot.GD.Print("Deleted file: " + path);
            }
            file.Dispose();
        #elif UNITY
            UnityEngine.Windows.File.Delete(path);
        #endif
    }

    public static FileStream Open(string path, FileMode mode)
    {
        #if NET
            return File.Open(path, mode);
        #elif GODOT
            throw new NotSupportedException("Use OpenText, OpenRead, or OpenWrite instead for Godot");
        #elif UNITY
            return new FileStream(path, mode);
        #endif
    }

    public static StreamReader OpenText(string path)
    {
        #if NET
            return File.OpenText(path);
        #elif GODOT
            var file = new Godot.File();
            if (file.Open(path, Godot.File.ModeFlags.Read) != Godot.Error.Ok)
            {
                file.Dispose();
                throw new System.IO.IOException("Could not open file: " + path);
            }
            var content = file.GetAsText();
            file.Close();
            file.Dispose();
            return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        #elif UNITY
            return new StreamReader(path);
        #endif
    }

    public static string ReadAllText(string path)
    {
        #if NET
            return File.ReadAllText(path);
        #elif GODOT
            var file = new Godot.File();
            if (file.Open(path, Godot.File.ModeFlags.Read) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open file for reading: " + path);
                file.Dispose();
                return null;
            }
            
            string content = file.GetAsText();
            file.Close();
            file.Dispose();
            return content;
        #elif UNITY
            return UnityEngine.Windows.File.ReadAllText(path);
        #endif
    }

    public static void WriteAllText(string path, string contents)
    {
        #if NET
            File.WriteAllText(path, contents);
        #elif GODOT
            var file = new Godot.File();
            if (file.Open(path, Godot.File.ModeFlags.Write) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open file for writing: " + path);
                file.Dispose();
                return;
            }
            
            file.StoreString(contents);
            file.Close();
            file.Dispose();
        #elif UNITY
            UnityEngine.Windows.File.WriteAllText(path, contents);
        #endif
    }

    public static byte[] ReadAllBytes(string path)
    {
        #if NET
            return File.ReadAllBytes(path);
        #elif GODOT
            var file = new Godot.File();
            if (file.Open(path, Godot.File.ModeFlags.Read) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open file for reading bytes: " + path);
                file.Dispose();
                return null;
            }
            
            var buffer = file.GetBuffer(Convert.ToInt64(file.GetLen()));
            file.Close();
            file.Dispose();
            return buffer;
        #elif UNITY
            return UnityEngine.Windows.File.ReadAllBytes(path);
        #endif
    }

    public static void WriteAllBytes(string path, byte[] bytes)
    {
        #if NET
            File.WriteAllBytes(path, bytes);
        #elif GODOT
            var file = new Godot.File();
            if (file.Open(path, Godot.File.ModeFlags.Write) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open file for writing bytes: " + path);
                file.Dispose();
                return;
            }
            
            file.StoreBuffer(bytes);
            file.Close();
            file.Dispose();
        #elif UNITY
            UnityEngine.Windows.File.WriteAllBytes(path, bytes);
        #endif
    }

    public static void Move(string sourceFileName, string destFileName)
    {
        #if NET
            File.Move(sourceFileName, destFileName);
        #elif GODOT
            Copy(sourceFileName, destFileName);
            Delete(sourceFileName);
        #elif UNITY
            UnityEngine.Windows.File.Move(sourceFileName, destFileName);
        #endif
    }

    public static DateTime GetLastWriteTime(string path)
    {
        #if NET
            return File.GetLastWriteTime(path);
        #elif GODOT
            var file = new Godot.File();
            if (!file.FileExists(path))
            {
                file.Dispose();
                throw new System.IO.FileNotFoundException("File not found", path);
            }
            // Note: Godot doesn't provide direct access to file timestamps
            file.Dispose();
            return DateTime.Now; // Default fallback
        #elif UNITY
            return UnityEngine.Windows.File.GetLastWriteTime(path);
        #endif
    }
}