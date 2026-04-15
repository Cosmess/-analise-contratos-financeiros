using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagFinanceiro.Application.UseCases.DeleteContract;
using RagFinanceiro.Application.UseCases.IngestContract;
using RagFinanceiro.Application.UseCases.QueryContract;
using System.Security.Claims;

namespace RagFinanceiro.Api.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize]
public class ContractsController(
    IngestContractHandler ingestHandler,
    QueryContractHandler queryHandler,
    DeleteContractHandler deleteHandler,
    ILogger<ContractsController> logger) : ControllerBase
{
    private string TenantId => GetRequiredClaim("tenant_id");
    private string UserId => GetRequiredClaim(ClaimTypes.NameIdentifier);

    [HttpPost("upload")]
    [Authorize(Roles = "admin,analyst")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req, CancellationToken cancellationToken)
    {
        ValidateUploadRequest(req);

        var tenantId = TenantId;
        var userId = UserId;

        logger.LogInformation(
            "Upload de contrato iniciado. ContractId: {ContractId} | TenantId: {TenantId} | UserId: {UserId}",
            req.ContractId, tenantId, userId);

        await using var stream = req.File.OpenReadStream();

        var command = new IngestContractCommand(
            PdfStream: stream,
            ContractId: req.ContractId,
            ContractNumber: req.ContractNumber,
            ClientName: req.ClientName,
            ClientCpf: req.ClientCpf,
            TenantId: tenantId
        );

        var result = await ingestHandler.HandleAsync(command, userId, cancellationToken);

        logger.LogInformation(
            "Upload concluido. ContractId: {ContractId} | Chunks: {ChunksIndexed} | TenantId: {TenantId}",
            result.ContractId, result.ChunksIndexed, tenantId);

        return Ok(result);
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest req, CancellationToken cancellationToken)
    {
        ValidateQueryRequest(req);

        var tenantId = TenantId;
        var userId = UserId;

        logger.LogInformation(
            "Consulta iniciada. TenantId: {TenantId} | UserId: {UserId} | QuestionLength: {QuestionLength}",
            tenantId, userId, req.Question.Length);

        var query = new QueryContractQuery(
            Question: req.Question,
            ClientCpf: req.ClientCpf,
            TenantId: tenantId
        );

        var result = await queryHandler.HandleAsync(query, cancellationToken);

        logger.LogInformation(
            "Consulta concluida. TenantId: {TenantId} | UserId: {UserId} | SourceCount: {SourceCount}",
            tenantId, userId, result.Sources.Count);

        return Ok(new { result.Question, result.Answer, result.Sources, QueriedBy = userId });
    }

    [HttpDelete("{contractId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string contractId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contractId))
            throw new ArgumentException("ContractId e obrigatorio.", nameof(contractId));

        var tenantId = TenantId;
        var userId = UserId;

        logger.LogInformation(
            "Exclusao de contrato solicitada. ContractId: {ContractId} | TenantId: {TenantId} | UserId: {UserId}",
            contractId, tenantId, userId);

        var command = new DeleteContractCommand(ContractId: contractId, TenantId: tenantId);
        await deleteHandler.HandleAsync(command, cancellationToken);

        logger.LogInformation(
            "Contrato excluido com sucesso. ContractId: {ContractId} | TenantId: {TenantId}",
            contractId, tenantId);

        return Ok(new { Deleted = contractId, By = userId });
    }

    private string GetRequiredClaim(string claimType)
    {
        var value = User.FindFirstValue(claimType);

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException($"Claim obrigatoria ausente: {claimType}.");

        return value;
    }

    private static void ValidateUploadRequest(UploadRequest req)
    {
        if (req.File is null || req.File.Length == 0)
            throw new ArgumentException("Arquivo PDF e obrigatorio.", nameof(req.File));

        if (string.IsNullOrWhiteSpace(req.ContractId))
            throw new ArgumentException("ContractId e obrigatorio.", nameof(req.ContractId));

        if (string.IsNullOrWhiteSpace(req.ContractNumber))
            throw new ArgumentException("ContractNumber e obrigatorio.", nameof(req.ContractNumber));

        if (string.IsNullOrWhiteSpace(req.ClientName))
            throw new ArgumentException("ClientName e obrigatorio.", nameof(req.ClientName));

        if (string.IsNullOrWhiteSpace(req.ClientCpf))
            throw new ArgumentException("ClientCpf e obrigatorio.", nameof(req.ClientCpf));
    }

    private static void ValidateQueryRequest(QueryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Question))
            throw new ArgumentException("Question e obrigatoria.", nameof(req.Question));

        if (string.IsNullOrWhiteSpace(req.ClientCpf))
            throw new ArgumentException("ClientCpf e obrigatorio.", nameof(req.ClientCpf));
    }
}

public record UploadRequest(
    IFormFile File,
    string ContractId,
    string ContractNumber,
    string ClientName,
    string ClientCpf
);

public record QueryRequest(string Question, string ClientCpf);
