using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Loyalty;
using CampusEats.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Loyalty
{
    public class GetLoyaltyTransactionsTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Handler_Returns_Paginated_Transactions()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var userId = Guid.NewGuid();
            context.Users.Add(new CampusEats.Backend.Domain.User
            {
                Id = userId,
                Email = "u@test",
                CreatedAt = DateTime.UtcNow
            });

            for (int i = 0; i < 25; i++)
            {
                context.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PointsChange = 10 + i,
                    Type = LoyaltyTransactionType.Earned,
                    Description = $"Tx {i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            await context.SaveChangesAsync();

            var handler = new GetLoyaltyTransactions.Handler(context);

            // Act - page 1, pageSize 10
            var res1 = await handler.Handle(new GetLoyaltyTransactions.Query(userId, Page: 1, PageSize: 10), CancellationToken.None);
            // Act - page 3, pageSize 10
            var res3 = await handler.Handle(new GetLoyaltyTransactions.Query(userId, Page: 3, PageSize: 10), CancellationToken.None);

            // Assert
            Assert.True(res1.IsSuccess);
            Assert.Equal(10, res1.Value.Count);

            Assert.True(res3.IsSuccess);
            // page 3 (items 21-25) => 5 items
            Assert.Equal(5, res3.Value.Count);

            // Ensure order is descending by CreatedAt
            var first = res1.Value.First();
            var last = res1.Value.Last();
            Assert.True(first.CreatedAt >= last.CreatedAt);
        }

        [Fact]
        public async Task Handler_Returns_Failure_When_User_Missing()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var handler = new GetLoyaltyTransactions.Handler(context);

            // Act
            var result = await handler.Handle(new GetLoyaltyTransactions.Query(Guid.NewGuid(), 1, 10), CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }
    }
}