#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <cufft.h>
#include <cmath>
#include <stdio.h>

#define PI 3.14159265358979323846f

// 1. GPU 내부에서 사용할 Ram-Lak 필터 생성 및 주파수 곱셈 커널
__global__ void ApplyRamLakFilterKernel(cufftComplex* d_complexSinogram, int width, int height) {
    int x = blockIdx.x * blockDim.x + threadIdx.x; // 각도 인덱스 (0 ~ 359)
    int y = blockIdx.y * blockDim.y + threadIdx.y; // 디텍터 인덱스 (0 ~ 399)

    if (x >= width || y >= height) return;

    // 현재 픽셀의 1차원 주소 계산
    int idx = y * width + x;

    // Ram-Lak 필터의 주파수 축 중심 자르기 (DC 성분이 가운데 오도록 처리)
    // 주파수 영역 해상도 N = height (400)
    int N = height;
    float freq = (float)y - (N / 2);

    // Ram-Lak 필터 수식: |정규화된 주파수|
    float filterValue = fabsf(freq / (N / 2));

    // FFT 결과물은 주파수 대역이 Shift되어 있으므로 
    // 현재 인덱스에 맞게 필터 맵핑 (0~N/2는 양의 주파수, N/2~N은 음의 주파수)
    int freqIdx = (y < N / 2) ? (y + N / 2) : (y - N / 2);
    float actualFilter = fabsf(((float)freqIdx - (N / 2)) / (N / 2));

    // 복소수 공간에 필터 값 곱하기 (Real, Imaginary 성분 각각 곱셈)
    d_complexSinogram[idx].x *= actualFilter;
    d_complexSinogram[idx].y *= actualFilter;
}

// 2. 실실수를 복소수 배열로 변환하는 전처리 커널
__global__ void ConvertFloatToComplex(float* d_in, cufftComplex* d_out, int size) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= size) return;

    d_out[idx].x = d_in[idx]; // 실수부 입력
    d_out[idx].y = 0.0f;      // 허수부는 0으로 초기화
}

// 3. 복소수 배열에서 크기(Magnitude) 또는 실수를 추출해 반환하는 후처리 커널
__global__ void ConvertComplexToFloat(cufftComplex* d_in, float* d_out, int size, int FFT_N) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= size) return;

    // FFT -> IFFT를 거치면 데이터가 N(데이터 개수)만큼 스케일업 되므로 N으로 나누어 보정
    d_out[idx] = d_in[idx].x / (float)FFT_N;
}

extern "C" __declspec(dllexport) void ProcessSinogram(float* sinogramData, int width, int height) {
    if (sinogramData == nullptr) return;

    int totalPixels = width * height; // 360 * 400 = 144,000
    size_t floatSize = totalPixels * sizeof(float);
    size_t complexSize = totalPixels * sizeof(cufftComplex);

    // GPU 메모리 할당
    float* d_inputRaw = nullptr;
    cufftComplex* d_complexData = nullptr;

    cudaMalloc((void**)&d_inputRaw, floatSize);
    cudaMalloc((void**)&d_complexData, complexSize);

    // C# 힙 메모리 데이터를 GPU 메모리로 복사
    cudaMemcpy(d_inputRaw, sinogramData, floatSize, cudaMemcpyHostToDevice);

    // [Step 1] float 데이터를 cufftComplex 데이터형으로 변환
    int blockSize = 256;
    int gridSize = (totalPixels + blockSize - 1) / blockSize;
    ConvertFloatToComplex << <gridSize, blockSize >> > (d_inputRaw, d_complexData, totalPixels);
    cudaDeviceSynchronize();

    // [Step 2] cuFFT Batch Plan 설정
    // 레이아웃 스펙: 가로(X)=360(각도), 세로(Y)=400(디텍터)
    // 목표: 각도별로 독립적인 400포인트 1D FFT를 360번 일괄 처리해야 함.
    cufftHandle plan;
    int rank = 1;           // 1차원 FFT
    int n[] = { height };   // FFT를 수행할 길이는 400 (디텍터 개수)
    int kembed[] = { height };
    int stride = width;     // 같은 각도 데이터 간의 물리적 간격은 가로 크기(360) 만큼 떨어져 있음
    int idist = 1;          // 다음 각도 데이터 시작점까지의 거리는 바로 옆 칸(1)
    int odist = 1;

    // 고급 레이아웃 설정을 통해 90도 돌아간 배열 구조를 고속 배정밀도 배치 연산으로 바인딩
    cufftResult result = cufftPlanMany(&plan, rank, n,
        kembed, stride, idist,
        kembed, stride, odist,
        CUFFT_C2C, width);

    if (result != CUFFT_SUCCESS) {
        printf("[CUDA ERROR] cuFFT Plan 생성 실패\n");
        return;
    }

    // [Step 3] Forward FFT 실행 (공간 도메인 ➔ 주파수 도메인)
    cufftExecC2C(plan, d_complexData, d_complexData, CUFFT_FORWARD);
    cudaDeviceSynchronize();

    // [Step 4] 주파수 영역에서 Ram-Lak 필터링 커널 적용
    dim3 filterBlock(16, 16);
    dim3 filterGrid((width + filterBlock.x - 1) / filterBlock.x, (height + filterBlock.y - 1) / filterBlock.y);
    ApplyRamLakFilterKernel << <filterGrid, filterBlock >> > (d_complexData, width, height);
    cudaDeviceSynchronize();

    // [Step 5] Inverse FFT 실행 (주파수 도메인 ➔ 필터링된 공간 도메인)
    cufftExecC2C(plan, d_complexData, d_complexData, CUFFT_INVERSE);
    cudaDeviceSynchronize();

    // [Step 6] 복소수 배열 결과물을 다시 float 배열로 복원 및 데이터 스케일 보정
    ConvertComplexToFloat << <gridSize, blockSize >> > (d_complexData, d_inputRaw, totalPixels, height);
    cudaDeviceSynchronize();

    // 필터링 작업이 완료된 GPU 메모리를 다시 C# 배열 주소로 전달
    cudaMemcpy(sinogramData, d_inputRaw, floatSize, cudaMemcpyDeviceToHost);

    // 사용한 리소스 수거 (Memory Leak 방지)
    cufftDestroy(plan);
    cudaFree(d_inputRaw);
    cudaFree(d_complexData);
}