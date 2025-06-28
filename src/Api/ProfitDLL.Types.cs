using System;
using System.Runtime.InteropServices;

namespace Edison.Trading.Api;

#region Delegates
public delegate void TAssetListCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName);
public delegate void TAssetListInfoCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN);
public delegate void TAssetListInfoCallbackV2(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN, [MarshalAs(UnmanagedType.LPWStr)] string strSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSubSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSegmento);
public delegate void TStateCallback(int nConnStateType, int result);
public delegate void TNewDailyCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, double sOpen, double sHigh, double sLow, double sClose, double sVol, double sAjuste, double sMaxLimit, double sMinLimit, double sVolBuyer, double sVolSeller, int nQtd, int nNegocios, int nContratosOpen, int nQtdBuyer, int nQtdSeller, int nNegBuyer, int nNegSeller);
public delegate void TPriceBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, IntPtr pArraySell, IntPtr pArrayBuy);
public delegate void TOfferBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, [MarshalAs(UnmanagedType.LPWStr)] string date_str, IntPtr pArraySell, IntPtr pArrayBuy);
public delegate void TNewTinyBookCallBack(TAssetID assetId, double price, int qtd, int side);
public delegate void TAccountCallback(int nCorretora, [MarshalAs(UnmanagedType.LPWStr)] string CorretoraNomeCompleto, [MarshalAs(UnmanagedType.LPWStr)] string AccountID, [MarshalAs(UnmanagedType.LPWStr)] string NomeTitular);
public delegate void TChangeStateTickerCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strDate, int nState);
public delegate void TTheoreticalPriceCallback(TAssetID assetId, double dTheoreticalPrice, long nTheoreticalQtd);
public delegate void TAdjustHistoryCallbackV2(TAssetID assetId, double dValue, [MarshalAs(UnmanagedType.LPWStr)] string adjustType, [MarshalAs(UnmanagedType.LPWStr)] string strObserv, [MarshalAs(UnmanagedType.LPWStr)] string dtAjuste, [MarshalAs(UnmanagedType.LPWStr)] string dtDeliber, [MarshalAs(UnmanagedType.LPWStr)] string dtPagamento, int nFlags, double dMult);
public delegate void TConnectorOrderCallback(TConnectorOrderIdentifier orderId);
public delegate void TConnectorAccountCallback(TConnectorAccountIdentifier accountId);
public delegate bool TEnumOrdersCallback([In] in TConnectorOrder a_Order, nint a_Param);
public delegate void TTradeCallbackV2(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags);
#endregion

#region Structs
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TAssetID
{
    public string Ticker;
    public string Bolsa;
    public int Feed;
}

public struct TGroupPrice
{
    public double Price;
    public int Qtd;
    public int Count;

    public TGroupPrice(double price, int count, int qtd)
    {
        Qtd = qtd;
        Price = price;
        Count = count;
    }
}

public struct TConnectorOffer
{
    public double Price;
    public long Qtd;
    public int Agent;
    public long OfferID;
    public DateTime Date;

    public TConnectorOffer(double price, long qtd, int agent, long offerId, DateTime date)
    {
        Price = price;
        Qtd = qtd;
        Agent = agent;
        OfferID = offerId;
        Date = date;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorAssetIdentifier
{
    public int Version;
    public string Ticker;
    public int TickerLength;
    public string Exchange;
    public int ExchangeLength;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorOrderIdentifier
{
    public int Version;
    public string ClOrderID;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorAccountIdentifier
{
    public int Version;
    public int BrokerID;
    public string AccountID;
    public string SubAccountID;
}

[StructLayout(LayoutKind.Sequential)]
public struct TConnectorTrade
{
    public int Version;
    public long TradeNumber;
    public long TradeTime;
    public double Price;
    public long Quantity;
    public int TradeCondition;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorOrderOut
{
    public int Version;
    public TConnectorOrderIdentifier OrderID;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    public uint OrderSide;
    public double Price;
    public long Quantity;
    public long TradedQuantity;
    public long CanceledQuantity;
    public uint OrderStatus;
    public uint TimeInForce;
    public uint ExecType;
    public string TextMessage;
    public int TextMessageLength;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorOrder
{
    public int Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    public TConnectorOrderIdentifier OrderID;
    public uint OrderSide;
    public double Price;
    public long Quantity;
    public uint OrderType;
    public uint TimeInForce;
    public uint OrderStatus;
    public long TradedQuantity;
    public long CanceledQuantity;
    public SystemTime ServerTime;
    public SystemTime LocalTime;
    public string TextMessage;
}

public struct SystemTime
{
    public ushort Year;
    public ushort Month;
    public ushort DayOfWeek;
    public ushort Day;
    public ushort Hour;
    public ushort Minute;
    public ushort Second;
    public ushort Milliseconds;

    public static SystemTime FromDateTime(DateTime dt) => new()
    {
        Year = (ushort)dt.Year,
        Month = (ushort)dt.Month,
        Day = (ushort)dt.Day,
        Hour = (ushort)dt.Hour,
        Minute = (ushort)dt.Minute,
        Second = (ushort)dt.Second,
        Milliseconds = (ushort)dt.Millisecond
    };

    public static DateTime ToDateTime(SystemTime st) =>
        new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.Milliseconds);
}

public enum TConnectorPositionType : byte
{
    DayTrade = 1,
    Consolidated = 2
}

[Flags]
public enum OfferBookFlags : uint
{
    None = 0,
    TradingStarted = 1,
    TradingEnded = 2,
    TradingDelayed = 4,
    Auction = 8,
    AuctionEnding = 16,
    ReOpening = 32,
    ClosingCall = 64,
    Volatility = 128
}

[StructLayout(LayoutKind.Sequential)]
public struct TConnectorTradingAccountPosition
{
    public int Version;
    public TConnectorAssetIdentifier AssetID;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorPositionType PositionType;
    public long OpenQuantity;
    public double OpenAveragePrice;
    public int OpenSide;
    public double AverageTradedOpenPrice;
    public double AverageTradedClosePrice;
    public long DailyBuyQuantity;
    public long DailySellQuantity;
    public double DailyAverageBuyPrice;
    public double DailyAverageSellPrice;
    public long OpenQuantityClosedToday;
    public double CustomPrice;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorZeroPosition
{
    public int Version;
    public TConnectorAssetIdentifier AssetID;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorPositionType PositionType;
    public double Price;
    public string Password;
}

[Flags]
public enum TConnectorTradeCallbackFlags : uint
{
    None = 0,
    Last = 1,
    IsHistorical = 2
}

public enum NResult
{
    OK = 0,
    InvalidParameter = unchecked((int)0x80070057),
    OutOfMemory = unchecked((int)0x8007000E),
    NotImplemented = unchecked((int)0x80004001),
    Unexpected = unchecked((int)0x8000FFFF),
    AccessDenied = unchecked((int)0x80070005),
    NotInitialized = unchecked((int)0x80004005)
}
#endregion
