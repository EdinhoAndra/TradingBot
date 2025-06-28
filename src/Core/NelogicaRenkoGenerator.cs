using System;
using System.Collections.Generic;
using System.Linq;
using Edison.Trading.Api;

namespace Edison.Trading.Core;

public enum RenkoDirection
{
    Up,
    Down
}

public class RenkoBrick
{
    public double Open { get; internal set; }
    public double High { get; internal set; }
    public double Low { get; internal set; }
    public double Close { get; internal set; }
    public RenkoDirection Direction { get; internal set; }
    public SystemTime Timestamp { get; internal set; }

    public override string ToString()
    {
        return $"[{SystemTime.ToDateTime(Timestamp):HH:mm:ss.fff}] {Direction} | O: {Open:F2} | H: {High:F2} | L: {Low:F2} | C: {Close:F2}";
    }
}

/// <summary>
/// Gera um gráfico Renko seguindo exatamente as regras e especificações da plataforma Nelógica.
/// </summary>
public class NelogicaRenkoGenerator
{
    public int R { get; }
    public double TickSize { get; }
    public double RegularBrickBodySize { get; }
    public double ReversalBrickBodySize { get; }
    public double ThresholdRegularMove { get; }
    public double ThresholdReversalMove { get; }

    public IReadOnlyList<RenkoBrick> Bricks => _bricks;
    private readonly List<RenkoBrick> _bricks = new();
    public event Action<RenkoBrick>? OnCloseBrick;

    private double? _anchorPrice;
    private SystemTime? _anchorTimestamp;
    private double _currentSwingHigh;
    private double _currentSwingLow;

    public NelogicaRenkoGenerator(int r, double tickSize)
    {
        if (tickSize <= 0)
            throw new ArgumentException("O tamanho do tick deve ser maior que zero.", nameof(tickSize));

        R = r < 2 ? 2 : r;
        TickSize = tickSize;

        ThresholdRegularMove = R * TickSize;
        RegularBrickBodySize = (R - 1) * TickSize;

        ThresholdReversalMove = (2 * R - 1) * TickSize;
        ReversalBrickBodySize = (2 * R - 2) * TickSize;
    }

    public void AddPrice(double currentPrice, SystemTime timestamp)
    {
        if (_anchorPrice == null && !_bricks.Any())
        {
            _anchorPrice = currentPrice;
            _anchorTimestamp = timestamp;
            _currentSwingHigh = currentPrice;
            _currentSwingLow = currentPrice;
            return;
        }

        _currentSwingHigh = Math.Max(_currentSwingHigh, currentPrice);
        _currentSwingLow = Math.Min(_currentSwingLow, currentPrice);

        int initialBrickCount = _bricks.Count;

        if (!_bricks.Any())
        {
            TryCreateFirstBrick(currentPrice, timestamp);
        }
        else
        {
            ProcessNextMove(currentPrice, timestamp);
        }

        if (_bricks.Count > initialBrickCount)
        {
            _currentSwingHigh = _bricks.Last().Close;
            _currentSwingLow = _bricks.Last().Close;
        }
    }

    private void TryCreateFirstBrick(double currentPrice, SystemTime timestamp)
    {
        double openPrice = _anchorPrice!.Value;
        double movement = currentPrice - openPrice;

        if (Math.Abs(movement) >= ThresholdRegularMove)
        {
            AddBricksSeries(openPrice, movement, timestamp);
            _anchorPrice = null;
            _anchorTimestamp = null;
        }
    }

    private void ProcessNextMove(double currentPrice, SystemTime timestamp)
    {
        var lastBrick = _bricks.Last();
        double movement = currentPrice - lastBrick.Close;

        if (lastBrick.Direction == RenkoDirection.Up)
        {
            if (movement >= ThresholdRegularMove)
            {
                AddBricksSeries(lastBrick.Close, movement, timestamp);
            }
            else if (movement <= -ThresholdReversalMove)
            {
                AddReversalBrick(lastBrick.Close, movement, timestamp);
            }
        }
        else
        {
            if (movement <= -ThresholdRegularMove)
            {
                AddBricksSeries(lastBrick.Close, movement, timestamp);
            }
            else if (movement >= ThresholdReversalMove)
            {
                AddReversalBrick(lastBrick.Close, movement, timestamp);
            }
        }
    }

    private void CreateAndAddBrick(double open, double close, RenkoDirection direction, SystemTime timestamp, double high, double low)
    {
        var newBrick = new RenkoBrick
        {
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Direction = direction,
            Timestamp = timestamp
        };
        _bricks.Add(newBrick);
        OnCloseBrick?.Invoke(newBrick);
    }

    private void AddBricksSeries(double openPrice, double totalMovement, SystemTime timestamp)
    {
        var direction = totalMovement > 0 ? RenkoDirection.Up : RenkoDirection.Down;
        double absMovement = Math.Abs(totalMovement);

        int brickCount = (int)(absMovement / ThresholdRegularMove);

        if (brickCount == 0) return;

        double swingHigh = _currentSwingHigh;
        double swingLow = _currentSwingLow;

        double currentOpen = openPrice;
        for (int i = 0; i < brickCount; i++)
        {
            double closePrice = currentOpen + (direction == RenkoDirection.Up ? RegularBrickBodySize : -RegularBrickBodySize);
            CreateAndAddBrick(currentOpen, closePrice, direction, timestamp, swingHigh, swingLow);
            currentOpen = closePrice;
        }
    }

    private void AddReversalBrick(double openPrice, double totalMovement, SystemTime timestamp)
    {
        var direction = totalMovement > 0 ? RenkoDirection.Up : RenkoDirection.Down;
        double absMovement = Math.Abs(totalMovement);

        double reversalClose = openPrice + (direction == RenkoDirection.Up ? ReversalBrickBodySize : -ReversalBrickBodySize);
        CreateAndAddBrick(openPrice, reversalClose, direction, timestamp, _currentSwingHigh, _currentSwingLow);

        double remainingMovement = absMovement - ThresholdReversalMove;
        if (remainingMovement >= ThresholdRegularMove)
        {
            AddBricksSeries(reversalClose, direction == RenkoDirection.Up ? remainingMovement : -remainingMovement, timestamp);
        }
    }
}

