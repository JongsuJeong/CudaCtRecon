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

            // 데이터 특성에 따른 디스플레이 레인지 고정
            float fixedMin = 0.0f;
            float fixedMax = 1.0f;

            if (type == "Original")
            {
                fixedMin = 0.0f;
                fixedMax = 200.0f; // 원본은 0~200 사이 분포
            }
            else if (type == "Filtered")
            {
                fixedMin = 0.0f;
                fixedMax = 2.0f;   // 엣지만 남은 필터 데이터는 값이 작음
            }

            float range = fixedMax - fixedMin;
            if (range == 0) range = 1.0f;

            for (int i = 0; i < data.Length; i++)
            {
                float val = data[i];
                if (type == "Filtered") val = Math.Abs(val); // 필터 결과는 절대값 처리

                float normalized = (val - fixedMin) / range;
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
    }
}