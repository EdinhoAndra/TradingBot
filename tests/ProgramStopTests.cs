using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Edison.Trading.Core;
using NUnit.Framework;

namespace Edison.Trading.Tests;

[TestFixture]
public class ProgramStopTests
{
    [Test]
    public void Stop_Should_FlushBufferAndGenerateCsvFromBin()
    {
        int r = 15;
        double tickSize = 5.0;
        int bufferSize = 200;
        string binPath = Path.Combine(Path.GetTempPath(), $"renko_{Guid.NewGuid()}.bin");
        string csvPath = Path.Combine(Path.GetTempPath(), $"renko_{Guid.NewGuid()}.csv");

        var generator = new NelogicaRenkoGenerator(r, tickSize);
        generator.ConfigureBuffer(bufferSize, binPath);

        double price = 100000;
        var ts = SystemTime.FromDateTime(DateTime.UtcNow);
        generator.AddPrice(price, ts);
        for (int i = 1; i <= bufferSize * r; i++)
        {
            price += tickSize;
            generator.AddPrice(price, ts);
        }

        generator.PersistBuffer();
        Assert.That(File.Exists(binPath), Is.True, "Binary file not saved");

        RenkoBufferProto proto;
        using (var fs = File.OpenRead(binPath))
        {
            proto = RenkoBufferProto.Parser.ParseFrom(fs);
        }
        Assert.That(proto.Bricks.Count, Is.EqualTo(bufferSize));

        ExportRenkoCsv(generator, csvPath, bufferSize);
        Assert.That(File.Exists(csvPath), Is.True, "CSV file not created");

        var lines = File.ReadAllLines(csvPath);
        Assert.That(lines[0], Is.EqualTo("Date,Open,High,Low,Close"));
        Assert.That(lines.Length, Is.EqualTo(bufferSize + 1));

        for (int i = 0; i < bufferSize; i++)
        {
            var p = proto.Bricks[i];
            var dt = new DateTime(p.Timestamp, DateTimeKind.Utc)
                .ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var expected = $"{dt},{p.Open},{p.High},{p.Low},{p.Close}";
            Assert.That(lines[i + 1], Is.EqualTo(expected));
        }

        File.Delete(binPath);
        File.Delete(csvPath);
    }

    private static void ExportRenkoCsv(NelogicaRenkoGenerator generator, string path, int limit)
    {
        var start = Math.Max(generator.Bricks.Count - limit, 0);
        using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
        writer.WriteLine("Date,Open,High,Low,Close");
        foreach (var brick in generator.Bricks.Skip(start))
        {
            var dt = SystemTime.ToDateTime(brick.Timestamp)
                .ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            writer.WriteLine($"{dt},{brick.Open},{brick.High},{brick.Low},{brick.Close}");
        }
    }
}
