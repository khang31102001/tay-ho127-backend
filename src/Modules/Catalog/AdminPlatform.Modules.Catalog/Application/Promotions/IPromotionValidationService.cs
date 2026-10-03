namespace AdminPlatform.Modules.Catalog.Application.Promotions;

/// <summary>Checks a code against a cart and computes the discount — the only place these business rules live.</summary>
public interface IPromotionValidationService
{
    Task<ValidatePromotionResponse> ValidateAsync(ValidatePromotionRequest request, CancellationToken cancellationToken);
}
