using NUnit.Framework;
using Edison.Trading.Core;
using Edison.Trading.Api;

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
            Assert.That(reversalBrick.Low, Is.EqualTo(99925));
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
}

