using System;
using Edison.Trading.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Diagnostics.CodeAnalysis;


namespace Edison.Trading.Core
{



public enum RenkoDirection
{
    Up,
    Down
}
// Fim da classe NelogicaRenkoGenerator
}
// Fim do namespace Edison.Trading.Core
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
    // Buffer FIFO para os últimos N renkos
    private int _bufferSize = 100;
    private readonly ConcurrentQueue<RenkoBrick> _renkoBuffer = new();
    private readonly object _bufferLock = new();
    private string _saveFilePath = "renkos.bin";
    private string? _csvFallbackPath;

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

    /// <summary>
    /// Exporta o buffer de renkos para um arquivo CSV.
    /// </summary>
    /// <param name="csvFilePath">Caminho do arquivo CSV de destino.</param>
    public void ExportBufferToCsv(string csvFilePath)
    {
        lock (_bufferLock)
        {
            using var writer = new StreamWriter(csvFilePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("Open,High,Low,Close,Direction,Timestamp");
            foreach (var brick in _renkoBuffer)
            {
                var dt = SystemTime.ToDateTime(brick.Timestamp).ToString("yyyy-MM-dd HH:mm:ss.fff");
                writer.WriteLine($"{brick.Open},{brick.High},{brick.Low},{brick.Close},{brick.Direction},{dt}");
            }
        }
    }

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

        TryLoadBufferFromDisk();
    }

    public void ConfigureBuffer(int bufferSize, string saveFilePath, string? csvFallbackPath = null)
    {
        _bufferSize = bufferSize;
        _saveFilePath = saveFilePath;
        _csvFallbackPath = csvFallbackPath;

        lock (_bufferLock)
        {
            while (_renkoBuffer.Count > _bufferSize)
                _renkoBuffer.TryDequeue(out _);
        }

        while (_bricks.Count > _bufferSize)
            _bricks.RemoveAt(0);
    }

    public bool TryLoadLastBrickFromDisk([NotNullWhen(true)] out RenkoBrick? lastBrick)
    {
        lastBrick = null;
        if (!File.Exists(_saveFilePath))
            return false;
        try
        {
            using var fs = File.OpenRead(_saveFilePath);
            var proto = RenkoBufferProto.Parser.ParseFrom(fs);
            if (proto.Bricks.Count == 0)
                return false;
            var p = proto.Bricks[^1];
            var dt = new DateTime(p.Timestamp, DateTimeKind.Utc);
            lastBrick = new RenkoBrick
            {
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Direction = (RenkoDirection)p.Direction,
                Timestamp = SystemTime.FromDateTime(dt)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void InitializeFromLastBrick(double close, RenkoDirection direction)
    {
        _bricks.Clear();
        var ts = SystemTime.FromDateTime(DateTime.UtcNow);
        var last = new RenkoBrick
        {
            Open = close,
            High = close,
            Low = close,
            Close = close,
            Direction = direction,
            Timestamp = ts
        };
        _bricks.Add(last);
        _currentSwingHigh = close;
        _currentSwingLow = close;
        _anchorPrice = null;
        _anchorTimestamp = null;
    }


    private void TryLoadBufferFromDisk()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                using var fs = File.OpenRead(_saveFilePath);
                var proto = RenkoBufferProto.Parser.ParseFrom(fs);
                foreach (var p in proto.Bricks)
                {
                    var dt = new DateTime(p.Timestamp, DateTimeKind.Utc);
                    var brick = new RenkoBrick
                    {
                        Open = p.Open,
                        High = p.High,
                        Low = p.Low,
                        Close = p.Close,
                        Direction = (RenkoDirection)p.Direction,
                        Timestamp = SystemTime.FromDateTime(dt)
                    };
                    _renkoBuffer.Enqueue(brick);
                    _bricks.Add(brick);
                }
                TrimLoaded();
                if (_renkoBuffer.TryPeek(out var last))
                {
                    Console.WriteLine($"[RENKO] Último preço do dia anterior: {last.Close}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RENKO] Erro ao carregar buffer: {ex.Message}");
                MoveCorruptedFile(_saveFilePath);
            }
        }

        if (_renkoBuffer.Count == 0 && _csvFallbackPath != null && File.Exists(_csvFallbackPath))
        {
            try
            {
                using var reader = new StreamReader(_csvFallbackPath);
                string? line = reader.ReadLine(); // header
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 6) continue;
                    var dt = DateTime.Parse(parts[5], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                    var brick = new RenkoBrick
                    {
                        Open = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                        High = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                        Low = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                        Close = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                        Direction = Enum.TryParse(parts[4], true, out RenkoDirection dir) ? dir : RenkoDirection.Up,
                        Timestamp = SystemTime.FromDateTime(dt)
                    };
                    _renkoBuffer.Enqueue(brick);
                    _bricks.Add(brick);
                }
                TrimLoaded();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RENKO] Erro ao carregar CSV: {ex.Message}");
                if (_csvFallbackPath != null)
                    MoveCorruptedFile(_csvFallbackPath);
            }
        }
    }

    private void TrimLoaded()
    {
        lock (_bufferLock)
        {
            while (_renkoBuffer.Count > _bufferSize)
                _renkoBuffer.TryDequeue(out _);
        }
        while (_bricks.Count > _bufferSize)
            _bricks.RemoveAt(0);
    }

    private void SaveBufferToDisk()
    {
        lock (_bufferLock)
        {
            var proto = new RenkoBufferProto();
            foreach (var brick in _renkoBuffer)
            {
                long ticks = SystemTime.ToDateTime(brick.Timestamp).ToUniversalTime().Ticks;
                proto.Bricks.Add(new RenkoBrickProto
                {
                    Open = brick.Open,
                    High = brick.High,
                    Low = brick.Low,
                    Close = brick.Close,
                    Direction = (int)brick.Direction,
                    Timestamp = ticks
                });
            }
            using var fs = File.Create(_saveFilePath);
            proto.WriteTo(fs);
        }
    }

    private void AppendBrickToFile(RenkoBrick brick)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_saveFilePath)!);
            using var fs = new FileStream(_saveFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            var proto = new RenkoBrickProto
            {
                Open = brick.Open,
                High = brick.High,
                Low = brick.Low,
                Close = brick.Close,
                Direction = (int)brick.Direction,
                Timestamp = SystemTime.ToDateTime(brick.Timestamp).ToUniversalTime().Ticks
            };
            proto.WriteDelimitedTo(fs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RENKO] Erro ao persistir {_saveFilePath}: {ex.Message}");
        }
    }

    public void PersistBuffer()
    {
        SaveBufferToDisk();
    }

    private static void MoveCorruptedFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            string? dir = Path.GetDirectoryName(path);
            if (dir == null) return;
            string corruptedDir = Path.Combine(dir, "corrupted");
            Directory.CreateDirectory(corruptedDir);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            string destPath = Path.Combine(corruptedDir, $"{fileName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}");
            File.Move(path, destPath, overwrite: true);
        }
        catch (Exception moveEx)
        {
            Console.WriteLine($"[RENKO] Falha ao mover arquivo corrompido {path}: {moveEx.Message}");
        }
    }

    public virtual void AddPrice(double currentPrice, SystemTime timestamp)
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
        if (_anchorPrice is null)
            throw new InvalidOperationException("AnchorPrice não inicializado.");
        double openPrice = _anchorPrice.Value;
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
        if (_bricks.Count > _bufferSize)
            _bricks.RemoveAt(0);

        lock (_bufferLock)
        {
            _renkoBuffer.Enqueue(newBrick);
            while (_renkoBuffer.Count > _bufferSize)
                _renkoBuffer.TryDequeue(out _);
        }

        // Save and calculate features concurrently
        _ = Task.Run(() => AppendBrickToFile(newBrick));
        _ = Task.Run(() => OnCloseBrick?.Invoke(newBrick));
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

        double high = direction == RenkoDirection.Up ? reversalClose : _currentSwingHigh;
        double low = direction == RenkoDirection.Down ? reversalClose : _currentSwingLow;

        CreateAndAddBrick(openPrice, reversalClose, direction, timestamp, high, low);

        double remainingMovement = absMovement - ThresholdReversalMove;
        if (remainingMovement >= ThresholdRegularMove)
        {
            AddBricksSeries(reversalClose, direction == RenkoDirection.Up ? remainingMovement : -remainingMovement, timestamp);
        }
    }
}

