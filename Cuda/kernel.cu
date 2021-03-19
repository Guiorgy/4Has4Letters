#include "device_launch_parameters.h"
#include "cuda_runtime.h"

#include <algorithm>
#include <stdio.h>
#include <cstdlib>

#define export _declspec(dllexport)

struct sec {
    unsigned __int64 start = 0;
    int steps = 0;
};

cudaError_t findBetweenCuda(const int* under, const int* thousands, unsigned __int64 start, unsigned __int64 end, sec* s);

__global__ void findKernel(const int* under, const int* thousands, int* steps, unsigned __int64 start, unsigned __int64 size)
{
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i < size) {
        steps[i] = 1;
        __int64 number = start + i;
        while (number != 4) {
            if (number < 1000)
            {
                number = under[number];
                steps[i]++;
            }
            else
            {
                int separated[10];
                int j = 0;
                while (number >= 1000)
                {
                    separated[j++] = (int)(number % 1000);
                    number /= 1000;
                }
                separated[j] = (int)(number % 1000);
                int nonZero = 0;
                while (separated[nonZero] == 0) ++nonZero;
                number = 0;
                int l = j;
                if (separated[l] != 1)
                    number += under[separated[l]] + 1;
                if (nonZero == j) {
                    number += thousands[--l];
                    continue;
                }
                else
                    number += thousands[--l] - 1;
                while (l > nonZero)
                {
                    if (separated[l] == 0)
                    {
                        --l;
                        continue;
                    }
                    number += 2;
                    if (separated[l] != 1)
                        number += under[separated[l]] + 1;
                    number += thousands[--l] - 1;
                }
                number += 2;
                if (nonZero == 0)
                    number += under[separated[nonZero]];
                else
                {
                    if (separated[nonZero] != 1)
                        number += under[separated[nonZero]] + 1;
                    number += thousands[nonZero - 1];
                }
                steps[i]++;
            }
        }
    }
}

int _blocks;
const int underSize = 1000, thousandsSize = 11;
int _under[underSize], _thousands[thousandsSize];

extern "C" {
    export void prepare(int under[underSize], int thousands[thousandsSize], unsigned int blocks) {
        //printf("preparing\r\n");
        for (int i = 0; i < underSize; ++i)
            _under[i] = under[i];
        for (int i = 0; i < thousandsSize; ++i)
            _thousands[i] = thousands[i];
        _blocks = blocks;
    }

    export sec findBetween(unsigned __int64 start, unsigned __int64 end) {
        sec s;
        cudaError_t cudaStatus = findBetweenCuda(_under, _thousands, start, end, &s);
        if (cudaStatus != cudaSuccess)
            fprintf(stderr, "findBetweenCuda failed!\r\n");
        return s;
    }

    export void reset() {
        // cudaDeviceReset must be called before exiting in order for profilingand
        // tracing tools such as Nsight and Visual Profiler to show complete traces.
        cudaError_t cudaStatus = cudaDeviceReset();
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaDeviceReset failed!\r\n");
        }
    }
}

// Helper function for using CUDA.
cudaError_t findBetweenCuda(const int* under, const int* thousands, unsigned __int64 start, unsigned __int64 end, sec* s)
{
    //printf("starting\r\n");
    const unsigned __int64 maxSize = _blocks * 1000;
    unsigned __int64 size = std::min(end - start, maxSize);
    int* steps = (int*)malloc(size * sizeof(int));

    int* dev_under = 0;
    int* dev_thousands = 0;
    int* dev_steps = 0;
    cudaError_t cudaStatus;

    cudaStatus = cudaSetDevice(0);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaSetDevice failed!  Do you have a CUDA-capable GPU installed?\r\n");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_under, underSize * sizeof(int));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed! (1)\r\n");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_thousands, thousandsSize * sizeof(int));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed! (2)\r\n");
        goto Error;
    }

    cudaStatus = cudaMemcpy(dev_under, under, underSize * sizeof(int), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed! (3)\r\n");
        goto Error;
    }

    cudaStatus = cudaMemcpy(dev_thousands, thousands, thousandsSize * sizeof(int), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed! (4)\r\n");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_steps, size * sizeof(int));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed! (5)\r\n");
        goto Error;
    }

    while (start < end) {
        //printf("searching\r\n");
        unsigned __int64 b = size == maxSize ? _blocks : size / 1000 + 1;
        // Launch a kernel on the GPU with one thread for each element.
        // Ignore the E0029 "expected an expression" error
        findKernel << <b, 1000 >> > (dev_under, dev_thousands, dev_steps, start, size);
        //printf("finishing\r\n");

        // Check for any errors launching the kernel
        cudaStatus = cudaGetLastError();
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "findKernel launch failed: %s\r\n", cudaGetErrorString(cudaStatus));
            goto Error;
        }

        // cudaDeviceSynchronize waits for the kernel to finish, and returns
        // any errors encountered during the launch.
        cudaStatus = cudaDeviceSynchronize();
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaDeviceSynchronize returned error code %d after launching findKernel!\r\n", cudaStatus);
            goto Error;
        }

        // Copy output vector from GPU buffer to host memory.
        cudaStatus = cudaMemcpy(steps, dev_steps, size * sizeof(int), cudaMemcpyDeviceToHost);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed! (6)\r\n");
            goto Error;
        }

        /*for (int i = 0; i < size; ++i)
            printf("%d: %d\r\n", i, steps[i]);
        printf("\r\n\r\n");*/

        for (int i = 0; i < size; ++i)
            if (steps[i] > s->steps) {
                s->steps = steps[i];
                s->start = start + i;
            }

        start += size;
        size = std::min(end - start, maxSize);
    }

Error:
    cudaFree(dev_under);
    cudaFree(dev_thousands);
    cudaFree(dev_steps);
    free(steps);

    /*fprintf(stderr, cudaGetErrorString(cudaStatus));
    printf("\r\n");*/

    return cudaStatus;
}