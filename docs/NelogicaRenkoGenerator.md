using System;
using System.Collections.Generic;
using System.Linq;
using ProfitDLL;


/// <summary>
/// Define a direção de um tijolo Renko.
/// Se o preço subir, o Renko é considerado "Up" (Alta).
/// Se o preço descer, o Renko é considerado "Down" (Baixa).
/// </summary>
public enum RenkoDirection
{
    Up,
    Down
}

/// <summary>
/// Representa um único tijolo (box) no gráfico Renko, com suas informações de Abertura, Máxima, Mínima e Fechamento.
/// </summary>
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
    // --- Propriedades de Configuração ---
    public int R { get; }
    public double TickSize { get; }

    // --- Propriedades Calculadas (Regras Nelógica) ---
    public double RegularBrickBodySize { get; }
    public double ReversalBrickBodySize { get; }
    public double ThresholdRegularMove { get; } // Renomeado de RegularBrickMove
    public double ThresholdReversalMove { get; } // Renomeado de ReversalBrickMove

    // --- Estado Interno ---
    public IReadOnlyList<RenkoBrick> Bricks => _bricks;
    private readonly List<RenkoBrick> _bricks = new List<RenkoBrick>();
    public event Action<RenkoBrick>? OnCloseBrick;

    // Preço âncora para determinar o ponto de partida do primeiro tijolo.
    private double? _anchorPrice;
    private SystemTime? _anchorTimestamp;

    // Rastreia os extremos do movimento de preço desde o último tijolo fechado.
    private double _currentSwingHigh;
    private double _currentSwingLow;

    /// <summary>
    /// Cria uma nova instância do gerador Renko com as especificações da Nelógica.
    /// </summary>
    /// <param name="r">O valor 'R' do Renko (ex: 5 para 5R). Se for informado 1, será automaticamente convertido para 2.</param>
    /// <param name="tickSize">O tamanho do tick do ativo (ex: 5.0 para WIN, 0.5 para WDO).</param>
    public NelogicaRenkoGenerator(int r, double tickSize)
    {
        if (tickSize <= 0)
            throw new ArgumentException("O tamanho do tick deve ser maior que zero.", nameof(tickSize));

        R = r < 2 ? 2 : r;
        TickSize = tickSize;

        // Regra Nelogica: Movimento de R ticks para um box regular.
        ThresholdRegularMove = R * TickSize;
        // O corpo do box (diferença Open-Close) é de (R-1) ticks.
        RegularBrickBodySize = (R - 1) * TickSize;

        // Regra Nelogica: Movimento de (2*R - 1) ticks para uma reversão.
        ThresholdReversalMove = (2 * R - 1) * TickSize;
        // O corpo do box de reversão é o movimento de reversão menos um tick.
        ReversalBrickBodySize = (2 * R - 2) * TickSize;
    }

    /// <summary>
    /// Adiciona um novo preço (tick) ao gerador para processamento.
    /// </summary>
    /// <param name="currentPrice">O preço do último negócio.</param>
    /// <param name="timestamp">A data e hora do negócio.</param>
    public void AddPrice(double currentPrice, SystemTime timestamp)
    {
        // Se for o primeiro tick (sem âncora e sem tijolos), define o preço âncora e aguarda o próximo tick.
        if (_anchorPrice == null && !_bricks.Any())
        {
            _anchorPrice = currentPrice;
            _anchorTimestamp = timestamp;
            _currentSwingHigh = currentPrice;
            _currentSwingLow = currentPrice;
            return;
        }

        // Atualiza os extremos do swing com o preço atual.
        _currentSwingHigh = Math.Max(_currentSwingHigh, currentPrice);
        _currentSwingLow = Math.Min(_currentSwingLow, currentPrice);

        int initialBrickCount = _bricks.Count;

        // Se ainda não houver tijolos, tenta formar o primeiro a partir do preço âncora.
        if (!_bricks.Any())
        {
            TryCreateFirstBrick(currentPrice, timestamp);
        }
        else // Se já existem tijolos, processa o movimento em relação ao último.
        {
            ProcessNextMove(currentPrice, timestamp);
        }

        // Se novos tijolos foram criados, reseta o swing a partir do fechamento do último.
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

        // Determina se o movimento foi suficiente para criar o primeiro tijolo (para cima ou para baixo).
        if (Math.Abs(movement) >= ThresholdRegularMove)
        {
            // O primeiro tijolo é sempre um tijolo regular.
            // Usa o timestamp do tick atual que fechou o tijolo, não o do âncora.
            AddBricksSeries(openPrice, movement, timestamp);

            // Após criar o primeiro tijolo, o preço âncora não é mais necessário.
            _anchorPrice = null;
            _anchorTimestamp = null;

            // A chamada para ProcessNextMove foi removida daqui.
            // AddBricksSeries já lida com a criação de todos os tijolos para o movimento atual.
            // O próximo tick será tratado em uma nova chamada a AddPrice.
        }
    }

    private void ProcessNextMove(double currentPrice, SystemTime timestamp)
    {
        var lastBrick = _bricks.Last();
        double movement = currentPrice - lastBrick.Close;

        // Lógica para continuação ou reversão baseada na direção do último tijolo.
        if (lastBrick.Direction == RenkoDirection.Up)
        {
            if (movement >= ThresholdRegularMove) // Continua para cima
            {
                AddBricksSeries(lastBrick.Close, movement, timestamp);
            }
            else if (movement <= -ThresholdReversalMove) // Reverte para baixo
            {
                AddReversalBrick(lastBrick.Close, movement, timestamp);
            }
        }
        else // lastBrick.Direction == RenkoDirection.Down
        {
            if (movement <= -ThresholdRegularMove) // Continua para baixo
            {
                AddBricksSeries(lastBrick.Close, movement, timestamp);
            }
            else if (movement >= ThresholdReversalMove) // Reverte para cima
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

        // Calcula quantos tijolos de continuação podem ser criados com o movimento total.
        int brickCount = (int)(absMovement / ThresholdRegularMove);

        if (brickCount == 0) return;

        // Captura o swing atual para todos os tijolos desta série.
        double swingHigh = _currentSwingHigh;
        double swingLow = _currentSwingLow;

        // O primeiro tijolo consome 'RegularBrickMove'. Os seguintes também consomem 'RegularBrickMove'.
        int continuationBrickCount = brickCount;
        
        double currentOpen = openPrice;
        for (int i = 0; i < continuationBrickCount; i++)
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

        // 1. Crie o tijolo de reversão.
        double reversalClose = openPrice + (direction == RenkoDirection.Up ? ReversalBrickBodySize : -ReversalBrickBodySize);
        CreateAndAddBrick(openPrice, reversalClose, direction, timestamp, _currentSwingHigh, _currentSwingLow);

        // Após a reversão, verifica se há movimento suficiente para tijolos de continuação.
        double remainingMovement = absMovement - ThresholdReversalMove;
        if (remainingMovement >= ThresholdRegularMove)
        {
            // A partir daqui, a lógica é a mesma de uma série de continuação.
            // O openPrice para a série de continuação é o fechamento do tijolo de reversão.
            AddBricksSeries(reversalClose, direction == RenkoDirection.Up ? remainingMovement : -remainingMovement, timestamp);
        }
    }
}
