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
            _mockRenkoGenerator.Verify(g => g.AddPrice(trade.Price, trade.TradeDate), Times.Once);
            Assert.That(_monitor.GetLastDclose(), Is.EqualTo(trade.Price));

            Marshal.FreeHGlobal(tradePtr);
        }

        [Test]
        public void RenkoTradeCallback_Should_Survive_GarbageCollections()
        {
            var asset = new TConnectorAssetIdentifier { Ticker = _symbol, Exchange = _exchange };
            var trade = new TConnectorTrade { Version = 0 };
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<TConnectorTrade>());
            Marshal.StructureToPtr(trade, ptr, false);

            int counter = 0;
            _mockProfitDll.Setup(p => p.TranslateTrade(ptr, ref It.Ref<TConnectorTrade>.IsAny))
                .Returns((nint _, ref TConnectorTrade t) =>
                {
                    t.Price = counter;
                    t.TradeDate = SystemTime.FromDateTime(DateTime.UtcNow);
                    counter++;
                    return DLLConnector.NL_OK;
                });

            int iterations = 100;
            for (int i = 0; i < iterations; i++)
            {
                _monitor.RenkoTradeCallback(asset, ptr, 0);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            _mockRenkoGenerator.Verify(g => g.AddPrice(It.IsAny<double>(), It.IsAny<SystemTime>()), Times.Exactly(iterations));
            Assert.That(_monitor.GetLastDclose(), Is.EqualTo(iterations - 1));

            Marshal.FreeHGlobal(ptr);
        }

    }
}
