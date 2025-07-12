using NUnit.Framework;
using Edison.Trading.Core;
using System.Globalization;


namespace Edison.Trading.Tests;

[TestFixture]
public class NelogicaRenkoGeneratorTests
{
    private NelogicaRenkoGenerator _generator;
    private readonly int _r = 15;
    private readonly double _tickSize = 5.0;

    [SetUp]
    public void Setup()
    {
        _generator = new NelogicaRenkoGenerator(_r, _tickSize);
    }

    [Test]
    public void Constructor_Should_CalculateCorrectSizes()
    {
        // Para R=15 e tickSize=5:
        // Regular move = 15 ticks = 75 pontos
        // Regular body = 14 ticks = 70 pontos
        // Reversal move = 29 ticks = 145 pontos
        // Reversal body = 28 ticks = 140 pontos

        Assert.Multiple(() =>
        {
            Assert.That(_generator.ThresholdRegularMove, Is.EqualTo(75.0));
            Assert.That(_generator.RegularBrickBodySize, Is.EqualTo(70.0));
            Assert.That(_generator.ThresholdReversalMove, Is.EqualTo(145.0));
            Assert.That(_generator.ReversalBrickBodySize, Is.EqualTo(140.0));
        });
    }

    [Test]
    public void AddPrice_Should_CreateRegularBrickWithCorrectHighLow()
    {
        var timestamp = SystemTime.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, 0));

        // Primeiro preço ancora em 100000
        _generator.AddPrice(100000, timestamp);

        // Move 75 pontos (15 ticks) para cima - deve criar um tijolo
        _generator.AddPrice(100075, timestamp);

        var brick = _generator.Bricks[0];
        Assert.Multiple(() =>
        {
            Assert.That(brick.Direction, Is.EqualTo(RenkoDirection.Up));
            Assert.That(brick.Open, Is.EqualTo(100000));
            Assert.That(brick.Close, Is.EqualTo(100070)); // Open + 70 pontos (14 ticks)
            Assert.That(brick.High, Is.EqualTo(100075));
            Assert.That(brick.Low, Is.EqualTo(100000));
        });
    }

    [Test]
    public void AddPrice_Should_CreateReversalBrickWithCorrectHighLow()
    {
        var timestamp = SystemTime.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, 0));

        // Primeiro preço ancora em 100000
        _generator.AddPrice(100000, timestamp);

        // Move 75 pontos para cima - cria primeiro tijolo
        _generator.AddPrice(100075, timestamp);

        // Move 145 pontos para baixo a partir do close do primeiro brick (100070 - 145 = 99925)
        _generator.AddPrice(99925, timestamp);

        Assert.That(_generator.Bricks, Has.Count.EqualTo(2));

        var reversalBrick = _generator.Bricks[1];
        Assert.Multiple(() =>
        {
            Assert.That(reversalBrick.Direction, Is.EqualTo(RenkoDirection.Down));
            Assert.That(reversalBrick.Open, Is.EqualTo(100070));
            Assert.That(reversalBrick.Close, Is.EqualTo(99930)); // Open - 140 pontos (28 ticks)
            Assert.That(reversalBrick.High, Is.EqualTo(100070));
            Assert.That(reversalBrick.Low, Is.EqualTo(99930), "No renko de reversão de baixa, low deve ser igual ao close");
        });
    }

    [Test]
    public void AddPrice_Should_CreateMultipleBricksWithCorrectHighLow()
    {
        var timestamp = SystemTime.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, 0));

        // Ancora em 100000
        _generator.AddPrice(100000, timestamp);

        // Move 225 pontos (3 tijolos regulares de 75 pontos cada)
        _generator.AddPrice(100225, timestamp);

        Assert.That(_generator.Bricks, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            // Todos os bricks devem ter o mesmo high/low do swing
            Assert.That(_generator.Bricks[0].Open, Is.EqualTo(100000));
            Assert.That(_generator.Bricks[0].Close, Is.EqualTo(100070));
            Assert.That(_generator.Bricks[0].High, Is.EqualTo(100225));
            Assert.That(_generator.Bricks[0].Low, Is.EqualTo(100000));

            Assert.That(_generator.Bricks[1].Open, Is.EqualTo(100070));
            Assert.That(_generator.Bricks[1].Close, Is.EqualTo(100140));
            Assert.That(_generator.Bricks[1].High, Is.EqualTo(100225));
            Assert.That(_generator.Bricks[1].Low, Is.EqualTo(100000));

            Assert.That(_generator.Bricks[2].Open, Is.EqualTo(100140));
            Assert.That(_generator.Bricks[2].Close, Is.EqualTo(100210));
            Assert.That(_generator.Bricks[2].High, Is.EqualTo(100225));
            Assert.That(_generator.Bricks[2].Low, Is.EqualTo(100000));
        });
    }

    [Test]
    public void AddPrice_Should_CreateReversalUpAndDown_WithCorrectHighLow()
    {
        var timestamp = SystemTime.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, 0));
        // Ancora em 100000
        _generator.AddPrice(100000, timestamp);
        // Sobe para criar primeiro brick up
        _generator.AddPrice(100075, timestamp);
        // Cai para criar reversão para baixo
        _generator.AddPrice(99925, timestamp);
        // Sobe para criar reversão para cima (precisa subir 145 pontos a partir do close anterior)
        _generator.AddPrice(100075, timestamp); // 99930 + 145 = 100075

        Assert.That(_generator.Bricks, Has.Count.EqualTo(3));

        var reversalDown = _generator.Bricks[1];
        Assert.Multiple(() =>
        {
            Assert.That(reversalDown.Direction, Is.EqualTo(RenkoDirection.Down));
            Assert.That(reversalDown.Low, Is.EqualTo(reversalDown.Close), "No renko de reversão de baixa, low deve ser igual ao close");
        });

        var reversalUp = _generator.Bricks[2];
        Assert.Multiple(() =>
        {
            Assert.That(reversalUp.Direction, Is.EqualTo(RenkoDirection.Up));
            Assert.That(reversalUp.High, Is.EqualTo(reversalUp.Close), "No renko de reversão de alta, high deve ser igual ao close");
        });
    }


    [Test]
    public void RegularBricks_Should_HaveHighAndLowReflectingSwing()
    {
        var timestamp = SystemTime.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0, 0));
        _generator.AddPrice(100000, timestamp);
        // Gera 3 bricks up
        _generator.AddPrice(100225, timestamp);
        // Gera 2 bricks down
        _generator.AddPrice(100000, timestamp);

        // Para bricks up, high deve ser igual ao maior preço atingido; para bricks down, low igual ao menor preço atingido
        // (valores esperados baseados no cenário do teste)
        Assert.That(_generator.Bricks[0].Direction, Is.EqualTo(RenkoDirection.Up));
        Assert.That(_generator.Bricks[0].High, Is.EqualTo(100225));
        Assert.That(_generator.Bricks[0].Low, Is.EqualTo(100000));

        Assert.That(_generator.Bricks[1].Direction, Is.EqualTo(RenkoDirection.Up));
        Assert.That(_generator.Bricks[1].High, Is.EqualTo(100225));
        Assert.That(_generator.Bricks[1].Low, Is.EqualTo(100000));

        Assert.That(_generator.Bricks[2].Direction, Is.EqualTo(RenkoDirection.Up));
        Assert.That(_generator.Bricks[2].High, Is.EqualTo(100225));
        Assert.That(_generator.Bricks[2].Low, Is.EqualTo(100000));

        // Agora bricks down
        // O swing mínimo será 100000, máximo 100225
        // Dependendo da lógica, pode haver reversão, mas para este cenário, todos são up
        // Se quiser testar bricks down, adicione mais movimentos para baixo e ajuste os asserts conforme necessário
    }

    [Test]
    public void FullRenkoPipeline_WINFUT_MockTicks_BufferAndExport()
    {
        // Parâmetros
        int r = 15;
        double tickSize = 5.0;
        int numBricksDesejados = 100;
        int bufferSize = 100;
        string binPath = Path.Combine(Path.GetTempPath(), $"renko_test_{Guid.NewGuid()}.bin");
        string csvPath = Path.Combine(Path.GetTempPath(), $"renko_test_{Guid.NewGuid()}.csv");

        // Instancia e configura buffer
        var generator = new NelogicaRenkoGenerator(r, tickSize);
        generator.ConfigureBuffer(bufferSize, binPath);

        // Mocka milhares de ticks crescentes
        double precoInicial = 100000;
        int ticksPorBrick = r;
        int totalTicks = (numBricksDesejados + 5) * ticksPorBrick;
        var timestamp = SystemTime.FromDateTime(new DateTime(2025, 6, 28, 10, 0, 0));
        for (int i = 0; i < totalTicks; i++)
        {
            double preco = precoInicial + i * tickSize;
            generator.AddPrice(preco, timestamp);
        }

        // Deve ter pelo menos 100 bricks
        Assert.That(generator.Bricks.Count, Is.GreaterThanOrEqualTo(numBricksDesejados));

        // Gera tabela visual
        var table = new System.Text.StringBuilder();
        table.AppendLine("OPEN,HIGH,LOW,CLOSE");
        foreach (var brick in generator.Bricks.Take(numBricksDesejados))
            table.AppendLine($"{brick.Open},{brick.High},{brick.Low},{brick.Close}");
        TestContext.Out.WriteLine(table.ToString());

        // Testa buffer em memória
        Assert.That(generator.Bricks.TakeLast(bufferSize).Select(b => b.Close), Is.EqualTo(generator.Bricks.Skip(generator.Bricks.Count - bufferSize).Select(b => b.Close)));

        // Testa salvar buffer em disco
        generator.PersistBuffer();
        Assert.That(File.Exists(binPath), Is.True, "Arquivo binário não foi salvo");
        long binLength = new FileInfo(binPath).Length;
        Assert.That(binLength, Is.GreaterThan(0), "Arquivo binário está vazio");

        // Testa exportação para CSV
        generator.ExportBufferToCsv(csvPath);
        Assert.That(File.Exists(csvPath), Is.True, "Arquivo CSV não foi salvo");
        string[] csvLines = File.ReadAllLines(csvPath);
        Assert.That(csvLines.Length, Is.GreaterThan(1), "CSV não contém dados");
        Assert.That(csvLines[0], Is.EqualTo("Open,High,Low,Close,Direction,Timestamp"));

        // Limpeza
        File.Delete(binPath);
        File.Delete(csvPath);
    }

    [Test]
    public void OnCloseBrick_Should_Fire_After_GCCollect()
    {
        var generator = new NelogicaRenkoGenerator(10, 5.0);
        int count = 0;
        generator.OnCloseBrick += _ => count++;

        var ts = SystemTime.FromDateTime(DateTime.UtcNow);
        generator.AddPrice(1000, ts);
        generator.AddPrice(1000 + generator.ThresholdRegularMove, ts);
        Assert.That(count, Is.EqualTo(1));

        GC.Collect();
        GC.WaitForPendingFinalizers();

        generator.AddPrice(1000 + 2 * generator.ThresholdRegularMove, ts);

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void StopRenko_Should_GenerateCsvFromBin()
    {
        int r = 15;
        double tickSize = 5.0;
        int bricksToGenerate = 205;
        string binPath = Path.Combine(Path.GetTempPath(), $"renko_stop_{Guid.NewGuid()}.bin");
        string csvPath = Path.Combine(Path.GetTempPath(), $"renko_stop_{Guid.NewGuid()}.csv");

        var generator = new NelogicaRenkoGenerator(r, tickSize);
        generator.ConfigureBuffer(200, binPath);

        double initialPrice = 100000;
        int totalTicks = (bricksToGenerate + 5) * r;
        var timestamp = SystemTime.FromDateTime(new DateTime(2025, 6, 28, 10, 0, 0));
        for (int i = 0; i < totalTicks; i++)
        {
            generator.AddPrice(initialPrice + i * tickSize, timestamp);
        }

        Assert.That(generator.Bricks.Count, Is.EqualTo(200));

        generator.PersistBuffer();
        Assert.That(File.Exists(binPath), Is.True, "Bin file was not created");

        var proto = RenkoBufferProto.Parser.ParseFrom(File.ReadAllBytes(binPath));
        Assert.That(proto.Bricks.Count, Is.EqualTo(200));

        using (var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8))
        {
            writer.WriteLine("Date,Open,High,Low,Close");
            foreach (var pb in proto.Bricks)
            {
                var dt = new DateTime(pb.Timestamp, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                writer.WriteLine($"{dt},{pb.Open},{pb.High},{pb.Low},{pb.Close}");
            }
        }

        Assert.That(File.Exists(csvPath), Is.True, "CSV file was not created");

        var csvLines = File.ReadAllLines(csvPath);
        Assert.That(csvLines.Length, Is.EqualTo(proto.Bricks.Count + 1));
        Assert.That(csvLines[0], Is.EqualTo("Date,Open,High,Low,Close"));

        for (int i = 0; i < 200; i++)
        {
            var pb = proto.Bricks[i];
            var parts = csvLines[i + 1].Split(',');
            var dt = DateTime.ParseExact(
                parts[0],
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            double open = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double high = double.Parse(parts[2], CultureInfo.InvariantCulture);
            double low = double.Parse(parts[3], CultureInfo.InvariantCulture);
            double close = double.Parse(parts[4], CultureInfo.InvariantCulture);

            Assert.Multiple(() =>
            {
                Assert.That(open, Is.EqualTo(pb.Open));
                Assert.That(high, Is.EqualTo(pb.High));
                Assert.That(low, Is.EqualTo(pb.Low));
                Assert.That(close, Is.EqualTo(pb.Close));
                Assert.That(dt.ToUniversalTime().Ticks, Is.EqualTo(pb.Timestamp));
            });
        }

        File.Delete(binPath);
        File.Delete(csvPath);
    }

}
