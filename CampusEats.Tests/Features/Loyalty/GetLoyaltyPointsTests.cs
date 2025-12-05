using System;
using System.Threading;
using System.Threading.Tasks;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Loyalty;
using CampusEats.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Loyalty
{
    public class GetLoyaltyPointsTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Handler_Returns_LoyaltyPoints_For_User()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@local",
                CreatedAt = DateTime.UtcNow,
                LoyaltyPoints = 120
            };
            context.Users.Add(user);

            context.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PointsChange = 120,
                Type = LoyaltyTransactionType.Earned,
                Description = "Initial",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var handler = new GetLoyaltyPoints.Handler(context);

            // Act
            var result = await handler.Handle(new GetLoyaltyPoints.Query(user.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(user.Id, result.Value.UserId);
            Assert.Equal(120, result.Value.CurrentPoints);
            Assert.Equal(120, result.Value.TotalEarned);
            Assert.Equal(0, result.Value.TotalRedeemed);
        }

        [Fact]
        public async Task Handler_Returns_Failure_When_User_Not_Found()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var handler = new GetLoyaltyPoints.Handler(context);

            // Act
            var result = await handler.Handle(new GetLoyaltyPoints.Query(Guid.NewGuid()), CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }
    }
}