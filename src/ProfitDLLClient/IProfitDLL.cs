using System;
using Edison.Trading.Core;

namespace Edison.Trading.ProfitDLLClient
{
    public interface IProfitDLL
    {
        int TranslateTrade(nint a_pTrade, ref TConnectorTrade trade);
        // Adicione outros métodos da ProfitDLL conforme necessário
    }
}
