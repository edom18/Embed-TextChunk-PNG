using System;
using System.IO;
using System.Text;
using UnityEngine;

public struct Chunk
{
    public int length;
    public string chunkType;
    public byte[] chunkData;
    public uint crc;
}

public static class PngParser
{
    private static Encoding _latin1 = Encoding.GetEncoding(28591);

    public static Texture2D Parse(byte[] data)
    {
        return null;
    }

    public static Chunk ParseChunk(byte[] data, int startIndex)
    {
        int size = GetLength(data, startIndex);
        (string chunkType, byte[] chunkTypeData) = GetChunkType(data, startIndex + 4);
        byte[] chunkData = GetChunkData(data, size, startIndex + 4 + 4);
        uint crc = GetCrc(data, startIndex + 4 + 4 + size);

        if (!CrcCheck(crc, chunkTypeData, chunkData))
        {
            throw new InvalidDataException($"[{nameof(PngTextChunkTest)}] Failed checking CRC. Something was wrong.");
        }

        return new Chunk
        {
            length = size,
            chunkType = chunkType,
            chunkData = chunkData,
            crc = crc,
        };
    }

    private static int GetLength(byte[] data, int index)
    {
        byte[] lengthData = new byte[4];
        Array.Copy(data, index, lengthData, 0, 4);
        Array.Reverse(lengthData);
        return BitConverter.ToInt32(lengthData, 0);
    }

    private static (string, byte[]) GetChunkType(byte[] data, int index)
    {
        byte[] chunkTypeData = new byte[4];
        Array.Copy(data, index, chunkTypeData, 0, 4);
        Array.Reverse(chunkTypeData);
        return (Encoding.ASCII.GetString(chunkTypeData), chunkTypeData);
    }

    private static byte[] GetChunkData(byte[] data, int size, int index)
    {
        byte[] chunkData = new byte[size];
        Array.Copy(data, index, chunkData, 0, chunkData.Length);
        Array.Reverse(chunkData);
        return chunkData;
    }

    private static uint GetCrc(byte[] data, int index)
    {
        byte[] crcData = new byte[4];
        Array.Copy(data, index, crcData, 0, 4);
        Array.Reverse(crcData);
        return BitConverter.ToUInt32(crcData, 0);
    }

    public static bool IsPng(byte[] data)
    {
        string signature = _latin1.GetString(data, 0, 8);
        return signature == "\x89PNG\r\n\x1a\n";
    }

    private static bool CrcCheck(uint crc, byte[] chunkTypeData, byte[] chunkData)
    {
        uint c = Crc32.Hash(0, chunkTypeData);
        c = Crc32.Hash(c, chunkData);
        return crc == c;
    }
}