using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
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

        [Fact]
        public async Task DepositFundsAsync_ShouldIncreaseBalance_WhenDepositIsMade()
        {
            // Arrange
            var deposit = new Deposit { Amount = 100 };
            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { Amount = 200, BalanceBefore = 300 });

            _mockRepository
                .Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            Balance result = await _service.DepositFundsAsync(deposit);

            // Assert
            result.Amount.Should().Be(600);
            _mockRepository.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
        }

        [Fact]
        public async Task DepositFundsAsync_ShouldNotChangeBalance_WhenDepositIsZero()
        {
            // Arrange
            var deposit = new Deposit { Amount = 0 };
            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { Amount = 100, BalanceBefore = 200 });

            _mockRepository
                .Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            Balance result = await _service.DepositFundsAsync(deposit);

            // Assert
            result.Amount.Should().Be(300);
            _mockRepository.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
        }

        [Fact]
        public async Task WithdrawFundsAsync_ShouldDecreaseBalance_WhenWithdrawalIsValid()
        {
            // Arrange
            var withdrawal = new Withdrawal { Amount = 50 };
            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { Amount = 200, BalanceBefore = 300 });

            _mockRepository
                .Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            Balance result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            result.Amount.Should().Be(450);
            _mockRepository.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
        }

        [Fact]
        public async Task WithdrawFundsAsync_ShouldThrowException_WhenInsufficientBalance()
        {
            // Arrange
            var withdrawal = new Withdrawal { Amount = 500 };
            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { Amount = 100, BalanceBefore = 200 });

            // Act
            Func<Task> act = async () => await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            await act.Should().ThrowAsync<InsufficientBalanceException>();
            _mockRepository.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
        }

        [Fact]
        public async Task WithdrawFundsAsync_ShouldNotChangeBalance_WhenWithdrawalIsZero()
        {
            // Arrange
            var withdrawal = new Withdrawal { Amount = 0 };
            _mockRepository
                .Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { Amount = 200, BalanceBefore = 300 });

            _mockRepository
                .Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            Balance result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            result.Amount.Should().Be(500);
            _mockRepository.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
        }

    }
}
