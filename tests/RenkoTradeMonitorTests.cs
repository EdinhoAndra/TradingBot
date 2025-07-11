using System;
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
        private Mock<IProfitDLL> _mockProfitDll;
        private RenkoTradeMonitor _monitor;

        private readonly string _symbol = "WINJ25";
        private readonly string _exchange = "B";
        private readonly int _r = 10;
        private readonly double _tickSize = 5.0;

        [SetUp]
        public void SetUp()
        {
            _mockRenkoGenerator = new Mock<NelogicaRenkoGenerator>(_r, _tickSize) { CallBase = true };
            _mockProfitDll = new Mock<IProfitDLL>();

            _monitor = new RenkoTradeMonitor(
                _symbol,
                _exchange,
                _r,
                _tickSize,
                _mockRenkoGenerator.Object,
                _mockProfitDll.Object);
        }

        
    }
}
