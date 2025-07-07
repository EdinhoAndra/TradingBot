using System;
using Edison.Trading.Core;

namespace Edison.Trading.ProfitDLLClient
{
    public class ProfitDLLWrapper : IProfitDLL
    {
        public int TranslateTrade(nint a_pTrade, ref TConnectorTrade trade)
        {
            return ProfitDLL.TranslateTrade(a_pTrade, ref trade);
        }
    }
}
