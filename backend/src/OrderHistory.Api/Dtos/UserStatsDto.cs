namespace OrderHistory.Api.Dtos;

public record UserStatsDto(int UserId, int OrderCount, decimal LifetimeSpend);
