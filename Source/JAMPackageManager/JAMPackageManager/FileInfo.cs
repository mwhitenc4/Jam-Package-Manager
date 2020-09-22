using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class FileInfo
{
    // This header is at the top of every file
    // The first 4 bytes, represent two short values
    // The values are identical and indicate file size
    static readonly byte[] DEFAULT_HEADER = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x42, 0x00, 0x00, 0x00, 0x53, 0x00, 0x00, 0x00, 0x8B, 0x00, 0x00, 0x00, 0xA2, 0x00, 0x00, 0x00 };

    // The Jam file that contains this file
    JAM Container { get; set; }

    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public int FileSize { get; set; }
    public int MetaOffset { get; set; }
    public int FileOffset { get; set; }

    public FileInfo(JAM container)
    {
        Container = container;
    }

    public void UpdateHeader(int newFileSize)
    {
        byte[] header = new byte[DEFAULT_HEADER.Length];

        byte[] fileSizeBytes = BitConverter.GetBytes(newFileSize);

        // For some reason, the file size is in the header twice
        for(int i = 0; i < 8; i++)
        {
            header[i] = fileSizeBytes[i % 4];
        }

        // Copy over bytes that are the same for all headers
        for(int i = 8; i < DEFAULT_HEADER.Length; i++)
        {
            header[i] = DEFAULT_HEADER[i];
        }

        // Overwrite old header (Only 8 bytes are unique)
        for(int i = 0; i < 8; i++)
        {
            Container.Data[FileOffset + i] = header[i];
        }
    }

    public void UpdateMeta(int newOffset)
    {
        byte[] newOffsetBytes = BitConverter.GetBytes(newOffset);

        // Overwrite old meta offset
        for(int i = 0; i < 4; i++)
        {
            Container.Data[(MetaOffset + 4) + i] = newOffsetBytes[i];
        }
    }

    public void LoadFileName(short fileID)
    {
        FileName = RemoveZeroes(Container.FileNames[fileID]);
    }

    public void LoadExtensionName(short extensionID)
    {
        FileExtension = RemoveZeroes(Container.ExtensionNames[extensionID]);
    }

    public void LoadFileOffset(int offset)
    {
        FileOffset = offset;
    }

    public void LoadMetaOffset(int offset)
    {
        MetaOffset = offset;
    }

    public void LoadFileSize()
    {
        byte[] fileSizeBytes = new byte[4];

        for(int i = 0; i < 4; i++)
        {
            fileSizeBytes[i] = Container.Data[FileOffset + i];
        }

        FileSize = BitConverter.ToInt32(fileSizeBytes, 0);
    }

    // Removes "0" bytes from a char array and returns a string
    static string RemoveZeroes(char[] input)
    {
        string output = "";

        foreach (char val in input)
        {
            if (val != 0)
            {
                output += val;
            }
        }

        return output;
    }

    public void Extract(string path)
    {
        byte[] fileData = new byte[FileSize];

        for(int i = 0; i < FileSize; i++)
        {
            fileData[i] = Container.Data[(FileOffset + 0x20) + i];
        }

        // Create file in output directory
        File.WriteAllBytes(path, fileData);
    }

    public void Resize(int newFileSize)
    {
        UpdateHeader(newFileSize);

        int sizeDifference = newFileSize - FileSize;

        FileSize = newFileSize;

        // We need to update meta data for files
        // that have an offset beyond this file
        // because resizing this file will change their offset
        foreach(FileInfo file in Container.Files)
        {
            if(file.FileOffset > FileOffset)
            {
                file.FileOffset += sizeDifference;
                file.UpdateMeta(file.FileOffset);
            }
        }

        // Create a new array to hold updated data
        byte[] newContainerData = new byte[Container.Data.Length + sizeDifference];

        // The offset for the end of the file
        // note: Headers have a size of 0x20 (32)
        int newEndOfFile = FileOffset + FileSize + 0x20;

        int oldEndOfFile = newEndOfFile - sizeDifference;

        // Stores the current location in the new container data
        int location = 0;
        // Copy all bytes up until the end of the file
        for (int i = 0;i<newEndOfFile; i++)
        {
            // Check if the file had data at the current offset
            if (i < oldEndOfFile)
            {
                newContainerData[i] = Container.Data[i];
            }
            else
            {
                newContainerData[i] = 0;
            }
            location++;
        }

        // Restore the rest of the file after the new data
        for(int i = oldEndOfFile; i < Container.Data.Length; i++)
        {
            newContainerData[location] = Container.Data[i];
            location++;
        }

        // Update the container data
        Container.Data = newContainerData;
    }

    public void Overwrite(byte[] newData)
    {
        Resize(newData.Length);

        for(int i = 0; i < newData.Length; i++)
        {
            Container.Data[(FileOffset + 0x20) + i] = newData[i];
        }
    }

    // This will replace the bytes in this file, with the bytes in an actual file
    // The output directory is used for convenience, you can extract, edit and repack without moving files
    public bool ReplaceWithFile(string fileName)
    {
        if (File.Exists(fileName)){
            byte[] newBytes = File.ReadAllBytes(fileName);

            Overwrite(newBytes); // Overwrite the old file
            return true;
        }
        return false;
    }
}
