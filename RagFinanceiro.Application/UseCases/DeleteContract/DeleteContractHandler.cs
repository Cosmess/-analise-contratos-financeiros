using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Application.UseCases.DeleteContract;

public class DeleteContractHandler
{
    private readonly IContractRepository _repository;

    public DeleteContractHandler(IContractRepository repository)
    {
        _repository = repository;
    }

    public Task HandleAsync(DeleteContractCommand command, CancellationToken cancellationToken = default)
    {
        var contractId = new ContractId(command.ContractId);
        var tenantId = new TenantId(command.TenantId);
        return _repository.DeleteAsync(contractId, tenantId, cancellationToken);
    }
}
