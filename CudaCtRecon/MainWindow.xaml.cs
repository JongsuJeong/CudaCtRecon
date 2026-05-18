using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CudaCtRecon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // 1단계에서 만든 C++ DLL의 함수를 가져옵니다.
        [DllImport("CppCudaEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessSinogram(float[] sinogramData, int width, int height);

        public MainWindow()
        {
            InitializeComponent();

            // 프로그램이 켜지자마자 테스트를 실행하도록 생성자에서 호출합니다.
            RunCtPipelineTest();
        }

        private void RunCtPipelineTest()
        {
            // 코랩에서 생성한 데이터 스펙
            int width = 360;       // 디텍터 픽셀 수
            int height = 400;      // 투영 각도 수
            string fileName = "Sinogram_Data_256x360.raw";


            // 1. 프로그램이 실행 중인 진짜 절대 경로를 구합니다.
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = System.IO.Path.Combine(exeDirectory, fileName);


            // 실행 폴더 경로에 파일이 있는지 확인
            if (!File.Exists(fileName))
            {
                MessageBox.Show($"데이터 파일을 찾을 수 없습니다.\n빌드 폴더에'{fileName}' 파일을 넣어주세요.");
                return;
            }

            try
            {
                // 1. RAW 바이너리 파일 통째로 읽기
                byte[] rawBytes = File.ReadAllBytes(fileName);

                // 2. byte 배열을 float[] 배열로 초고속 고속 복사 (마샬링)
                float[] sinogramData = new float[rawBytes.Length / 4]; // float은 4바이트이므로
                Buffer.BlockCopy(rawBytes, 0, sinogramData, 0, rawBytes.Length);

                // 3. 호출 전 C#에서 첫 번째 값 확인
                //float originalValue = sinogramData[0];

                int targetX = width / 2;   // 180 (각도 중앙)
                int targetY = height / 2;  // 200 (디텍터 중앙)
                int centerIndex = (targetY * width) + targetX; // (200 * 360) + 180 = 72180 번 인덱스!

                float originalValue = sinogramData[centerIndex];

                // 4. C++ CUDA DLL 함수 호출! (어제 세팅한 OS 국경선을 넘어갑니다)
                ProcessSinogram(sinogramData, width, height);

                // 5. 호출 후 값이 정말로 C++에서 바꾼 대로 변경되었는지 확인
                float modifiedValue = sinogramData[centerIndex];

                MessageBox.Show($"[통신 성공!]\n\n" +
                                $"C#에서 보낸 처음 값: {originalValue}\n" +
                                $"CUDA DLL 통과 후 값: {modifiedValue}\n\n" +
                                $"정확히 2배가 되었다면 대용량 메모리가 1도 깨지지 않고 완전히 연동된 것입니다!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"에러 발생: {ex.Message}");
            }
        }
    }
}