using System;
using System.Runtime.InteropServices;
using Edison.Trading.Core;

namespace Edison.Trading.ProfitDLLClient
{
    /// <summary>
    /// Monitora trades em tempo real e alimenta o NelogicaRenkoGenerator de forma thread-safe.
    /// </summary>
    public class RenkoTradeMonitor
    {
        private readonly NelogicaRenkoGenerator _renkoGenerator;
        private readonly IProfitDLL _profitDll;
        private readonly TConnectorTradeCallback _dedicatedTradeCallback;
        private readonly string _symbol;
        private readonly string _exchange;
        private readonly object _lock = new object();
        private double _lastDclose;

        public RenkoTradeMonitor(string symbol, string exchange, int r, double tickSize, NelogicaRenkoGenerator? renkoGenerator = null, IProfitDLL? profitDll = null)
        {
            _symbol = symbol;
            _exchange = exchange;
            _renkoGenerator = renkoGenerator ?? new NelogicaRenkoGenerator(r, tickSize);
            _profitDll = profitDll ?? new ProfitDLLWrapper();

            // Configuração do buffer e eventos (opcional)
            _renkoGenerator.ConfigureBuffer(200, $"renko_{symbol}_{exchange}.bin", 60);
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
            // Não há método para remover callback, mas pode-se sobrescrever se necessário
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
        /// Atualiza o RenkoGenerator de forma thread-safe (caso precise ser chamado externamente).
        /// </summary>
        public void UpdateRenko()
        {
            lock (_lock)
            {
                // _renkoGenerator.Update(); // Removido pois não existe mais
            }
        }
    }
}
