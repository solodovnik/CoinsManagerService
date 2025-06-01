using CoinsManagerService.Dtos;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using CoinsManagerService.Data;
using System.Text.Json;
using AutoMapper;
using CoinsManagerService.Models;

namespace CoinsManagerService.Services
{
    public class CoinSearchService : ICoinSearchService
    {
        private readonly ICoinsRepo _coinRepository;
        private readonly IMapper _mapper;
        private readonly IImageProcessingService _imageProcessingService;

        public CoinSearchService(ICoinsRepo coinsRepository, IMapper mapper, IImageProcessingService imageProcessingService)
        {
            _coinRepository = coinsRepository;
            _mapper = mapper;
            _imageProcessingService = imageProcessingService;
        }

        public async Task<CoinReadDto> FindMatchAsync(IFormFile obverse, IFormFile reverse)
        {
            var obvEmbedding = await GetImageEmbeddingAsync(obverse);
            var revEmbedding = await GetImageEmbeddingAsync(reverse);

            var storedEmbeddings = await _coinRepository.GetCoinEmbeddingsAsync();

            var bestMatch = FindBestMatch(obvEmbedding, revEmbedding, storedEmbeddings);

            const double threshold = 0.85;
            if (bestMatch.ObverseSimilarity > threshold && bestMatch.ReverseSimilarity > threshold)
            {
                var coin = await _coinRepository.GetCoinByIdAsync(bestMatch.CoinId);
                return _mapper.Map<CoinReadDto>(coin);
            }

            return null;
        }

        private async Task<float[]> GetImageEmbeddingAsync(IFormFile image)
        {
            using var stream = new MemoryStream();
            await image.CopyToAsync(stream);
            var bytes = stream.ToArray();
            var cropped = await _imageProcessingService.CropAsync(bytes);
            return GetEmbedding(cropped);
        }

        private (int CoinId, double ObverseSimilarity, double ReverseSimilarity) FindBestMatch(
            float[] obvEmbedding, float[] revEmbedding, IEnumerable<CoinEmbeddings> storedEmbeddings)
        {
            int bestCoinId = 0;
            double bestObvSim = 0;
            double bestRevSim = 0;

            foreach (var stored in storedEmbeddings)
            {
                var storedObv = JsonSerializer.Deserialize<float[]>(stored.ObverseEmbedding);
                var storedRev = JsonSerializer.Deserialize<float[]>(stored.ReverseEmbedding);

                var obvSim = CosineSimilarity(obvEmbedding, storedObv);
                var revSim = CosineSimilarity(revEmbedding, storedRev);

                if (obvSim > bestObvSim && revSim > bestRevSim)
                {
                    bestObvSim = obvSim;
                    bestRevSim = revSim;
                    bestCoinId = stored.CoinId;
                }
            }

            return (bestCoinId, bestObvSim, bestRevSim);
        }

        private float[] GetEmbedding(Image<Rgba32> image)
        {           
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var modelPath = Path.Combine(AppContext.BaseDirectory, "ML", "clip-ViT-B-32-vision.onnx");
            var imageTensor = PreprocessImage(ms);

            using var session = new InferenceSession(modelPath);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("pixel_values", imageTensor)
            };

            using var results = session.Run(inputs);

            // Find output by name or index. Common names: "image_embeds", or check session.OutputMetadata.Keys
            var outputTensor = results
                .FirstOrDefault(x => x.Name == "image_embeds")?.AsEnumerable<float>().ToArray()
                ?? results.First().AsEnumerable<float>().ToArray(); // fallback

            // Optional: L2 normalize the embedding (recommended for search)
            var norm = MathF.Sqrt(outputTensor.Sum(v => v * v));
            var normalized = outputTensor.Select(v => v / norm).ToArray();
        
            return normalized;
        }

        private static DenseTensor<float> PreprocessImage(Stream imageStream)
        {
            int targetWidth = 224;
            int targetHeight = 224;
            float[] mean = { 0.48145466f, 0.4578275f, 0.40821073f };
            float[] std = { 0.26862954f, 0.26130258f, 0.27577711f };

            using var image = Image.Load<Rgb24>(imageStream);
            image.Mutate(x => x.Resize(targetWidth, targetHeight));

            var tensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });

            for (int y = 0; y < targetHeight; y++)
                for (int x = 0; x < targetWidth; x++)
                {
                    var pixel = image[x, y];
                    tensor[0, 0, y, x] = ((pixel.R / 255f) - mean[0]) / std[0];
                    tensor[0, 1, y, x] = ((pixel.G / 255f) - mean[1]) / std[1];
                    tensor[0, 2, y, x] = ((pixel.B / 255f) - mean[2]) / std[2];
                }

            return tensor;
        }

        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must be of same length");

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}
