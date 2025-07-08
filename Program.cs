using System;
using Edison.Trading.Core;
using System.Globalization;
using System.Linq;
using System.Text;
using Edison.Trading.ProfitDLLClient;

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

        NelogicaRenkoGenerator? renkoGen = null;
        RenkoTradeMonitor? monitor = null;

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
                        case "start renko":
                            if (monitor is not null)
                            {
                                Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Monitor Renko já está rodando.");
                                break;
                            }

                            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Código do ativo (ex WINFUT:B): ");
                            string? assetRaw = Console.ReadLine();
                            if (string.IsNullOrWhiteSpace(assetRaw) || !assetRaw.Contains(':'))
                            {
                                Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Formato de ativo inválido.");
                                break;
                            }
                            var parts = assetRaw.ToUpper().Split(':');
                            renkoGen = new NelogicaRenkoGenerator(15, 5.0);
                            renkoGen.ConfigureBuffer(200, "renko_rt.bin", 5);
                            renkoGen.StartPeriodicSave();
                            monitor = new RenkoTradeMonitor(parts[0], parts[1], 15, 5.0, renkoGen);
                            monitor.Start();
                            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("🔄 Monitor Renko iniciado. Use 'stop renko' para parar.");
                            break;
                        case "stop renko":
                            if (monitor is null || renkoGen is null)
                            {
                                Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("Monitor Renko não está ativo.");
                                break;
                            }
                            monitor.Stop();
                            renkoGen.StopPeriodicSaveAndFlush();
                            ExportRenkoCsv(renkoGen, "renko_final.csv", 200);
                            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("⏹ Monitor Renko parado e CSV salvo em renko_final.csv");
                            monitor = null;
                            renkoGen = null;
                            break;
                        case "exit":
                            terminate = true;
                            break;
                        default:
                            Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync("❓ Comando inválido.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Edison.Trading.ProfitDLLClient.DLLConnector.WriteSync($"⚠️ Erro: {ex.Message}");
            }
        }
        }

        private static void ExportRenkoCsv(NelogicaRenkoGenerator generator, string path, int limit)
        {
            var start = Math.Max(generator.Bricks.Count - limit, 0);
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("Date,Open,High,Low,Close");
            foreach (var brick in generator.Bricks.Skip(start))
            {
                var dt = SystemTime.ToDateTime(brick.Timestamp).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                writer.WriteLine($"{dt},{brick.Open},{brick.High},{brick.Low},{brick.Close}");
            }
        }
    }
}


