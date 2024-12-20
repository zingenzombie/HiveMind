using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileManagement
{
    static string objectDirectory = "objectDirectory/";

    public static void DownloadFile(string fileName, TileStream tileStream)
    {


        tileStream.SendBytesToStream(new byte[1] { 1 });
        tileStream.SendStringToStream(fileName);

        //Server doesn't have file
        if (tileStream.GetBytesFromStream()[0] != 1)
            return;

        byte[] data = tileStream.GetBytesFromStream();

        FileStream fs = File.Create(objectDirectory + fileName);

        fs.Write(data);
        fs.Close();
    }
}
