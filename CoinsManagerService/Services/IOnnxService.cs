using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace CoinsManagerService.Services
{
    public interface IOnnxService
    {
        public IDisposableReadOnlyCollection<DisposableNamedOnnxValue> GenerateEmbeddings(DenseTensor<float> imageTensor);
    }
}
