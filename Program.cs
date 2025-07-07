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

namespace Edison.Trading.Program
{
    public class Program
    {
        public static void Main(string[] args)
        {

        Console.Write("Chave de ativação: ");
        string? key = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(key))
        {
            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Chave de ativação não informada.");
            return;
        }

        Console.Write("Usuário: ");
        string? userInput = Console.ReadLine();
        string user = !string.IsNullOrWhiteSpace(userInput) ? userInput : string.Empty;
        if (string.IsNullOrEmpty(user))
        {
            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Usuário não informado.");
            return;
        }

        string password = Edison.Trading.ProfitDLLClient.DLLConnector.ReadPassword();

        if (Edison.Trading.ProfitDLLClient.DLLConnector.StartDLL(key, user, password) != Edison.Trading.ProfitDLLClient.DLLConnector.NL_OK)
        {
            return;
        }

        var terminate = false;
        while (!terminate)
        {
            try
            {
                if (Edison.Trading.ProfitDLLClient.DLLConnector.bMarketConnected && Edison.Trading.ProfitDLLClient.DLLConnector.bAtivo)
                {
                    Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Comando: ");

                    string? inputRaw = Console.ReadLine();
                    string input = inputRaw ?? string.Empty;
                    switch (input)
                    {
                        case "subscribe":
                            Edison.Trading.ProfitDLLClient.DLLConnector.SubscribeAsset();
                            break;
                        case "unsubscribe":
                            Edison.Trading.ProfitDLLClient.DLLConnector.UnsubscribeAsset();
                            break;
                        case "offerbook":
                            Edison.Trading.ProfitDLLClient.DLLConnector.DoSubscribeOfferBook();
                            break;
                        case "request history":
                            Edison.Trading.ProfitDLLClient.DLLConnector.RequestHistory();
                            break;
                        case "request order":
                            Edison.Trading.ProfitDLLClient.DLLConnector.RequestOrder();
                            break;
                        case "get position":
                            Edison.Trading.ProfitDLLClient.DLLConnector.DoGetPosition();
                            break;
                        case "zero position":
                            Edison.Trading.ProfitDLLClient.DLLConnector.DoZeroPosition();
                            break;
                        case "get orders":
                            Edison.Trading.ProfitDLLClient.DLLConnector.DoGetOrders();
                            break;
                        case "get agent name":
                            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Nome do agente: " + Edison.Trading.ProfitDLLClient.DLLConnector.DoGetAgentName());
                            break;

                        case "exit":
                            terminate = true;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync(ex.Message);
            }
        }

        }
    }
}


