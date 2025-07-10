using System;
using Edison.Trading.Core;
using System.Globalization;
using System.Linq;
using System.Text;
using Edison.Trading.ProfitDLLClient;
using System.Threading;

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
                ProfitDLLClient.DLLConnector.WriteSync("Chave de ativação não informada.");
                return;
            }

            Console.Write("Usuário: ");
            string? userInput = Console.ReadLine();
            string user = !string.IsNullOrWhiteSpace(userInput) ? userInput : string.Empty;
            if (string.IsNullOrEmpty(user))
            {
                ProfitDLLClient.DLLConnector.WriteSync("Usuário não informado.");
                return;
            }

            string password = ProfitDLLClient.DLLConnector.ReadPassword();

            if (ProfitDLLClient.DLLConnector.StartDLL(key, user, password) != ProfitDLLClient.DLLConnector.NL_OK)
            {
                return;
            }

            NelogicaRenkoGenerator? renkoGen = null;
            RenkoTradeMonitor? monitor = null;

            ProfitDLLClient.DLLConnector.WriteSync("✅ Sistema iniciado. Digite comandos ou 'exit' para sair.");

            bool terminate = false;
            while (!terminate)
            {
                if (!ProfitDLLClient.DLLConnector.bMarketConnected)
                {
                    ProfitDLLClient.DLLConnector.WriteSync("⚠️ Market ainda não conectado. Comandos podem falhar.");
                }

                ProfitDLLClient.DLLConnector.WriteSync("Comando: ");
                string? inputRaw = Console.ReadLine();
                string input = inputRaw ?? string.Empty;

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    terminate = true;
                    break;
                }

                try
                {
                    HandleCommand(input, ref renkoGen, ref monitor);
                }
                catch (Exception ex)
                {
                    ProfitDLLClient.DLLConnector.WriteSync($"⚠️ Erro: {ex.Message}");
                }

                Thread.Sleep(100); // Pequeno delay para evitar CPU 100%
            }
        }

        private static void HandleCommand(string input, ref NelogicaRenkoGenerator? renkoGen, ref RenkoTradeMonitor? monitor)
        {
            switch (input.ToLowerInvariant())
            {
                case "subscribe":
                    ProfitDLLClient.DLLConnector.SubscribeAsset();
                    break;
                case "unsubscribe":
                    ProfitDLLClient.DLLConnector.UnsubscribeAsset();
                    break;
                case "offerbook":
                    ProfitDLLClient.DLLConnector.DoSubscribeOfferBook();
                    break;
                case "request history":
                    ProfitDLLClient.DLLConnector.RequestHistory();
                    break;
                case "request order":
                    ProfitDLLClient.DLLConnector.RequestOrder();
                    break;
                case "get position":
                    ProfitDLLClient.DLLConnector.DoGetPosition();
                    break;
                case "zero position":
                    ProfitDLLClient.DLLConnector.DoZeroPosition();
                    break;
                case "get orders":
                    ProfitDLLClient.DLLConnector.DoGetOrders();
                    break;
                case "get agent name":
                    ProfitDLLClient.DLLConnector.WriteSync("Nome do agente: " + ProfitDLLClient.DLLConnector.DoGetAgentName());
                    break;
                case "select account":
                    ProfitDLLClient.DLLConnector.ListAccountsInteractive();
                    break;
                case "start renko":
                    if (monitor is not null)
                    {
                        ProfitDLLClient.DLLConnector.WriteSync("Monitor Renko já está rodando.");
                        return;
                    }
                    ProfitDLLClient.DLLConnector.WriteSync("Código do ativo (ex WINFUT:B): ");
                    string? assetRaw = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(assetRaw) || !assetRaw.Contains(':'))
                    {
                        ProfitDLLClient.DLLConnector.WriteSync("Formato de ativo inválido.");
                        return;
                    }
                    var parts = assetRaw.ToUpper().Split(':');
                    renkoGen = new NelogicaRenkoGenerator(15, 5.0);
                    
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "renko_rt.bin");
                    renkoGen.ConfigureBuffer(200, filePath);
                    //renkoGen.ConfigureBuffer(200, "renko_rt.bin");

                    double lastClose;
                    RenkoDirection direction;
                    if (renkoGen.TryLoadLastBrickFromDisk(out var lastBrick) && lastBrick is not null)
                    {
                        lastClose = lastBrick.Close;
                        direction = lastBrick.Direction;
                        ProfitDLLClient.DLLConnector.WriteSync($"Último fechamento salvo: {lastClose}");
                    }
                    else
                    {
                        ProfitDLLClient.DLLConnector.WriteSync("Nenhum arquivo de renko salvo encontrado. Buscando dClose do servidor...");
                        double dClose = 0;
                        int ret = ProfitDLL.GetLastDailyClose(parts[0], parts[1], ref dClose, 0);
                        if (ret != ProfitDLLClient.DLLConnector.NL_OK)
                        {
                            ProfitDLLClient.DLLConnector.WriteSync($"Erro ao obter dClose: {ret}");
                        }
                        lastClose = dClose;
                        ProfitDLLClient.DLLConnector.WriteSync($"Último preço de fechamento: {lastClose}");
                        ProfitDLLClient.DLLConnector.WriteSync("Informe a direção do último tijolo (up/down): ");
                        string? dirInput = Console.ReadLine();
                        direction = string.Equals(dirInput, "down", StringComparison.OrdinalIgnoreCase) ? RenkoDirection.Down : RenkoDirection.Up;
                    }

                    renkoGen.InitializeFromLastBrick(lastClose, direction);
                    monitor = new RenkoTradeMonitor(parts[0], parts[1], 15, 5.0, renkoGen);

                    monitor.SelectAccount();
                    monitor.Start();
                    ProfitDLLClient.DLLConnector.WriteSync("🔄 Monitor Renko iniciado. Use 'stop renko' para parar.");
                    break;
                case "stop renko":
                    if (monitor is null || renkoGen is null)
                    {
                        ProfitDLLClient.DLLConnector.WriteSync("Monitor Renko não está ativo.");
                        return;
                    }
                    monitor.Stop();
                    renkoGen.PersistBuffer();
                    ExportRenkoCsv(renkoGen, "renko_final.csv", 200);
                    ProfitDLLClient.DLLConnector.WriteSync("⏹ Monitor Renko parado e CSV salvo em renko_final.csv");
                    monitor = null;
                    renkoGen = null;
                    break;
                default:
                    ProfitDLLClient.DLLConnector.WriteSync("❓ Comando inválido.");
                    break;
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
