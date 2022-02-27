using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ParallelTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            for (int i = 0; i < 100; i++)
            {
                Test();
            }
        }
    }

    private unsafe void Test()
    {
        const int width = 100;
        const int height = 100;
        byte[] data = new byte[width * height];

        fixed (byte* pin = data)
        {
            byte* p = pin;

            ParallelLoopResult result = Parallel.For(0, height, (y, state) =>
            {
                // Debug.Log($"i: {y.ToString()}, state: {state}");

                byte* ip = p + (width * y);

                for (int x = 0; x < width; ++x)
                {
                    *ip = (byte)x;
                    ++ip;
                }
            });
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte t = data[y * width + x];
                if (t != x)
                {
                    Debug.LogError("Something was wrong.");
                    return;
                }
            }
        }

        Debug.Log($"Check OK!!!!!!!!");
    }
}