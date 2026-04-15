namespace RagFinanceiro.Domain.ValueObjects;

public record TenantId(string Value)
{
    public override string ToString() => Value;
}
