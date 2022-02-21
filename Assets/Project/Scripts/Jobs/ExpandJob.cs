using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

[BurstCompile]
public class ExpandJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<byte> data;
    [ReadOnly] public NativeArray<int> indices;
    public NativeArray<Pixel32> pixels;

    public PngMetaData metaData;
    public byte bitsPerPixel;
    public int rowSize;
    public int stride;

    public void Execute(int index)
    {
        int y = indices[index];

        int idx = rowSize * y;
        int startIndex = idx + 1;

        if (data.Length < startIndex + (metaData.width * stride))
        {
            throw new IndexOutOfRangeException("Index out of range.");
        }

        byte filterType = data[idx];

        switch (filterType)
        {
            case 0:
                break;

            case 1:
                // UnsafeExpand1(data, startIndex, stride, h, pixels, metaData);
                break;

            case 2:
                ExpandType2(startIndex, y);
                break;

            case 3:
                ExpandType3(startIndex, y);
                break;

            case 4:
                ExpandType4(startIndex, y);
                break;
        }
    }

    private unsafe void ExpandType2(int startIndex, int y)
    {
        Pixel32* pixelPtr = (Pixel32*)pixels.GetUnsafePtr();
        pixelPtr += (metaData.width * (metaData.height - 1 - y));

        byte* ptr = (byte*)data.GetUnsafePtr();
        ptr += startIndex;

        Pixel32 up = Pixel32.Zero;
        Pixel32 current = default;

        int upStride = metaData.width;

        for (int x = 0; x < metaData.width; ++x)
        {
            *(uint*)&current = *(uint*)ptr;

            if (y == 0)
            {
                up = Pixel32.Zero;
            }
            else
            {
                *(uint*)&up = *(uint*)(pixelPtr + upStride);
            }

            up = Pixel32.CalculateFloor(current, up);

            ptr += stride;

            *pixelPtr = up;
            ++pixelPtr;
        }
    }

    private unsafe void ExpandType3(int startIndex, int y)
    {
        Pixel32* pixelPtr = (Pixel32*)pixels.GetUnsafePtr();
        pixelPtr += (metaData.width * (metaData.height - 1 - y));

        byte* ptr = (byte*)data.GetUnsafePtr();
        ptr += startIndex;

        Pixel32 up = Pixel32.Zero;
        Pixel32 left = Pixel32.Zero;
        Pixel32 current = default;

        int upStride = metaData.width;

        for (int x = 0; x < metaData.width; ++x)
        {
            *(uint*)&current = *(uint*)ptr;

            if (y == 0)
            {
                up = Pixel32.Zero;
            }
            else
            {
                *(uint*)&up = *(uint*)(pixelPtr + upStride);
            }

            left = Pixel32.CalculateAverage(current, left, up);

            ptr += stride;

            *pixelPtr = left;
            ++pixelPtr;
        }
    }

    private unsafe void ExpandType4(int startIndex, int y)
    {
        Pixel32* pixelPtr = (Pixel32*)pixels.GetUnsafePtr();
        pixelPtr += (metaData.width * (metaData.height - 1 - y));

        byte* ptr = (byte*)data.GetUnsafePtr();
        ptr += startIndex;

        Pixel32 up = Pixel32.Zero;
        Pixel32 left = Pixel32.Zero;
        Pixel32 leftUp = Pixel32.Zero;
        Pixel32 current = default;

        int upStride = metaData.width;

        for (int x = 0; x < metaData.width; ++x)
        {
            *(uint*)&current = *(uint*)ptr;

            if (y == 0)
            {
                up = Pixel32.Zero;
            }
            else
            {
                *(uint*)&up = *(uint*)(pixelPtr + upStride);
            }

            if (y == 0 || x == 0)
            {
                leftUp = Pixel32.Zero;
            }
            else
            {
                *(uint*)&leftUp = *(uint*)(pixelPtr + upStride - 1);
            }

            left = Pixel32.CalculatePaeth(left, up, leftUp, current);

            ptr += stride;

            *pixelPtr = left;
            ++pixelPtr;
        }
    }
}