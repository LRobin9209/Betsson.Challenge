using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using FluentAssertions;
using Moq;

namespace Betsson.OnlineWallets.UnitTests
{
    public class OnlineWalletServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepository;
        private readonly OnlineWalletService _service;

        public OnlineWalletServiceTests()
        {
            _mockRepository = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnBalance()
        {
            // Arrange
            var onlineWalletEntry = new OnlineWalletEntry { BalanceBefore = 100, Amount = 50 };
            _mockRepository.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(onlineWalletEntry);

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            result.Amount.Should().Be(150);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnCorrectBalance_WhenTransactionExists()
        {
            // Arrange
            var lastEntry = new OnlineWalletEntry
            {
                Amount = 50,
                BalanceBefore = 100
            };

            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);

            // Act
            Balance result = await _service.GetBalanceAsync();

            // Assert
            result.Amount.Should().Be(150);
            _mockRepository.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        }

    }
}
