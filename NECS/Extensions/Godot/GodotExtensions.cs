#if GODOT && !GODOT4_0_OR_GREATER
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GodotExtensions
{
    public static Vector3 MoveTowardDistance(this Vector3 from, Vector3 to, float distance_delta)
    {
        return from.MoveToward(to, distance_delta * from.DistanceTo(to));
    }

    public static Vector3 Set(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3( X == null ? original.x : (float)X, Y == null ? original.y : (float)Y, Z == null ? original.z : (float)Z);
    }

    public static Vector3 Increase(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3(X == null ? original.x : original.x + (float)X, Y == null ? original.y : original.y + (float)Y, Z == null ? original.z : original.z + (float)Z);
    }
}
#endif
#if GODOT4_0_OR_GREATER
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GodotExtensions
{
    public static Vector3 MoveTowardDistance(this Vector3 from, Vector3 to, float distance_delta)
    {
        return from.MoveToward(to, distance_delta * from.DistanceTo(to));
    }

    public static Vector3 Set(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3( X == null ? original.X : (float)X, Y == null ? original.Y : (float)Y, Z == null ? original.Z : (float)Z);
    }

    public static Vector3 Increase(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3(X == null ? original.X : original.X + (float)X, Y == null ? original.Y : original.Y + (float)Y, Z == null ? original.Z : original.Z + (float)Z);
    }

    public static void RecursiveCopy(string sourceFolder, string targetFolder)
    {
        // Убедимся, что исходная папка существует
        if (!DirectoryExists(sourceFolder))
        {
            GD.PrintErr("Source directory does not exist: " + sourceFolder);
            return;
        }

        // Создаем целевую директорию, если ее нет
        if (!DirectoryExists(targetFolder))
        {
            CreateDirectory(targetFolder);
        }

        // Получаем список всех файлов и папок в исходной директории
        var sourceDir = new Godot.Directory();
        if (sourceDir.Open(sourceFolder) != Error.Ok)
        {
            GD.PrintErr("Failed to open source directory: " + sourceFolder);
            return;
        }

        sourceDir.ListDirBegin();

        string fileName;
        while ((fileName = sourceDir.GetNext()) != "")
        {
            // Игнорируем текущую и родительскую директории
            if (fileName == "." || fileName == "..")
                continue;

            string sourcePath = sourceFolder + "/" + fileName;
            string targetPath = targetFolder + "/" + fileName;

            if (IsDirectory(sourcePath))
            {
                // Если это директория, рекурсивно копируем ее
                RecursiveCopy(sourcePath, targetPath);
            }
            else
            {
                // Если это файл, копируем его
                CopyFile(sourcePath, targetPath);
            }
        }
        
        sourceDir.ListDirEnd();
    }

    // Проверка, существует ли директория
    public static bool DirectoryExists(string path)
    {
        var dir = new Godot.Directory();
        return dir.DirExists(path);
    }

    // Создание директории, если она не существует
    public static void CreateDirectory(string path)
    {
        var dir = new Godot.Directory();
        if (dir.MakeDir(path) != Error.Ok)
        {
            GD.PrintErr("Failed to create directory: " + path);
        }
        dir.Dispose();
    }

    // Проверка, является ли путь директорией
    public static bool IsDirectory(string path)
    {
        var dir = new Godot.Directory();
        dir.Dispose();
        return dir.DirExists(path);
    }

    // Копирование файла из sourcePath в targetPath
    public static void CopyFile(string sourcePath, string targetPath)
    {
        var file = new Godot.File();
        var dest = new Godot.File();
        // Открываем исходный файл для чтения
        if (file.Open(sourcePath, Godot.File.ModeFlags.Read) != Error.Ok)
        {
            GD.PrintErr("Failed to open source file: " + sourcePath);
            file.Close();
            dest.Close();
            file.Dispose();
            dest.Dispose();
            return;
        }

        // Создаем или открываем целевой файл для записи
        if (dest.Open(targetPath, Godot.File.ModeFlags.Write) != Error.Ok)
        {
            GD.PrintErr("Failed to open target file: " + targetPath);
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
    }
}
#endif
#if GODOT && !GODOT4_0_OR_GREATER


#endif