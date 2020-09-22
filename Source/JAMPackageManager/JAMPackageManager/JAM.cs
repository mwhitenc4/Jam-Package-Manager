using System;
using System.Collections.Generic;
using System.IO;

public class JAM
{
    public byte[] Data { get; set; } // The entire JAM file, stored in bytes
    public string FileName { get; set; }
    public short FileCount { get; set; }
    public short ExtensionCount { get; set; }
    public char[][] FileNames { get; set; }
    public char[][] ExtensionNames { get; set; }
    public int FirstOffset { get; set; } // The offset for the first file in the JAM package
    public List<FileInfo> Files = new List<FileInfo>(); // Each file within the JAM file

    public static JAM Read(string fileName)
    {
        if (fileName.Length < 4 || fileName.Substring(fileName.Length - 4, 4).ToUpper() != ".JAM")
        {
            return null;
        }

        JAM current = new JAM();

        current.FileName = fileName;

        string fileNameWithPath = current.FileName;

        if (File.Exists(fileNameWithPath))
        {
            current.Data = File.ReadAllBytes(fileNameWithPath); // Load the file into the byte array

            // Create a backup if it doesn't already exist
            if (!File.Exists(fileNameWithPath + ".bak"))
            {
                File.WriteAllBytes(fileNameWithPath + ".bak", current.Data);
            }
        }
        else return null;

        current.LoadData();
        current.LoadFiles();
        return current;
    }

    void LoadData()
    {
        LoadFirstOffset();
        LoadFileCount();
        LoadExtensionCount();
    }

    void LoadFiles()
    {
        LoadFileNames();
        LoadExtensionNames();

        // File meta data appears immediately after extensions (+ 4 bytes, likely used for folder info)
        int location = 0x20 + (FileCount * 8) + (ExtensionCount * 4) + 4;

        int currentFile = 0; // Incrementer to keep track of current file
        while (location < FirstOffset)
        {
            byte[] fileIDBytes = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                fileIDBytes[i] = Data[location + i];
            }

            byte[] extensionIDBytes = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                extensionIDBytes[i] = Data[(location + 2) + i];
            }

            byte[] offsetBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                offsetBytes[i] = Data[(location + 4) + i];
            }

            short fileID = BitConverter.ToInt16(fileIDBytes, 0);
            // Sometimes only one byte is used for the file ID
            if (fileID > FileCount || fileID < 0)
            {
                fileID = fileIDBytes[0];
            }
            short extensionID = BitConverter.ToInt16(extensionIDBytes, 0);
            int offset = BitConverter.ToInt32(offsetBytes, 0);

            // I don't know how folders work, but I know that they fall outside of these bounds
            if (extensionID > 0 && extensionID < ExtensionCount)
            {
                FileInfo file = new FileInfo(this);
                Files.Add(file);

                file.LoadFileName(fileID);
                file.LoadExtensionName(extensionID);
                file.LoadFileOffset(offset);
                file.LoadMetaOffset(location);
                file.LoadFileSize();

                currentFile++;
            }
            location += 8;
        }
    }

    void LoadFileNames()
    {
        for (int i = 0; i < FileCount; i++)
        {
            FileNames[i] = new char[8];
            // All file names are 8 characters (bytes)
            for (int j = 0; j < 8; j++)
            {
                FileNames[i][j] = (char)Data[0x20 + (i * 8) + j]; // File names always start at 0x20
            }
        }
    }

    void LoadExtensionNames()
    {
        int offset = 0x20 + FileCount * 8; // Extensions appear immediately after file names

        for (int i = 0; i < ExtensionCount; i++)
        {
            ExtensionNames[i] = new char[4];
            // All extension names are 4 characters (bytes)
            for (int j = 0; j < 4; j++)
            {
                ExtensionNames[i][j] = (char)Data[offset + (i * 4) + j];
            }
        }
    }

    // This will read through a certain number of bytes at an offset, and return them
    public byte[] ReadBytes(int offset, int size)
    {
        byte[] container = new byte[size]; // Create the container

        // Loop through each of the bytes
        for (int i = 0; i < size; i++)
        {
            container[i] = Data[offset + i]; // Grab the bytes from the file's data
        }

        return container;
    }

    void LoadFirstOffset()
    {
        int offset = 0x08;
        int size = 4;

        // Convert the "first offset" bytes to an integer
        FirstOffset = BitConverter.ToInt32(
            ReadBytes(offset, size),
            0
        );
    }

    void LoadFileCount()
    {
        int offset = 0x1C;
        int size = 2;

        // Convert the "first offset" bytes to an integer
        FileCount = BitConverter.ToInt16(
            ReadBytes(offset, size),
            0
        );

        FileNames = new char[FileCount][];
    }

    void LoadExtensionCount()
    {
        int offset = 0x1E;
        int size = 2;

        // Convert the "first offset" bytes to an integer
        ExtensionCount = BitConverter.ToInt16(
            ReadBytes(offset, size),
            0
        );

        ExtensionNames = new char[ExtensionCount][];
    }

    public void Export()
    {
        File.WriteAllBytes(FileName, Data);
    }

    public FileInfo FindFile(string fileName)
    {
        string[] fileNameParts = fileName.Split('.'); // Split file by dot

        // Check if an extension was provided
        if (fileNameParts.Length > 0)
        {
            foreach (FileInfo file in Files)
            {
                if (file.FileName == fileNameParts[0] && file.FileExtension == fileNameParts[1])
                {
                    return file;
                }
            }
        }
        else
        {
            foreach (FileInfo file in Files)
            {
                if (file.FileName == fileNameParts[0])
                {
                    return file;
                }
            }
        }

        return null;
    }

    public FileInfo FindFile(int offset)
    {
        foreach (FileInfo file in Files)
        {
            if (file.FileOffset == offset)
            {
                return file;
            }
        }

        return null;
    }

    // Finds all files with a specific filename
    public List<FileInfo> FindFiles(string fileName)
    {
        List<FileInfo> returnedFiles = new List<FileInfo>();
        foreach(FileInfo file in Files)
        {
            if(file.FileName == fileName)
            {
                returnedFiles.Add(file);
            }
        }

        return returnedFiles;
    }
}