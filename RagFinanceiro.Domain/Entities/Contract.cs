using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Domain.Entities;

public class Contract
{
    public ContractId ContractId { get; private set; }
    public TenantId TenantId { get; private set; }
    public Cpf Cpf { get; private set; }
    public string Number { get; private set; }
    public string ClientName { get; private set; }
    public int ChunkCount { get; private set; }
    public DateTime IndexedAt { get; private set; }

    private Contract(ContractId contractId, TenantId tenantId, Cpf cpf, string number, string clientName)
    {
        ContractId = contractId;
        TenantId = tenantId;
        Cpf = cpf;
        Number = number;
        ClientName = clientName;
        ChunkCount = 0;
        IndexedAt = DateTime.UtcNow;
    }

    public static Contract Create(string contractId, string tenantId, string cpf, string number, string clientName)
    {
        return new Contract(
            new ContractId(contractId),
            new TenantId(tenantId),
            new Cpf(cpf),
            number,
            clientName
        );
    }

    public void UpdateChunkCount(int count)
    {
        ChunkCount = count;
    }
}
