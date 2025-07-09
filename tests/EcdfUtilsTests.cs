using System;
using System.Linq;
using System.IO;
using Edison.Trading.Core;
using NUnit.Framework;

namespace Edison.Trading.Tests;

[TestFixture]
public class EcdfUtilsTests
{
    [Test]
    public void GenerateEcdfDecisions_Should_GenerateRowsWithValidValues()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string onnxPath = Path.Combine(root, "files", "pipeline_lightgbm.onnx");
        string calPath = Path.Combine(root, "files", "isotonic_calibrator.json");
        var calibrator = new IsotonicCalibrator(onnxPath, calPath);

        // create random data with 11 features
        var rnd = new Random(0);
        float[][] xRef = Enumerable.Range(0, 20)
            .Select(_ => Enumerable.Range(0, 11).Select(__ => (float)rnd.NextDouble()).ToArray())
            .ToArray();
        float[][] xNew = Enumerable.Range(0, 5)
            .Select(_ => Enumerable.Range(0, 11).Select(__ => (float)rnd.NextDouble()).ToArray())
            .ToArray();

        var result = EcdfUtils.GenerateEcdfDecisions(calibrator, xRef, xNew, DecisionMode.Filter);

        Assert.That(result, Has.Count.EqualTo(5));
        foreach (var row in result)
        {
            Assert.That(row.ProbClass0 + row.ProbClass1, Is.EqualTo(1.0).Within(1e-6));
            Assert.That(row.Percentile, Is.InRange(0.0, 1.0));
            Assert.That(row.Sizing, Is.InRange(0, 1));
            Assert.That(row.Date, Is.Null);
        }
    }
}
