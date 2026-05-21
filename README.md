# 🚀 CudaCtRecon: C#-CUDA 기반 3D CT Reconstruction 엔진

> "자기계발과 CUDA 고속 병렬처리 학습이라는 훌륭한 명분으로 새 컴퓨터를 지른 기념 프로젝트"

## 💡 프로젝트 개요
오픈된 2D X-ray 투영 이미지(Sinogram) 데이터를 활용하여, C#과 CUDA 연동을 통해 3D 볼륨(Voxel) 데이터로 초고속 재구성(Reconstruction)하는 역투영 엔진 프로토타입입니다. 

머신비전 현업에서 요구되는 **대용량 데이터의 이종 언어 간(C# ↔ C++) 무손실 마샬링 통신**과 **GPU를 활용한 수학적/기하학적 역문제(Inverse Problem) 해결 능력**을 증명하기 위해 단기 완성으로 기획되었습니다.

## 🛠 주요 기술 스택
* **UI & Control:** C# (.NET 10.0), WPF
* **Core Engine:** C++, CUDA Toolkit (v13.2)
* **Library:** cuFFT (주파수 도메인 필터링)

---

## 📅 마일스톤 및 진행 상황 (초고속 Day 단위 진행)

### 🌊 [Day 1] 데이터 파이프라인 및 워밍업 (I/O & Memory) - `완료`
- [x] 오픈소스 CT 데이터셋(Shepp-Logan Phantom 시노그램) 확보 및 스펙 분석
- [x] C# 환경에서 대용량 Binary Raw 파일 Load 및 메모리 평탄화(Flatten) 구현
- [x] `DllImport`를 통한 C# ↔ C++(CUDA) 간 대용량 `float` 배열 P/Invoke 마샬링 환경 개통
- [x] 인덱스(Endianness 및 360x400 해상도) 디버깅 및 무손실 데이터 통신 검증 완료

### 🌪 [Day 2] 전처리 필터링 커널 (Filtering) - `완료`
- [x] CT 영상 복원 $1/r$ Blurring 보정을 위한 Ram-Lak(Ramp) 필터 수학적 모델링
- [x] NVIDIA `cuFFT` 라이브러리 연동 및 360개 각도 동시 처리 Batch Plan 최적화
- [x] 1D FFT ➔ Filter Convolution ➔ IFFT 파이프라인 커널 작성
- [x] 메모리 누수 방지(`cudaFree`, `cufftDestroy`) 및 C# WPF 3단 필터링 시각화 UI 구축

### 💎 [Day 3] 코어 엔진 구현 (Back-Projection) - `완료`
- [x] 3D Voxel (x,y,z) ➔ 2D Sensor (u,v) 기하학적 역산(Geometry) 수식 모델링
- [x] 1 스레드 = 1 Voxel 매핑을 위한 Grid/Block 차원 동적 할당
- [x] Bilinear Interpolation(쌍선형 보간법)을 적용한 누적 연산 커널 최적화
- [x] 렌더링 완료된 3D Volume 배열 C# 메모리 회수 및 Z축 중간 단면(Center Slice) 추출 검증

### 🚀 [Day 4] 최적화 및 UI/포트폴리오 마무리 - `마무리`
- [ ] 2D 투영 데이터 고속 캐싱을 위한 CUDA Texture Memory 적용 및 벤치마크
- [X] Z축 스크롤링이 가능한 C# 실시간 단면 뷰어(Slider UI) 제작
- [ ] CPU vs GPU 연산 처리 시간 벤치마크 프로파일링 비교표 도출
- [X] 전체 코드 리팩토링 및 3D Volume Raw/mhd Export 기능 구현

---
*Last Updated: 2026-05-19*
