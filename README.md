# 🚀 CudaCtRecon: C#-CUDA 기반 3D CT Reconstruction 엔진

> "자기계발과 CUDA 고속 병렬처리 학습이라는 훌륭한 명분으로 새 컴퓨터를 지른 기념 프로젝트"

## 💡 프로젝트 개요
오픈된 2D X-ray 투영 이미지(Sinogram) 데이터를 활용하여, C#과 CUDA 연동을 통해 3D 볼륨(Voxel) 데이터로 초고속 재구성(Reconstruction)하는 역투영 엔진 프로토타입입니다. 

머신비전 현업에서 요구되는 **대용량 데이터의 이종 언어 간(C# ↔ C++) 무손실 마샬링 통신**과 **GPU를 활용한 수학적/기하학적 역문제(Inverse Problem) 해결 능력**을 증명하기 위해 1달 단기 완성으로 기획되었습니다.

## 🛠 주요 기술 스택
* **UI & Control:** C# (.NET 10.0), WPF
* **Core Engine:** C++, CUDA Toolkit (v13.2)
* **Library:** cuFFT (주파수 도메인 필터링)

## 🎯 최종 목표
1. **Raw 데이터 처리:** 360 각도로 촬영된 2D 투영 데이터(Sinogram_Data_360x400.raw) I/O 제어
2. **GPU 파이프라인:** C#에서 읽어들인 대용량 1차원 평탄화 배열을 CUDA 메모리로 병목 없이 전송
3. **FDK 알고리즘 구현:** * **Filtering:** cuFFT를 활용한 주파수 도메인 Ram-Lak 필터 적용
   * **Back-Projection:** 1 Thread = 1 Voxel 매핑 전략으로 2D 센서 좌표 역산 및 누적
4. **시각화 및 최적화:** C#에서 3D 볼륨 단면 실시간 확인 및 CPU vs GPU 처리 속도 벤치마크 

---

## 📅 4주 완성 마일스톤 (약 40~50시간 투자)

### 🌊 [Week 1] 데이터 파이프라인 및 워밍업 (I/O & Memory)
- [x] 오픈소스 CT 데이터셋(Shepp-Logan Phantom 시노그램) 확보 및 스펙 분석
- [x] C# 환경에서 대용량 Binary Raw 파일 Load 및 메모리 평탄화(Flatten) 구현
- [x] `DllImport`를 통한 C# ↔ C++(CUDA) 간 대용량 `float` 배열 P/Invoke 마샬링 환경 개통
- [x] 인덱스(Endianness 및 해상도) 디버깅 및 무손실 데이터 통신 검증 완료

### 🌪 [Week 2] 전처리 필터링 커널 (Filtering)
- [ ] CT 영상 복원을 위한 Ram-Lak 필터 수학적 모델링
- [ ] NVIDIA `cuFFT` 라이브러리 연동 및 GPU 메모리 레이아웃 최적화
- [ ] 1D FFT ➔ Filter Convolution ➔ IFFT 파이프라인 커널 작성
- [ ] 메모리 누수(`cudaFree`) 프로파일링 및 C# UI단 Sinogram 시각화 검증

### 💎 [Week 3] 코어 엔진 구현 (Back-Projection)
- [ ] 3D Voxel (x,y,z) ➔ 2D Sensor (u,v) 기하학적 역산(Geometry) 수식 모델링
- [ ] 1 스레드 = 1 Voxel 매핑을 위한 Grid/Block 차원 동적 할당
- [ ] Bilinear Interpolation(쌍선형 보간법)을 적용한 누적 연산 커널 최적화
- [ ] 렌더링 완료된 3D Volume 배열 C# 메모리 회수 및 Z축 중간 단면(Center Slice) 추출 검증

### 🚀 [Week 4] 최적화 및 UI/포트폴리오 마무리
- [ ] 2D 투영 데이터 고속 캐싱을 위한 CUDA Texture Memory 적용 및 벤치마크
- [ ] Z축 스크롤링이 가능한 C# 실시간 단면 뷰어(Slider UI) 제작
- [ ] CPU vs GPU 연산 처리 시간 벤치마크 프로파일링 비교표 도출
- [ ] 전체 코드 리팩토링 및 3D Volume Raw/mhd Export 기능 구현

---
*Last Updated: 2026-05-18*
