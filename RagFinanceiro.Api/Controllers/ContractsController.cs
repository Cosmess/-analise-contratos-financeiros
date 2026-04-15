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
    DeleteContractHandler deleteHandler) : ControllerBase
{
    private string TenantId => User.FindFirstValue("tenant_id")!;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("upload")]
    [Authorize(Roles = "admin,analyst")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req, CancellationToken cancellationToken)
    {
        await using var stream = req.File.OpenReadStream();

        var command = new IngestContractCommand(
            PdfStream: stream,
            ContractId: req.ContractId,
            ContractNumber: req.ContractNumber,
            ClientName: req.ClientName,
            ClientCpf: req.ClientCpf,
            TenantId: TenantId
        );

        var result = await ingestHandler.HandleAsync(command, UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest req, CancellationToken cancellationToken)
    {
        var query = new QueryContractQuery(
            Question: req.Question,
            ClientCpf: req.ClientCpf,
            TenantId: TenantId
        );

        var result = await queryHandler.HandleAsync(query, cancellationToken);
        return Ok(new { result.Question, result.Answer, result.Sources, QueriedBy = UserId });
    }

    [HttpDelete("{contractId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string contractId, CancellationToken cancellationToken)
    {
        var command = new DeleteContractCommand(ContractId: contractId, TenantId: TenantId);
        await deleteHandler.HandleAsync(command, cancellationToken);
        return Ok(new { Deleted = contractId, By = UserId });
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
