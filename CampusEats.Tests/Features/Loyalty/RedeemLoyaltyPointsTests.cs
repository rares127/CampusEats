using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Loyalty;
using CampusEats.Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Loyalty
{
    public class RedeemLoyaltyPointsTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Handler_Fails_If_Not_Enough_Points()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var user = new CampusEats.Backend.Domain.User
            {
                Id = Guid.NewGuid(),
                Email = "lowpoints@test",
                LoyaltyPoints = 30,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var handler = new RedeemLoyaltyPoints.Handler(context);
            var command = new RedeemLoyaltyPoints.Command(user.Id, PointsToRedeem: 50);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Minimum", result.Errors.First() ?? result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handler_Succeeds_And_Creates_Transaction_When_Enough_Points()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var user = new CampusEats.Backend.Domain.User
            {
                Id = Guid.NewGuid(),
                Email = "rich@test",
                LoyaltyPoints = 500,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var handler = new RedeemLoyaltyPoints.Handler(context);
            var command = new RedeemLoyaltyPoints.Command(user.Id, PointsToRedeem: 100, OrderId: null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(100, result.Value.PointsRedeemed);
            Assert.Equal(400, result.Value.RemainingPoints);

            // Validate DB changes
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.Equal(400, updatedUser.LoyaltyPoints);

            var tx = context.LoyaltyTransactions.FirstOrDefault(t => t.UserId == user.Id && t.PointsChange < 0);
            Assert.NotNull(tx);
            Assert.Equal(-100, tx.PointsChange);
            Assert.Equal(LoyaltyTransactionType.Redeemed, tx.Type);
        }
    }
}