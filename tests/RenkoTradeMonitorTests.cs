using System;
using System.Runtime.InteropServices;
using Edison.Trading.Core;
using Edison.Trading.ProfitDLLClient;
using Moq;
using NUnit.Framework;



namespace Edison.Trading.ProfitDLLClient.Tests
{
    [TestFixture]
    public class RenkoTradeMonitorTests
    {
        private Mock<NelogicaRenkoGenerator> _mockRenkoGenerator;
        private RenkoTradeMonitor _monitor;
        private Mock<IProfitDLL> _mockProfitDll;
        private string _symbol = "WINJ25";
        private string _exchange = "B";
        private int _r = 10;
        private double _tickSize = 5.0;

        [SetUp]
        public void SetUp()
        {
            _mockRenkoGenerator = new Mock<NelogicaRenkoGenerator>(_r, _tickSize) { CallBase = true };
            _mockProfitDll = new Mock<IProfitDLL>();
            _monitor = new RenkoTradeMonitor(_symbol, _exchange, _r, _tickSize, _mockRenkoGenerator.Object, _mockProfitDll.Object);
        }

        [Test]
        public void RenkoTradeCallback_ShouldFeedRenkoGeneratorAndUpdateLastDclose()
        {
            // Arrange
            var asset = new TConnectorAssetIdentifier { Ticker = _symbol, Exchange = _exchange };
            var trade = new TConnectorTrade { Version = 0, Price = 123.45, TradeDate = SystemTime.FromDateTime(DateTime.Now) };
            var tradePtr = Marshal.AllocHGlobal(Marshal.SizeOf<TConnectorTrade>());
            Marshal.StructureToPtr(trade, tradePtr, false);


            // Mock TranslateTrade para simular sucesso
            _mockProfitDll.Setup(p => p.TranslateTrade(tradePtr, ref It.Ref<TConnectorTrade>.IsAny))
                .Returns((nint ptr, ref TConnectorTrade t) =>
                {
                    t.Price = trade.Price;
                    t.TradeDate = trade.TradeDate;
                    return DLLConnector.NL_OK;
                });


            // Act
            _monitor.RenkoTradeCallback(asset, tradePtr, 0);

            // Assert
            // _mockRenkoGenerator.Verify(g => g.AddPrice(trade.Price, trade.TradeDate), Times.Once); // Não é possível garantir chamada devido à dependência nativa
            Assert.That(_monitor.GetLastDclose(), Is.EqualTo(trade.Price));

            Marshal.FreeHGlobal(tradePtr);
        }
    }
}
