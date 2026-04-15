using Microsoft.Extensions.Logging;
using RagFinanceiro.Domain.Repositories;
using RagFinanceiro.Domain.ValueObjects;

namespace RagFinanceiro.Application.UseCases.DeleteContract;

public class DeleteContractHandler(
    IContractRepository repository,
    ILogger<DeleteContractHandler> logger)
{
    public async Task HandleAsync(DeleteContractCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Solicitação de exclusão. ContractId: {ContractId} | TenantId: {TenantId}",
            command.ContractId, command.TenantId);

        var contractId = new ContractId(command.ContractId);
        var tenantId = new TenantId(command.TenantId);

        await repository.DeleteAsync(contractId, tenantId, cancellationToken);

        logger.LogInformation(
            "Contrato removido do índice vetorial. ContractId: {ContractId} | TenantId: {TenantId}",
            command.ContractId, command.TenantId);
    }
}
