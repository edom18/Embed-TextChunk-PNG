using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class NativeArrayTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Test();
        }
    }

    private unsafe void Test()
    {
        NativeArray<Pixel32> array = new NativeArray<Pixel32>(100, Allocator.Temp);

        Pixel32* ptr = (Pixel32*)array.GetUnsafePtr();
        Pixel32* start = ptr;
        for (int i = 0; i < array.Length; ++i)
        {
            *(uint*)ptr = (uint)1234;
            ++ptr;
        }

        NativeArray<byte> byteArray = new NativeArray<byte>(array.Length * 4, Allocator.Temp);
        byte* bytePtr = (byte*)byteArray.GetUnsafePtr();

        for (int i = 0; i < array.Length; ++i)
        {
            *(uint*)bytePtr = *(uint*)start;
        
            bytePtr += 4;
            ++start;
        }

        array.Dispose();
        byteArray.Dispose();
    }
}