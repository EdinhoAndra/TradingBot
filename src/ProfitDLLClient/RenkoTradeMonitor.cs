using System;
using System.Runtime.InteropServices;
using Edison.Trading.Core;
using Edison.Trading.Indicators;

namespace Edison.Trading.ProfitDLLClient
{
    /// <summary>
    /// Monitora trades em tempo real e alimenta o NelogicaRenkoGenerator de forma thread-safe.
    /// </summary>
    public class RenkoTradeMonitor
    {
        private readonly NelogicaRenkoGenerator _renkoGenerator;
        private readonly RenkoBrickBuffer _brickBuffer;
        private readonly IProfitDLL _profitDll;
        private readonly TConnectorTradeCallback _dedicatedTradeCallback;
        private readonly string _symbol;
        private readonly string _exchange;
        private readonly object _lock = new object();
        private double _lastDclose;
        private string? _selectedAccount;

        public RenkoTradeMonitor(string symbol, string exchange, int r, double tickSize, NelogicaRenkoGenerator? renkoGenerator = null, IProfitDLL? profitDll = null)
        {
            _symbol = symbol;
            _exchange = exchange;
            _renkoGenerator = renkoGenerator ?? new NelogicaRenkoGenerator(r, tickSize);
            _profitDll = profitDll ?? new ProfitDLLWrapper();

            // Buffer persistente de tijolos
            _brickBuffer = new RenkoBrickBuffer(200, $"renko_{symbol}_{exchange}.bin");

            // A cada fechamento de tijolo o buffer é atualizado
            _renkoGenerator.OnCloseBrick += _brickBuffer.AddBrick;
            _renkoGenerator.OnCloseBrick += HandleNewBrick;

            // Callback dedicado para Renko
            _dedicatedTradeCallback = new TConnectorTradeCallback(RenkoTradeCallback);
        }

        /// <summary>
        /// Inicia o monitoramento do ativo e registra o callback dedicado.
        /// </summary>
        public void Start()
        {
            ProfitDLL.SubscribeTicker(_symbol, _exchange);
            ProfitDLL.SetTradeCallbackV2(_dedicatedTradeCallback);
        }

        /// <summary>
        /// Cancela a inscrição do ativo e remove o callback.
        /// </summary>
        public void Stop()
        {
            ProfitDLL.UnsubscribeTicker(_symbol, _exchange);
            // P/Invoke permite null, mas a assinatura não é anulável
            ProfitDLL.SetTradeCallbackV2(null!);
            _renkoGenerator.OnCloseBrick -= _brickBuffer.AddBrick;
            _renkoGenerator.OnCloseBrick -= HandleNewBrick;
        }

        /// <summary>
        /// Callback chamado pela ProfitDLL em cada trade. Thread-safe.
        /// </summary>
        // Refatorado para usar a mesma assinatura do TradeCallback do DLLConnector
        internal void RenkoTradeCallback(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags)
        {
            if (a_Asset.Ticker != _symbol || a_Asset.Exchange != _exchange)
                return;

            var trade = new TConnectorTrade { Version = 0 };
            if (_profitDll.TranslateTrade(a_pTrade, ref trade) == DLLConnector.NL_OK)
            {
                lock (_lock)
                {
                    _lastDclose = trade.Price;
                    _renkoGenerator.AddPrice(trade.Price, trade.TradeDate);
                }
            }
        }

        /// <summary>
        /// Manipulador chamado quando um novo tijolo Renko é fechado.
        /// </summary>
        private void HandleNewBrick(RenkoBrick brick)
        {
            // Exemplo: log, estratégia, etc.
            Console.WriteLine($"Novo tijolo: {brick.Direction} | {brick.Open} -> {brick.Close}");
        }

        /// <summary>
        /// Retorna o último preço dclose recebido de forma thread-safe.
        /// </summary>
        public double GetLastDclose()
        {
            lock (_lock)
            {
                return _lastDclose;
            }
        }

        /// <summary>
        /// Mantido apenas para compatibilidade e atualmente não executa nenhuma ação.
        /// </summary>
        public void UpdateRenko()
        {
            lock (_lock)
            {
                // _renkoGenerator.Update(); // Removido pois não existe mais
            }
        }

        /// <summary>
        /// Extrai as séries de tijolos presentes no buffer.
        /// </summary>
        public void ExtractSeries(out Memory<DateTime> timestamps, out Memory<double> open,
            out Memory<double> high, out Memory<double> low, out Memory<double> close)
        {
            _brickBuffer.ExtractSeries(out timestamps, out open, out high, out low, out close);
        }

        /// <summary>
        /// Permite ao usuário escolher a conta ativa via Console.
        /// </summary>
        public void SelectAccount()
        {
            var ids = ProfitDLL.ListAccounts();
            if (ids.Length == 0)
            {
                Console.WriteLine("Nenhuma conta disponível.");
                return;
            }
            for (int i = 0; i < ids.Length; i++)
            {
                Console.WriteLine($"{i}: {ids[i]}");
            }

            Console.Write("Selecione a conta (índice ou nome): ");
            string? input = Console.ReadLine();
            string? chosen = null;
            if (int.TryParse(input, out int idx) && idx >= 0 && idx < ids.Length)
            {
                chosen = ids[idx];
            }
            else
            {
                foreach (var id in ids)
                {
                    if (string.Equals(id, input, StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = id;
                        break;
                    }
                }
            }

            if (chosen == null)
            {
                Console.WriteLine("Conta inválida.");
                return;
            }

            ProfitDLL.SetActiveAccount(chosen);
            _selectedAccount = chosen;
            Console.WriteLine($"Conta ativa: {chosen}");
        }
    }
}
