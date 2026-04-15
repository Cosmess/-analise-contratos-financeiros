namespace RagFinanceiro.Domain.ValueObjects;

public record ContractId(string Value)
{
    public override string ToString() => Value;
}
