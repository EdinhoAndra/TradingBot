// ...existing code...
using System;
using Edison.Trading.Core;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

using static Edison.Trading.Core.ProfitDLL;

namespace Edison.Trading.ProfitDLLClient
{
public partial class DLLConnector
{
    // Auxiliar: Escreve o resultado de uma operação
    public static void WriteResult(int retVal)
    {
        if (retVal == NL_OK)
        {
            WriteSync("Operação realizada com sucesso");
        }
        else
        {
            WriteSync($"Erro na operação: {retVal}");
        }
    }

    // Auxiliar: Lê o identificador de ativo
    public static TConnectorAssetIdentifier ReadAssetID()
    {
        string? input;
        Match match = Match.Empty;
        do
        {
            Console.Write("Código do ativo (ex PETR4:B): ");
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            match = Regex.Match(input.ToUpper(), "([^:]+):([A-Za-z0-9])");
        } while (string.IsNullOrWhiteSpace(input) || !match.Success);

        return new TConnectorAssetIdentifier()
        {
            Version = 0,
            Ticker = match.Groups[1].Value,
            Exchange = match.Groups[2].Value
        };
    }

    // Auxiliar: Lê o identificador de conta
    public static TConnectorAccountIdentifier ReadAccountId()
    {
        string? input;
        do
        {
            Console.Write("Código da conta (ex 1171:12345:1): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input, @"\d+:\d+(:\d+)?"));

        var numbers = input.Split(':');

        var retVal = new TConnectorAccountIdentifier()
        {
            Version = 0,
            BrokerID = int.Parse(numbers[0]),
            AccountID = numbers[1],
            SubAccountID = ""
        };

        if (numbers.Length == 3)
        {
            retVal.SubAccountID = numbers[2];
        }

        return retVal;
    }
    // --- Implementações reais dos métodos chamados em Program.cs ---
    public static string DoGetAgentName()
    {
        WriteSync("Informe o ID do agente: ");
        string? agentInputRaw = Console.ReadLine();
        string agentInput = agentInputRaw ?? string.Empty;

        WriteSync("Informe a Flag: (0 - Normal, 1 - Abreviado): ");
        string? flagInputRaw = Console.ReadLine();
        string flagInput = flagInputRaw ?? string.Empty;

        if (!Int32.TryParse(agentInput, out int agentId))
        {
            WriteSync("ID do agente inválido.");
            return "Erro";
        }

        if (!Int32.TryParse(flagInput, out int shortFlag))
        {
            WriteSync("Flag inválida.");
            return "Erro";
        }

        int agentLength = ProfitDLL.GetAgentNameLength(agentId, shortFlag);
        StringBuilder AgentName  = new StringBuilder(agentLength);
        int retVal = ProfitDLL.GetAgentName(agentLength, agentId, AgentName , shortFlag);
        if (retVal == NL_OK)
        {
            string result = AgentName.ToString();
            WriteSync("Resultado: " + result);
            return result;
        }
        else
        {
            WriteSync($"Erro no GetAgentName: {retVal}");
            return "Erro";
        }
    }

    static string strAssetListFilter = "";

    public static void SubscribeAsset()
    {
        string? input;
        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.ToUpper(), "[^:]+:[A-Za-z0-9]"));

        var split = input.ToUpper().Split(':');
        if (split.Length < 2)
        {
            WriteSync("Formato de ativo inválido.");
            return;
        }

        var retVal = SubscribeTicker(split[0], split[1]);
        if (retVal == NL_OK)
        {
            WriteSync("Subscribe com sucesso");
        }
        else
        {
            WriteSync($"Erro no subscribe: {retVal}");
        }
    }

    public static void DoSubscribeOfferBook()
    {
        string? input;
        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.ToUpper(), "[^:]+:[A-Za-z0-9]"));

        var split = input.ToUpper().Split(':');
        if (split.Length < 2)
        {
            WriteSync("Formato de ativo inválido.");
            return;
        }

        var retVal = ProfitDLL.SubscribeOfferBook(split[0], split[1]);
        WriteResult(retVal);
    }

    public static void UnsubscribeAsset()
    {
        string? input;
        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.ToUpper(), "[^:]+:[A-Za-z0-9]"));

        var split = input.ToUpper().Split(':');
        if (split.Length < 2)
        {
            WriteSync("Formato de ativo inválido.");
            return;
        }

        var retVal = UnsubscribeTicker(split[0], split[1]);
        if (retVal == NL_OK)
        {
            WriteSync("Unsubscribe com sucesso");
        }
        else
        {
            WriteSync($"Erro no unsubscribe: {retVal}");
        }
    }

    public static void RequestHistory()
    {
        string? input;
        do
        {
            Console.Write("Insira o codigo do ativo e clique enter (ex. PETR4:B): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.ToUpper(), "[^:]+:[A-Za-z0-9]"));

        var split = input.ToUpper().Split(':');
        if (split.Length < 2)
        {
            WriteSync("Formato de ativo inválido.");
            return;
        }

        var retVal = GetHistoryTrades(split[0], split[1], DateTime.Today.ToString(dateFormat), DateTime.Now.ToString(dateFormat));
        if (retVal == NL_OK)
        {
            WriteSync("GetHistoryTrades com sucesso");
        }
        else
        {
            WriteSync($"Erro no GetHistoryTrades: {retVal}");
        }
    }

    public static void RequestOrder()
    {
        WriteSync("Informe um ClOrdId: ");
        string? clOrdId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(clOrdId))
        {
            WriteSync("ClOrdId não informado.");
            return;
        }
        var retVal = ProfitDLL.GetOrder(clOrdId);
        if (retVal == NL_OK)
        {
            WriteSync("GetOrder com sucesso");
        }
        else
        {
            WriteSync($"Erro no GetOrder: {retVal}");
        }
    }

    public static void DoGetPosition()
    {
        var assetId = ReadAssetID();
        var accountId = ReadAccountId();
        string? input;
        do
        {
            Console.Write("Tipo da posição (1 - day trade, 2 - consolidado): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || (input != "1" && input != "2"));

        var positionType = (TConnectorPositionType)byte.Parse(input!);
        var position = new TConnectorTradingAccountPosition()
        {
            Version = 1,
            AssetID = assetId,
            AccountID = accountId,
            PositionType = positionType
        };
        var retVal = ProfitDLL.GetPositionV2(ref position);
        if (retVal == NL_OK)
        {
            WriteSync($"{position.OpenSide} | {position.OpenAveragePrice} | {position.OpenQuantity}");
            WriteSync($"{position.DailyAverageBuyPrice} | {position.DailyAverageSellPrice} | {position.DailyBuyQuantity} | {position.DailySellQuantity}");
        }
        else
        {
            WriteSync($"Erro no GetPositionV2: {retVal}");
        }
    }

    public static void DoZeroPosition()
    {
        string? input;
        do
        {
            Console.Write("Código do ativo (ex PETR4:B): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input.ToUpper(), "[^:]+:[A-Za-z0-9]"));

        var assetId = new TConnectorAssetIdentifier()
        {
            Version = 0,
            Ticker = input!.ToUpper()[..input.IndexOf(':')],
            Exchange = input.ToUpper()[(input.IndexOf(':') + 1)..]
        };

        do
        {
            Console.Write("Código da conta (ex 1171:12345:1): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || !Regex.IsMatch(input, @"\d+:\d+(:\d+)?"));

        var numbers = input.Split(':');
        var accountId = new TConnectorAccountIdentifier()
        {
            Version = 0,
            BrokerID = int.Parse(numbers[0]),
            AccountID = numbers[1],
            SubAccountID = ""
        };
        if (numbers.Length == 3)
        {
            accountId.SubAccountID = numbers[2];
        }

        do
        {
            Console.Write("Tipo da posição (1 - day trade, 2 - consolidado): ");
            input = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(input) || (input != "1" && input != "2"));

        var positionType = (TConnectorPositionType)byte.Parse(input!);
        var zeroOrder = new TConnectorZeroPosition()
        {
            Version = 1,
            AssetID = assetId,
            AccountID = accountId,
            PositionType = positionType,
            Password = ReadPassword(),
            Price = -1
        };
        var retVal = ProfitDLL.SendZeroPositionV2(ref zeroOrder);
        if (retVal == NL_OK)
        {
            WriteSync($"Sucesso no SendZeroPositionV2: {retVal}");
        }
        else
        {
            WriteSync($"Erro no SendZeroPositionV2: {retVal}");
        }
    }

    public static void DoGetOrders()
    {
        var count = 0;
        bool EnumOrders([In] in TConnectorOrder a_Order, nint a_Param)
        {
            WriteSync($"{nameof(EnumOrders)}: {a_Order}");
            if (a_Order.Quantity == 100) { count++; }
            return true;
        }
        var accountId = ReadAccountId();
        var ret = ProfitDLL.EnumerateOrdersByInterval(ref accountId, 0, SystemTime.FromDateTime(DateTime.Now.AddHours(-1)), SystemTime.FromDateTime(DateTime.Now.AddMinutes(-1)), 0, EnumOrders);
        if (ret != NL_OK) { WriteSync($"{nameof(ProfitDLL.EnumerateOrdersByInterval)}: {(NResult)ret}"); }
        WriteSync($"{nameof(ProfitDLL.EnumerateOrdersByInterval)}: Orders with 100 quantity: {count}");
    }

    public static int StartDLL(string key, string user, string password)
    {
        int retVal;
        bool bRoteamento = true;
        static void EmptyHistoryCallback(TAssetID AssetID, int nCorretora, int nQtd, int nTradedQtd, int nLeavesQtd, int Side, double sPrice, double sStopPrice, double sAvgPrice, long nProfitID,
            string TipoOrdem, string Conta, string Titular, string ClOrdID, string Status, string Date) { }
        static void EmptyOrderChangeCallback(TAssetID assetId, int nCorretora, int nQtd, int nTradedQtd, int nLeavesQtd, int Side, double sPrice, double sStopPrice, double sAvgPrice, long nProfitID,
            string TipoOrdem, string Conta, string Titular, string ClOrdID, string Status, string Date, string TextMessage) { }
        static void EmptyTradeCallback(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit) { }
        static void EmptyOfferBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, string date, IntPtr pArraySell, IntPtr pArrayBuy) { }
        static void EmptyHistoryTradeCallback(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType) { }
        static void EmptyProgressCallback(TAssetID assetId, int nProgress) { }
        if (bRoteamento)
        {
            retVal = ProfitDLL.DLLInitializeLogin(
                key, user, password, _stateCallback,
                new Edison.Trading.Core.THistoryCallBack(EmptyHistoryCallback),
                new Edison.Trading.Core.TOrderChangeCallBack(EmptyOrderChangeCallback),
                _accountCallback,
                new Edison.Trading.Core.TTradeCallback(EmptyTradeCallback),
                _newDailyCallback,
                _priceBookCallback,
                new Edison.Trading.Core.TOfferBookCallback(EmptyOfferBookCallback),
                new Edison.Trading.Core.THistoryTradeCallback(EmptyHistoryTradeCallback),
                new Edison.Trading.Core.TProgressCallBack(EmptyProgressCallback),
                _newTinyBookCallBack
            );
        }
        else
        {
            retVal = ProfitDLL.DLLInitializeMarketLogin(
                key, user, password, _stateCallback,
                new Edison.Trading.Core.TTradeCallback(EmptyTradeCallback),
                _newDailyCallback,
                _priceBookCallback,
                new Edison.Trading.Core.TOfferBookCallback(EmptyOfferBookCallback),
                new Edison.Trading.Core.THistoryTradeCallback(EmptyHistoryTradeCallback),
                new Edison.Trading.Core.TProgressCallBack(EmptyProgressCallback),
                _newTinyBookCallBack
            );
        }
        if (retVal != NL_OK)
        {
            WriteSync($"Erro na inicialização: {retVal}");
        }
        else
        {
            SetTradeCallbackV2(_TradeCallback);
            SetHistoryTradeCallbackV2(_HistoryTradeCallback);
            ProfitDLL.SetOrderCallback(_orderCallback);
            ProfitDLL.SetOrderHistoryCallback(_orderHistoryCallback);
            ProfitDLL.SetOfferBookCallbackV2(_offerBookCallbackV2);
            ProfitDLL.SetAssetListInfoCallbackV2(_assetListInfoCallbackV2);
            ProfitDLL.SetAdjustHistoryCallbackV2(_adjustHistoryCallbackV2);
        }
        return retVal;
    }
        //////////////////////////////////////////////////////////////////////////////
        // Error Codes
        public const int NL_OK = 0x00000000;  // OK

        private static readonly object writeLock = new object();

        public static string ReadPassword()
        {
            Console.Write("Senha: ");

            var retVal = "";
            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);
                var key = keyInfo.Key;

                if (key == ConsoleKey.Enter)
                {
                    break;
                }

                if (key == ConsoleKey.Backspace && retVal.Length > 0)
                {
                    retVal = retVal[..^1];

                    var (left, top) = Console.GetCursorPosition();
                    Console.SetCursorPosition(left - 1, top);

                    Console.Write(" ");
                    Console.SetCursorPosition(left - 1, top);
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    retVal += keyInfo.KeyChar;
                }
            }

            Console.WriteLine();

            return retVal;
        }

        // --- Métodos utilitários públicos ---
        public static void WriteSync(string text)
        {
            lock (writeLock)
            {
                Console.WriteLine(text);
            }
        }

        // --- Stubs públicos para todos os callbacks referenciados ---
        public static void AssetListCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName) { }
        public static void AssetListInfoCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN) { }
        public static void AssetListInfoCallbackV2(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN, [MarshalAs(UnmanagedType.LPWStr)] string strSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSubSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSegmento) { }
        public static void StateCallback(int nConnStateType, int result) { }
        public static void NewDailyCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, double sOpen, double sHigh, double sLow, double sClose, double sVol, double sAjuste, double sMaxLimit, double sMinLimit, double sVolBuyer, double sVolSeller, int nQtd, int nNegocios, int nContratosOpen, int nQtdBuyer, int nQtdSeller, int nNegBuyer, int nNegSeller) { }
        public static void PriceBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, IntPtr pArraySell, IntPtr pArrayBuy) { }
        public static void OfferBookCallbackV2(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, [MarshalAs(UnmanagedType.LPWStr)] string date_str, IntPtr pArraySell, IntPtr pArrayBuy) { }
        public static void NewTinyBookCallBack(TAssetID assetId, double price, int qtd, int side) { }
        public static void AccountCallback(int nCorretora, [MarshalAs(UnmanagedType.LPWStr)] string CorretoraNomeCompleto, [MarshalAs(UnmanagedType.LPWStr)] string AccountID, [MarshalAs(UnmanagedType.LPWStr)] string NomeTitular) { }
        public static void ChangeStateTickerCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strDate, int nState) { }
        public static void TheoreticalPriceCallback(TAssetID assetId, double dTheoreticalPrice, Int64 nTheoreticalQtd) { }
        public static void AdjustHistoryCallbackV2(TAssetID assetId, double dValue, [MarshalAs(UnmanagedType.LPWStr)] string adjustType, [MarshalAs(UnmanagedType.LPWStr)] string strObserv, [MarshalAs(UnmanagedType.LPWStr)] string dtAjuste, [MarshalAs(UnmanagedType.LPWStr)] string dtDeliber, [MarshalAs(UnmanagedType.LPWStr)] string dtPagamento, int nFlags, double dMult) { }
        public static void OrderCallback(TConnectorOrderIdentifier orderId) { }
        public static void OrderHistoryCallback(TConnectorAccountIdentifier accountId) { }
        public static void TradeCallback(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags) { }
        public static void HistoryTradeCallback(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags) { }

        #region obj garbage KeepAlive
        public static TAssetListCallback _assetListCallback = new TAssetListCallback(AssetListCallback);
        public static TAssetListInfoCallback _assetListInfoCallback = new TAssetListInfoCallback(AssetListInfoCallback);
        public static TAssetListInfoCallbackV2 _assetListInfoCallbackV2 = new TAssetListInfoCallbackV2(AssetListInfoCallbackV2);
        public static TStateCallback _stateCallback = new TStateCallback(StateCallback);
        public static TNewDailyCallback _newDailyCallback = new TNewDailyCallback(NewDailyCallback);
        public static TPriceBookCallback _priceBookCallback = new TPriceBookCallback(PriceBookCallback);
        public static TOfferBookCallback _offerBookCallbackV2 = new TOfferBookCallback(OfferBookCallbackV2);
        public static TNewTinyBookCallBack _newTinyBookCallBack = new TNewTinyBookCallBack(NewTinyBookCallBack);
        public static TAccountCallback _accountCallback = new TAccountCallback(AccountCallback);
        public static TChangeStateTickerCallback _changeStateTickerCallback = new TChangeStateTickerCallback(ChangeStateTickerCallback);
        public static TTheoreticalPriceCallback _theoreticalPriceCallback = new TTheoreticalPriceCallback(TheoreticalPriceCallback);
        public static TAdjustHistoryCallbackV2 _adjustHistoryCallbackV2 = new TAdjustHistoryCallbackV2(AdjustHistoryCallbackV2);
        public static TConnectorOrderCallback _orderCallback = new TConnectorOrderCallback(OrderCallback);
        public static TConnectorAccountCallback _orderHistoryCallback = new TConnectorAccountCallback(OrderHistoryCallback);
        public static TConnectorTradeCallback _TradeCallback = new TConnectorTradeCallback(DLLConnector.TradeCallback);
        public static TConnectorTradeCallback _HistoryTradeCallback = new TConnectorTradeCallback(DLLConnector.HistoryTradeCallback);
        #endregion

        #region variables
        public static Queue<Trade> Traders = new Queue<Trade>();
        private static readonly object TradeLock = new object();
        public static Queue<Trade> HistTraders = new Queue<Trade>();
        private static readonly object HistLock = new object();

        public static List<TGroupPrice> m_lstPriceSell = new List<TGroupPrice>();
        public static List<TGroupPrice> m_lstPriceBuy = new List<TGroupPrice>();

        public static List<TConnectorOffer> m_lstOfferSell = new List<TConnectorOffer>();
        public static List<TConnectorOffer> m_lstOfferBuy = new List<TConnectorOffer>();

        public static bool bAtivo = false;
        public static bool bMarketConnected = false;

        static readonly CultureInfo provider = CultureInfo.InvariantCulture;
        #endregion

        #region consts
        private const string dateFormat = "dd/MM/yyyy HH:mm:ss.fff";
        #endregion

        #region Client Functions
        // ...existing code...
        // (todo o restante do conteúdo do arquivo já está dentro da classe)
        // ...existing code...
        #endregion
    }
}

