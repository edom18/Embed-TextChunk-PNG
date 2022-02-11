using System;
using System.Collections.Generic;
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

public struct PngMetaData
{
    public int width;
    public int height;
    public byte bitDepth;
    public byte colorType;
    public byte compressionMethod;
    public byte filterMethod;
    public byte interlace;
}

public static class PngParser
{
    public static readonly int PngSignatureSize = 8;
    public static readonly int PngHeaderSize = 33;

    private static readonly Encoding _latin1 = Encoding.GetEncoding(28591);
    private const string _signature = "\x89PNG\r\n\x1a\n";

    public static Texture2D Parse(byte[] data)
    {
        if (!IsPng(data))
        {
            throw new InvalidDataException($"[{nameof(PngTextChunkTest)}] Provided data is not PNG format.");
        }

        Chunk ihdr = GetHeaderChunk(data);

        PngMetaData metaData = GetMetaData(ihdr);

        Debug.Log($"[{nameof(PngParser)}] A parsed png size is {metaData.width.ToString()} x {metaData.height.ToString()}");

        const int metaDataSize = 4 + 4 + 4;

        int index = PngSignatureSize + ihdr.length + metaDataSize;

        while (true)
        {
            if (data.Length < index) break;

            Chunk chunk = ParseChunk(data, index);

            Debug.Log($"[{nameof(PngParser)}] Chunk type : {chunk.chunkType}, length: {chunk.length.ToString()}");

            if (chunk.chunkType == "IEND") break;

            index += chunk.length + metaDataSize;
        }

        return null;
    }

    public static Chunk GetHeaderChunk(byte[] data)
    {
        return ParseChunk(data, PngSignatureSize);
    }

    public static PngMetaData GetMetaData(Chunk headerChunk)
    {
        if (headerChunk.chunkType != "IHDR")
        {
            Debug.LogError($"[{nameof(PngParser)}] The chunk is not a header chunk.");
            return default;
        }

        byte[] wdata = new byte[4];
        byte[] hdata = new byte[4];
        Array.Copy(headerChunk.chunkData, 0, wdata, 0, 4);
        Array.Copy(headerChunk.chunkData, 4, hdata, 0, 4);
        Array.Reverse(wdata);
        Array.Reverse(hdata);
        uint w = BitConverter.ToUInt32(wdata, 0);
        uint h = BitConverter.ToUInt32(hdata, 0);

        return new PngMetaData
        {
            width = (int)w,
            height = (int)h,
            bitDepth = headerChunk.chunkData[8],
            colorType = headerChunk.chunkData[9],
            compressionMethod = headerChunk.chunkData[10],
            filterMethod = headerChunk.chunkData[11],
            interlace = headerChunk.chunkData[12],
        };
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
        return (Encoding.ASCII.GetString(chunkTypeData), chunkTypeData);
    }

    private static byte[] GetChunkData(byte[] data, int size, int index)
    {
        byte[] chunkData = new byte[size];
        Array.Copy(data, index, chunkData, 0, chunkData.Length);
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
        return signature == _signature;
    }

    private static bool CrcCheck(uint crc, byte[] chunkTypeData, byte[] chunkData)
    {
        uint c = Crc32.Hash(0, chunkTypeData);
        c = Crc32.Hash(c, chunkData);
        return crc == c;
    }
}