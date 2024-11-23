using AutoMapper;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Web;
using Betsson.OnlineWallets.Web.Controllers;
using Betsson.OnlineWallets.Web.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace Betsson.OnlineWalletsController.ApiTests
{
    public class OnlineWalletControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OnlineWalletControllerTests()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var repositoryMock = new Mock<IOnlineWalletRepository>();

                        repositoryMock.Setup(r => r.GetLastOnlineWalletEntryAsync())
                            .ReturnsAsync(new OnlineWalletEntry
                            {
                                BalanceBefore = 100,
                                Amount = 50
                            });

                        repositoryMock.Setup(r => r.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                            .Returns(Task.CompletedTask);

                        services.AddSingleton(repositoryMock.Object);

                        
                        var mapperConfig = new MapperConfiguration(cfg =>
                        {
                            cfg.CreateMap<Balance, BalanceResponse>();
                            cfg.CreateMap<DepositRequest, Deposit>();
                            cfg.CreateMap<WithdrawalRequest, Withdrawal>();
                        });
                        services.AddSingleton(mapperConfig.CreateMapper());
                    });
                });
        }

        [Fact]
        public async Task Balance_Should_Return_Correct_Amount()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/OnlineWallet/Balance");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();

            // Assert
            balanceResponse.Should().NotBeNull();
            balanceResponse.Amount.Should().Be(150);
        }

        [Fact]
        public async Task Deposit_ShouldReturnOkWithUpdatedBalance()
        {
            // Arrange
            var depositRequest = new DepositRequest { Amount = 100 };
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/OnlineWallet/Deposit", depositRequest);

            // Assert
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.True(balanceResponse.Amount > 0);
        }

        [Fact]
        public async Task Deposit_ShouldReturnBadRequestForNegativeAmount()
        {
            // Arrange
            var depositRequest = new DepositRequest { Amount = -100 };
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/OnlineWallet/Deposit", depositRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Withdraw_ShouldReturnOkWithUpdatedBalance()
        {
            // Arrange
            var withdrawRequest = new WithdrawalRequest { Amount = 50 };
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/OnlineWallet/Withdraw", withdrawRequest);

            // Assert
            response.EnsureSuccessStatusCode();
            var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
            Assert.True(balanceResponse.Amount >= 0); 
        }

        [Fact]
        public async Task Withdraw_ShouldThrowInsufficientBalanceException_WhenBalanceIsLow()
        {
            // Arrange
            var withdrawRequest = new WithdrawalRequest { Amount = 1000 };  
            var client = _factory.CreateClient();

            var mockOnlineWalletService = new Mock<IOnlineWalletService>();
            mockOnlineWalletService.Setup(service => service.GetBalanceAsync())
                .ReturnsAsync(new Balance { Amount = 500 });

            mockOnlineWalletService.Setup(service => service.WithdrawFundsAsync(It.IsAny<Withdrawal>()))
                .ThrowsAsync(new InsufficientBalanceException());

            var controller = new OnlineWalletController(
                Mock.Of<ILogger<OnlineWalletController>>(),
                Mock.Of<IMapper>(),
                mockOnlineWalletService.Object
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(async () =>
            {
                await controller.Withdraw(withdrawRequest);
            });

            // Assert
            Assert.NotNull(exception);
        }

    }
}
