using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[BurstCompile]
public struct ExpandType1Job : IJobParallelFor
{
    [NativeDisableParallelForRestriction]public NativeArray<byte> data;
    [ReadOnly] public NativeArray<int> indices;
    [NativeDisableParallelForRestriction] public NativeArray<Pixel32> pixels;

    public PngMetaData metaData;

    public void Execute(int index)
    {
        int y = indices[index];

        int idx = metaData.rowSize * y;
        int startIndex = idx + 1;

        if (data.Length < startIndex + (metaData.width * metaData.stride))
        {
            throw new IndexOutOfRangeException("Index out of range.");
        }

        Expand(startIndex, y);
    }

    private unsafe void Expand(int startIndex, int y)
    {
        Pixel32* pixelPtr = (Pixel32*)pixels.GetUnsafePtr();
        pixelPtr += (metaData.width * (metaData.height - 1 - y));

        byte* ptr = (byte*)data.GetUnsafePtr();
        ptr += startIndex;

        Pixel32 left = Pixel32.Zero;
        Pixel32 current = default;

        for (int x = 0; x < metaData.width; ++x)
        {
            *(uint*)&current = *(uint*)ptr;

            left = Pixel32.CalculateFloor(current, left);

            ptr += metaData.stride;

            *pixelPtr = left;
            ++pixelPtr;
        }
    }
}