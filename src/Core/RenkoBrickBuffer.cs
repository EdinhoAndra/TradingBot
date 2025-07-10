using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Google.Protobuf;
using Edison.Trading.Core;

namespace Edison.Trading.Indicators
{
    /// <summary>
    /// Thread-safe buffer that keeps the most recent RenkoBricks in memory.
    /// Bricks are loaded from a proto-bin file (incremental format) or an
    /// optional CSV fallback. New bricks are appended to the proto file as
    /// they are processed so the historical file grows incrementally.
    /// </summary>
    public class RenkoBrickBuffer
    {
        private readonly int _capacity;
        private readonly ConcurrentDictionary<DateTime, RenkoBrick> _dict = new();
        private readonly LinkedList<DateTime> _order = new();
        private readonly object _lock = new();
        private readonly string _protoPath;
        private readonly string? _csvPath;

        public RenkoBrickBuffer(int capacity, string protoPath, string? csvPath = null)
        {
            _capacity = capacity > 0 ? capacity : throw new ArgumentException("Capacity must be positive", nameof(capacity));
            _protoPath = protoPath;
            _csvPath = csvPath;
            Load();
        }

        /// <summary>
        /// Returns the bricks ordered by timestamp (oldest first).
        /// </summary>
        public IReadOnlyList<RenkoBrick> Bricks
        {
            get
            {
                lock (_lock)
                {
                    var list = new List<RenkoBrick>(_order.Count);
                    foreach (var ts in _order)
                        if (_dict.TryGetValue(ts, out var b)) list.Add(b);
                    return list;
                }
            }
        }

        /// <summary>
        /// Extracts raw series arrays from the buffer for model consumption.
        /// </summary>
        public void ExtractSeries(out Memory<DateTime> timestamps, out Memory<double> open, out Memory<double> high, out Memory<double> low, out Memory<double> close)
        {
            lock (_lock)
            {
                int n = _order.Count;
                var tsArr = new DateTime[n];
                var openArr = new double[n];
                var highArr = new double[n];
                var lowArr = new double[n];
                var closeArr = new double[n];
                int i = 0;
                foreach (var ts in _order)
                {
                    var b = _dict[ts];
                    tsArr[i] = ts;
                    openArr[i] = b.Open;
                    highArr[i] = b.High;
                    lowArr[i] = b.Low;
                    closeArr[i] = b.Close;
                    i++;
                }
                timestamps = tsArr;
                open = openArr;
                high = highArr;
                low = lowArr;
                close = closeArr;
            }
        }

        private void Load()
        {
            bool loaded = false;
            if (File.Exists(_protoPath))
            {
                try
                {
                    using var fs = File.OpenRead(_protoPath);
                    while (fs.Position < fs.Length)
                    {
                        var proto = RenkoBrickProto.Parser.ParseDelimitedFrom(fs);
                        if (proto == null) break;
                        AddBrickInternal(Convert(proto));
                    }
                    loaded = _order.Count > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RenkoBrickBuffer] Erro ao ler {_protoPath}: {ex.Message}");
                    MoveCorruptedFile(_protoPath);
                }
            }

            if (!loaded && _csvPath != null && File.Exists(_csvPath))
            {
                try
                {
                    using var reader = new StreamReader(_csvPath);
                    string? line = reader.ReadLine(); // header
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        if (parts.Length < 5) continue;
                        var dt = DateTime.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                        var brick = new RenkoBrick
                        {
                            Open = double.Parse(parts[1], CultureInfo.InvariantCulture),
                            High = double.Parse(parts[2], CultureInfo.InvariantCulture),
                            Low = double.Parse(parts[3], CultureInfo.InvariantCulture),
                            Close = double.Parse(parts[4], CultureInfo.InvariantCulture),
                            Direction = double.Parse(parts[4], CultureInfo.InvariantCulture) >= double.Parse(parts[1], CultureInfo.InvariantCulture) ? RenkoDirection.Up : RenkoDirection.Down,
                            Timestamp = SystemTime.FromDateTime(dt)
                        };
                        AddBrickInternal(brick);
                    }
                    loaded = _order.Count > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RenkoBrickBuffer] Erro ao ler {_csvPath}: {ex.Message}");
                    MoveCorruptedFile(_csvPath);
                }
            }
        }

        private static RenkoBrick Convert(RenkoBrickProto p)
        {
            var dt = new DateTime(p.Timestamp, DateTimeKind.Utc);
            return new RenkoBrick
            {
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Direction = (RenkoDirection)p.Direction,
                Timestamp = SystemTime.FromDateTime(dt)
            };
        }

        private void AddBrickInternal(RenkoBrick brick)
        {
            var ts = SystemTime.ToDateTime(brick.Timestamp);
            lock (_lock)
            {
                _dict[ts] = brick;
                _order.AddLast(ts);
                while (_order.Count > _capacity)
                {
                    var oldTs = _order.First!.Value;
                    _order.RemoveFirst();
                    _dict.TryRemove(oldTs, out _);
                }
            }
        }

        /// <summary>
        /// Adds a brick to the in-memory buffer and appends it to the proto file.
        /// </summary>
        public void AddBrick(RenkoBrick brick)
        {
            AddBrickInternal(brick);
            AppendBrickToFile(brick);
        }

        private void AppendBrickToFile(RenkoBrick brick)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_protoPath)!);
                    using var fs = new FileStream(_protoPath, FileMode.Append, FileAccess.Write, FileShare.Read);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RenkoBrickBuffer] Erro ao persistir {_protoPath}: {ex.Message}");
            }
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
                string newName = $"{fileName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
                string destPath = Path.Combine(corruptedDir, newName);
                File.Move(path, destPath, overwrite: true);
            }
            catch (Exception moveEx)
            {
                Console.WriteLine($"[RenkoBrickBuffer] Falha ao mover arquivo corrompido {path}: {moveEx.Message}");
            }
        }
    }
}
