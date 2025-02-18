using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NECS;

public class DirectoryAdapter
    {
        #if GODOT
        //private static Godot.Directory _godotDirectory = new Godot.Directory();
        #endif

        public static bool Exists(string path)
        {
            #if NET
                return Directory.Exists(path);
            #elif GODOT
            using(var dir = new Godot.Directory())
                return dir.DirExists(path);
            #elif UNITY
                return System.IO.Directory.Exists(path);
            #endif
        }

        public static bool IsDirectory(string path)
        {
            #if GODOT
            using (var dir = new Godot.Directory())
                return dir.DirExists(path);
            #elif NET
                return System.IO.Directory.Exists(path);
            #elif UNITY
                return System.IO.Directory.Exists(path);
            #endif
        }

        public static DirectoryInfo CreateDirectory(string path, bool recursive = true)
        {
            #if NET
                return Directory.CreateDirectory(path);
            #elif GODOT
            using(var dir = new Godot.Directory())
            {
                var error = recursive ? dir.MakeDirRecursive(path) : dir.MakeDir(path); 
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to create directory: {error}");
                return new DirectoryInfo(path);
            }
            #elif UNITY
                return System.IO.Directory.CreateDirectory(path);
            #endif
        }

        public static void Delete(string path)
        {
            #if NET
                Directory.Delete(path);
            #elif GODOT
            using(var dir = new Godot.Directory())
            {
                var error = dir.Remove(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to delete directory: {error}");
            }
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
                    RecursiveDelete(path);
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
                return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            #elif GODOT
            using(var _godotDirectory = new Godot.Directory())
            {
                var files = new List<string>();
                var error = _godotDirectory.Open(path.FixPath());
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string fileName = _godotDirectory.GetNext();
                while (fileName != "")
                {
                    if (!_godotDirectory.CurrentIsDir())
                    {
                        files.Add(PathEx.Combine(path, fileName).FixPath());
                    }
                    fileName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return files.ToArray();
            }
            #elif UNITY
                return System.IO.Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            #endif
        }

        public static string[] GetDirectories(string path)
        {
            #if NET
                return Directory.GetDirectories(path);
            #elif GODOT
            using(var _godotDirectory = new Godot.Directory())
            {
                var directories = new List<string>();
                var error = _godotDirectory.Open(path.FixPath());
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string dirName = _godotDirectory.GetNext();
                while (dirName != "")
                {
                    if (_godotDirectory.CurrentIsDir())
                    {
                        directories.Add(PathEx.Combine(path, dirName).FixPath());
                    }
                    dirName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return directories.ToArray();
            }
            #elif UNITY
                return System.IO.Directory.GetDirectories(path);
            #endif
        }

        public static string GetParent(string path)
        {
            #if NET
                return Directory.GetParent(path)?.FullName;
            #elif GODOT
                return PathEx.GetDirectoryName(path);
            #elif UNITY
                return System.IO.Directory.GetParent(path)?.FullName;
            #endif
        }

        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            #if NET
                return Directory.EnumerateDirectories(path);
            #elif GODOT
            using(var _godotDirectory = new Godot.Directory())
            {
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
                        directories.Add(PathEx.Combine(path, dirName));
                    }
                    dirName = _godotDirectory.GetNext();
                }
                _godotDirectory.ListDirEnd();
                return directories;
            }
                
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
                using(var _godotDirectory = new Godot.Directory())
                {
                    if (!Exists(sourceDirName))
                        throw new DirectoryNotFoundException($"Source directory not found: {sourceDirName}");
                    if (Exists(destDirName))
                        throw new IOException($"Destination directory already exists: {destDirName}");

                    var error = _godotDirectory.Open(PathEx.GetDirectoryName(sourceDirName));
                    if (error != Godot.Error.Ok)
                        throw new IOException($"Failed to open source directory: {error}");

                    error = _godotDirectory.Rename(sourceDirName, destDirName);
                    if (error != Godot.Error.Ok)
                        throw new IOException($"Failed to move directory: {error}");
                }
                
            #elif UNITY
                System.IO.Directory.Move(sourceDirName, destDirName);
            #endif
        }

        #if GODOT
        /// <summary>
        /// NOT WORKING, OBSOLETE
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="IOException"></exception>
        private static void DeleteDirectoryRecursive(string path)
        {
            using(var _godotDirectory = new Godot.Directory())
            {
                var error = _godotDirectory.Open(path);
                if (error != Godot.Error.Ok)
                    throw new IOException($"Failed to open directory: {error}");

                _godotDirectory.ListDirBegin(true);
                string fileName = _godotDirectory.GetNext();
                while (fileName != "")
                {
                    string fullPath = PathEx.Combine(path, fileName);
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
            
        }

        public static void RecursiveCopy(string sourceFolder, string targetFolder)
        {
            if (!Exists(sourceFolder))
            {
                Godot.GD.PrintErr("Source directory does not exist: " + sourceFolder);
                return;
            }

            if (!Exists(targetFolder))
            {
                CreateDirectory(targetFolder);
            }

            var sourceDir = new Godot.Directory();
            if (sourceDir.Open(sourceFolder) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open source directory: " + sourceFolder);
                return;
            }

            sourceDir.ListDirBegin();

            string fileName;
            while ((fileName = sourceDir.GetNext()) != "")
            {
                if (fileName == "." || fileName == "..")
                    continue;

                string sourcePath = sourceFolder + "/" + fileName;
                string targetPath = targetFolder + "/" + fileName;

                if (IsDirectory(sourcePath))
                {
                    RecursiveCopy(sourcePath, targetPath);
                }
                else
                {
                    FileAdapter.Copy(sourcePath, targetPath);
                }
            }
            
            sourceDir.ListDirEnd();
        }

        private static void RecursiveDelete(string folderPath)
        {
            if (!DirectoryAdapter.Exists(folderPath))
            {
                Godot.GD.PrintErr("Directory does not exist: " + folderPath);
                return;
            }

            var dir = new Godot.Directory();
            if (dir.Open(folderPath) != Godot.Error.Ok)
            {
                Godot.GD.PrintErr("Failed to open directory: " + folderPath);
                return;
            }

            dir.ListDirBegin();

            string fileName;
            while ((fileName = dir.GetNext()) != "")
            {
                if (fileName == "." || fileName == "..")
                    continue;

                string currentPath = folderPath + "/" + fileName;

                if (IsDirectory(currentPath))
                {
                    RecursiveDelete(currentPath);
                }
                else
                {
                    FileAdapter.Delete(currentPath);
                }
            }

            dir.ListDirEnd();

            dir.Remove(folderPath);
            Godot.GD.Print("Deleted: " + folderPath);
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
                using(var dir = new Godot.Directory())
                {
                    dir.Remove(path);
                }
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
    #if GODOT
    public static void AppendText(string path, string content)
    {
        var file = new Godot.File();
        if (file.Open(path, Godot.File.ModeFlags.ReadWrite) != Godot.Error.Ok)
        {
            Godot.GD.PrintErr("Failed to open file for appending: " + path);
            return;
        }

        // Move the cursor to the end of the file
        file.SeekEnd();
        file.StoreString(content);
        file.Close();
        file.Dispose();
    }

    public static void AppendBytes(string path, byte[] content)
    {
        var file = new Godot.File();
        if (file.Open(path, Godot.File.ModeFlags.ReadWrite) != Godot.Error.Ok)
        {
            Godot.GD.PrintErr("Failed to open file for appending: " + path);
            return;
        }

        // Move the cursor to the end of the file
        file.SeekEnd();
        file.StoreBuffer(content);
        file.Close();
        file.Dispose();
    }
    #endif

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
