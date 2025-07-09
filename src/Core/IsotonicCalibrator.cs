using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace Edison.Trading.Core
{
    /// <summary>
    /// Provides isotonic regression calibration for an existing ONNX model.
    /// </summary>
    public class IsotonicCalibrator : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private double[]? _thresholds;
        private double[]? _values;

        public IsotonicCalibrator(string onnxModelPath)
        {
            _session = new InferenceSession(onnxModelPath);
            _inputName = _session.InputMetadata.Keys.First();
        }

        /// <summary>
        /// Initializes the calibrator loading previously fitted parameters from a JSON file.
        /// </summary>
        /// <param name="onnxModelPath">Path to the ONNX model.</param>
        /// <param name="calibratorPath">Path to the JSON file with calibration data.</param>
        public IsotonicCalibrator(string onnxModelPath, string calibratorPath)
            : this(onnxModelPath)
        {
            LoadCalibration(calibratorPath);
        }


        /// <summary>
        /// Predicts the calibrated probability for a single feature vector.
        /// </summary>
        public float PredictCalibrated(float[] features)
        {
            float raw = PredictRaw(features);
            return ApplyCalibration(raw);
        }

        /// <summary>
        /// Predicts calibrated probabilities for a batch of feature vectors in a single ONNX invocation.
        /// </summary>
        public float[] PredictCalibratedBatch(float[][] featureMatrix)
        {
            if (_thresholds == null || _values == null)
                throw new InvalidOperationException("Calibrator has not been fitted.");

            var inputName = _inputName;
            int featureCount = _session.InputMetadata[inputName].Dimensions[1];

            var inputTensor = new DenseTensor<float>(new[] { featureMatrix.Length, featureCount });

            for (int i = 0; i < featureMatrix.Length; i++)
            {
                if (featureMatrix[i].Length != featureCount)
                    throw new ArgumentException("Número de features incorreto.");

                for (int j = 0; j < featureCount; j++)
                    inputTensor[i, j] = featureMatrix[i][j];
            }

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };
            using var results = _session.Run(inputs);
            var output = results.Last().AsEnumerable<float>().ToArray();
            int cols = output.Length / featureMatrix.Length;
            var rawScores = new float[featureMatrix.Length];
            for (int i = 0; i < featureMatrix.Length; i++)
                rawScores[i] = output[i * cols + 1];

            var calibrated = new float[rawScores.Length];
            for (int i = 0; i < rawScores.Length; i++)
            {
                calibrated[i] = ApplyCalibration(rawScores[i]);
            }

            return calibrated;
        }

        private float PredictRaw(float[] features)
        {
            var inputName = _inputName;

            if (features.Length != _session.InputMetadata[inputName].Dimensions[1])
                throw new ArgumentException("Número de features incorreto.");

            var inputTensor = new DenseTensor<float>(features, new[] { 1, features.Length });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };
            using var results = _session.Run(inputs);
            var probs = results.Last().AsEnumerable<float>().ToArray();
            return probs.Length == 2 ? probs[1] : probs.First();
        }

        private float ApplyCalibration(float raw)
        {
            if (_thresholds == null || _values == null)
                throw new InvalidOperationException("Calibrator has not been fitted.");

            if (raw <= _thresholds[0])
                return (float)_values[0];

            if (raw >= _thresholds[^1])
                return (float)_values[^1];

            for (int i = _thresholds.Length - 1; i >= 0; i--)
            {
                if (raw >= _thresholds[i])
                    return (float)_values[i];
            }

            throw new InvalidOperationException("Calibration failed.");
        }

        /// <summary>
        /// Loads calibration parameters from a JSON file.
        /// </summary>
        public void LoadCalibration(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<CalibrationData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (data == null)
                throw new InvalidOperationException("Invalid calibration file.");

            _thresholds = data.Thresholds;
            _values = data.Values;
        }


        public void Dispose()
        {
            _session.Dispose();
        }

        private sealed class CalibrationData
        {
            public double[]? Thresholds { get; set; }
            public double[]? Values { get; set; }
        }
    }
}
