#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <stdio.h>

// C#에서 호출할 수 있도록 C 스타일의 인터페이스로 외부에 노출합니다.
extern "C" __declspec(dllexport) void ProcessSinogram(float* sinogramData, int width, int height) {
    if (sinogramData == nullptr) return;

    // 데이터가 잘 넘어왔는지 확인하기 위해 첫 번째 픽셀 값을 디버그용으로 살짝 변경해봅니다.
    // 정상적으로 연결되었다면 C# 쪽 배열의 첫 번째 값도 함께 바뀌게 됩니다.
    printf(" [CUDA DLL] C#으로부터 데이터를 받았습니다.\n");
    printf(" [CUDA DLL] 원래 첫 번째 값: %f\n", sinogramData[0]);

    // 디텍터의 정중앙(width / 2) 픽셀을 타겟으로 잡습니다.
    int targetX = width / 2;
    int targetY = height / 2;
    int centerIndex = (targetY * width) + targetX;

    // 중앙 픽셀 값에 2배 곱하기
    sinogramData[centerIndex] = sinogramData[centerIndex] * 2.0f;

    printf(" [CUDA DLL] 변경된 첫 번째 값: %f\n", sinogramData[0]);
}