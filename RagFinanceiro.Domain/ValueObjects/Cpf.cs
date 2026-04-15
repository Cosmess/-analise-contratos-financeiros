namespace RagFinanceiro.Domain.ValueObjects;

public record Cpf
{
    public string Value { get; }

    public Cpf(string value)
    {
        var digits = value?.Replace(".", "").Replace("-", "") ?? string.Empty;
        if (digits.Length != 11 || !digits.All(char.IsDigit))
            throw new ArgumentException($"CPF inválido: '{value}'. Deve conter exatamente 11 dígitos numéricos.");
        Value = digits;
    }

    public override string ToString() => Value;
}
