using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Edison.Trading.Api;

public static partial class ProfitDLL
{
    private const string DLL_NAME = "ProfitDLL.dll";

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int DLLInitializeLogin(string key, string user, string password, TStateCallback stateCallback, TAssetListCallback assetListCallback, TAssetListInfoCallback assetListInfoCallback, TAccountCallback accountCallback, TChangeStateTickerCallback changeStateTickerCallback, TNewDailyCallback newDailyCallback, TPriceBookCallback priceBookCallback, TTheoreticalPriceCallback theoreticalPriceCallback, TOfferBookCallback offerBookCallback, TAdjustHistoryCallbackV2 adjustHistoryCallbackV2, TNewTinyBookCallBack newTinyBookCallBack);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int DLLInitializeMarketLogin(string key, string user, string password, TStateCallback stateCallback, TAssetListCallback assetListCallback, TNewDailyCallback newDailyCallback, TPriceBookCallback priceBookCallback, TTheoreticalPriceCallback theoreticalPriceCallback, TOfferBookCallback offerBookCallback, TAdjustHistoryCallbackV2 adjustHistoryCallbackV2, TNewTinyBookCallBack newTinyBookCallBack);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetOrderDetails(ref TConnectorOrderOut order);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int EnumerateAllOrders(ref TConnectorAccountIdentifier accountId, nint a_Param, int a_nUnused, TEnumOrdersCallback a_pCallback);
    
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int EnumerateOrdersByInterval(ref TConnectorAccountIdentifier accountId, nint a_Param, SystemTime a_dtStart, SystemTime a_dtEnd, int a_nUnused, TEnumOrdersCallback a_pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void GetServerClock(ref double serverClock, ref int year, ref int month, ref int day, ref int hour, ref int min, ref int sec, ref int mili);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void FreePointer(IntPtr pRetorno, int pos);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetAgentNameLength(int agentId, int shortFlag);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetAgentName(int agentLength, int agentId, StringBuilder AgentName, int shortFlag);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int SubscribeTicker(string ticker, string bolsa);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int SubscribeOfferBook(string ticker, string bolsa);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int UnsubscribeTicker(string ticker, string bolsa);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetHistoryTrades(string ticker, string bolsa, string from, string to);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetOrder(string clOrdId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int GetPositionV2(ref TConnectorTradingAccountPosition position);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int SendZeroPositionV2(ref TConnectorZeroPosition zeroOrder);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int TranslateTrade(nint pTrade, ref TConnectorTrade trade);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetTradeCallbackV2(TTradeCallbackV2 pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetHistoryTradeCallbackV2(TTradeCallbackV2 pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetOrderCallback(TConnectorOrderCallback pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetOrderHistoryCallback(TConnectorAccountCallback pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetOfferBookCallbackV2(TOfferBookCallback pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetAssetListInfoCallbackV2(TAssetListInfoCallbackV2 pCallback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern void SetAdjustHistoryCallbackV2(TAdjustHistoryCallbackV2 pCallback);
}
