using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CudaCtRecon
{
    public partial class MainWindow : Window
    {
        // CUDA DLL Import
        [DllImport("CppCudaEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessSinogram(float[] sinogramData, int width, int height);

        // DLL 호출 선언 추가 (기존 ProcessSinogram 아래에 붙이세요)
        [DllImport("CppCudaEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BackProjection(float[] sinogramData, float[] volumeData, int volSize, int angles, int detectorWidth);

        public MainWindow()
        {
            InitializeComponent();
            RunCtPipelineTest(); // 시작하자마자 파이프라인 가동
        }

        private void RunCtPipelineTest()
        {
            int width = 360;       // 투영 각도 수
            int height = 400;      // 디텍터 픽셀 수
            string fileName = "Sinogram_Data_256x360.raw";

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = System.IO.Path.Combine(exeDirectory, fileName);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"데이터 파일을 찾을 수 없습니다.\n'{fullPath}'에 파일이 있는지 확인하세요.");
                return;
            }

            try
            {
                // [Step 1] 데이터 로드 및 메모리 마샬링
                byte[] rawBytes = File.ReadAllBytes(fullPath);
                float[] sinogramData = new float[rawBytes.Length / 4];

                // ★ 반드시 카피를 먼저 해야 합니다!
                Buffer.BlockCopy(rawBytes, 0, sinogramData, 0, rawBytes.Length);

                // [Step 2] 원본 시노그램 시각화 (데이터가 채워진 후 렌더링)
                DrawFloatArrayToImage(sinogramData, width, height, Image_Original, "Original");

                // [Step 3] 램프 필터 모양 시각화 (가운데 이미지)
                DrawRampFilterImage(height, Image_Filter);

                // [Step 4] CUDA DLL 주파수 필터링 통신!
                ProcessSinogram(sinogramData, width, height);

                // [Step 5] 필터링이 완료된 결과 시각화
                DrawFloatArrayToImage(sinogramData, width, height, Image_Filtered, "Filtered");

                // --- [Step 6] 3D Back-Projection 추가 ---
                int volSize = 256; // 우리가 만들 볼륨 크기
                float[] volumeData = new float[volSize * volSize * volSize];

                // 이제 CUDA 엔진에게 필터링된 데이터(sinogramData)를 던져서 3D(volumeData)로 재구성합니다.
                BackProjection(sinogramData, volumeData, volSize, height, width);

                // [Step 7] 결과 시각화 (Z축 중간 단면인 128번 슬라이스를 추출하여 시각화)
                VisualizeSlice(volumeData, volSize, volSize / 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"에러 발생: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // 렌더링 유틸리티 함수 모음
        // -------------------------------------------------------------------------

        // 1. float 배열을 0~255 Grayscale 2D 이미지로 변환하는 함수
        private void DrawFloatArrayToImage(float[] data, int width, int height, Image targetControl, string type)
        {
            byte[] pixels = new byte[width * height];

            // [핵심] 데이터의 통계치를 기반으로 동적 정규화
            float min = data.Min();
            float max = data.Max();
            float range = max - min;
            if (range <= 0.0001f) range = 1.0f; // 0 나누기 방지

            for (int i = 0; i < data.Length; i++)
            {
                float val = data[i];

                // 필터링된 데이터만 절대값 처리 (다른 데이터는 그대로)
                if (type == "Filtered") val = Math.Abs(val);

                // 동적 정규화
                float normalized = (val - min) / range;

                // 0~255 스케일링
                int bVal = (int)(normalized * 255.0f);
                pixels[i] = (byte)Math.Max(0, Math.Min(255, bVal));
            }

            WriteableBitmap wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
            targetControl.Source = wb;
        }

        // 2. 수학적 Ram-Lak 필터(1D)를 2D 이미지로 시각화하는 함수
        private void DrawRampFilterImage(int size, Image targetControl)
        {
            int N = size;
            byte[] pixels = new byte[N];

            for (int y = 0; y < N; y++)
            {
                int freqIdx = (y < N / 2) ? (y + N / 2) : (y - N / 2);
                float filterValue = Math.Abs(((float)freqIdx - (N / 2)) / (N / 2));
                pixels[y] = (byte)(filterValue * 255.0f);
            }

            // 가로 사이즈 N, 세로 사이즈 1인 비트맵 생성
            WriteableBitmap wb = new WriteableBitmap(N, 1, 96, 96, PixelFormats.Gray8, null);
            wb.WritePixels(new Int32Rect(0, 0, N, 1), pixels, N, 0);
            targetControl.Source = wb;
        }

        private void VisualizeSlice(float[] volumeData, int volSize, int sliceZ)
        {
            // 256x256 슬라이스 추출
            float[] slice = new float[volSize * volSize];
            int offset = sliceZ * (volSize * volSize);
            Array.Copy(volumeData, offset, slice, 0, slice.Length);

            // Image_Reconstruction이라는 이름의 Image 컨트롤이 XAML에 있다고 가정합니다.
            DrawFloatArrayToImage(slice, volSize, volSize, Image_Reconstruction, "Reconstruction");
        }
    }
}