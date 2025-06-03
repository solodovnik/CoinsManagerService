using CoinsManagerService.Services;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.IO;
using System;

public class OnnxService : IOnnxService, IDisposable
{
    private readonly InferenceSession _session;

    public OnnxService()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "ML", "clip-ViT-B-32-vision.onnx");
        _session = new InferenceSession(modelPath);
    }

    public IDisposableReadOnlyCollection<DisposableNamedOnnxValue> GenerateEmbeddings(DenseTensor<float> imageTensor)
    {
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("pixel_values", imageTensor)
        };

        return _session.Run(inputs);
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
