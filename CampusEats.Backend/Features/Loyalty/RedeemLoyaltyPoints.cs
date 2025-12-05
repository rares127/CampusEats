using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Loyalty;

public static class RedeemLoyaltyPoints
{
    public record Command(Guid UserId, int PointsToRedeem, Guid? OrderId = null) : IRequest<Result<RedeemPointsResultDto>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required");
            RuleFor(x => x.PointsToRedeem)
                .GreaterThan(0).WithMessage("Points to redeem must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Cannot redeem more than 10000 points at once");
        }
    }

    public class Handler : IRequestHandler<Command, Result<RedeemPointsResultDto>>
    {
        private readonly AppDbContext _context;
        private const decimal PointsToMoneyRatio = 0.1m;
        private const int MinimumPointsToRedeem = 50; 

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<RedeemPointsResultDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return Result<RedeemPointsResultDto>.Failure("User not found");

            if (user.LoyaltyPoints < MinimumPointsToRedeem)
                return Result<RedeemPointsResultDto>.Failure($"Minimum {MinimumPointsToRedeem} points required to redeem");

            if (request.PointsToRedeem < MinimumPointsToRedeem)
                return Result<RedeemPointsResultDto>.Failure($"Minimum {MinimumPointsToRedeem} points required to redeem");

            if (user.LoyaltyPoints < request.PointsToRedeem)
                return Result<RedeemPointsResultDto>.Failure($"Insufficient points. You have {user.LoyaltyPoints} points.");

            var discountAmount = request.PointsToRedeem * PointsToMoneyRatio;

            user.LoyaltyPoints -= request.PointsToRedeem;
            user.UpdatedAt = DateTime.UtcNow;

            var transaction = new LoyaltyTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PointsChange = -request.PointsToRedeem,
                Type = LoyaltyTransactionType.Redeemed,
                Description = request.OrderId.HasValue
                    ? $"Redeemed {request.PointsToRedeem} points for {discountAmount:F2} RON discount on order"
                    : $"Redeemed {request.PointsToRedeem} points for {discountAmount:F2} RON discount",
                OrderId = request.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new RedeemPointsResultDto
            {
                PointsRedeemed = request.PointsToRedeem,
                DiscountAmount = discountAmount,
                RemainingPoints = user.LoyaltyPoints,
                Message = $"Successfully redeemed {request.PointsToRedeem} points for {discountAmount:F2} RON discount!"
            };

            return Result<RedeemPointsResultDto>.Success(result);
        }
    }
}