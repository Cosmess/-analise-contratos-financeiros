# RAG Financeiro — Análise de Contratos

API para análise inteligente de contratos de crédito pessoal. O sistema indexa PDFs de contratos em um banco vetorial e responde perguntas jurídico-financeiras em linguagem natural, com isolamento de dados por tenant (multitenancy).

---

## Contexto do Projeto

Instituições financeiras precisam consultar rapidamente cláusulas contratuais específicas de cada cliente. Este sistema utiliza **RAG (Retrieval-Augmented Generation)** para:

1. Receber PDFs de contratos via upload
2. Extrair e fragmentar o texto em chunks semânticos
3. Gerar embeddings e armazená-los no PostgreSQL com pgvector
4. Responder perguntas jurídico-financeiras citando trechos exatos do contrato do cliente

Cada tenant (banco/instituição) opera em coleção isolada. Os dados de um cliente só são acessíveis com o CPF correto e dentro do tenant autenticado via JWT.

---

## Arquitetura

O projeto segue **Clean Architecture** com separação em 4 camadas, combinada com **DDD (Domain-Driven Design)** e princípios **SOLID**.

```
┌─────────────────────────────────────────────────────┐
│                   RagFinanceiro.Api                 │  ← Apresentação
│           Controllers · JWT Auth · Swagger          │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│              RagFinanceiro.Application              │  ← Casos de Uso
│        IngestContract · QueryContract · Delete      │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│               RagFinanceiro.Domain                  │  ← Núcleo do Domínio
│   Entities · Value Objects · Interfaces · Services  │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│            RagFinanceiro.Infrastructure             │  ← Infraestrutura
│   SemanticKernel · pgvector · PdfPig · Npgsql       │
└─────────────────────────────────────────────────────┘
```

### Camadas

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domínio | `RagFinanceiro.Domain` | Entidades, Value Objects, interfaces de repositório e serviços de domínio |
| Aplicação | `RagFinanceiro.Application` | Casos de uso (Commands/Queries/Handlers), orquestração |
| Infraestrutura | `RagFinanceiro.Infrastructure` | Implementações concretas: pgvector, PDF, LLM, DI |
| API | `RagFinanceiro.Api` | Controllers HTTP, autenticação JWT, Swagger |

### Regra de dependência
```
Api → Application → Domain ← Infrastructure
```
O domínio não conhece nenhuma camada externa. A infraestrutura implementa as interfaces definidas no domínio.

---

## Padrões Utilizados

### Clean Architecture
- Separação estrita entre domínio, aplicação e infraestrutura
- Dependências apontam sempre para dentro (Domain não depende de nada externo)
- Infraestrutura injetada via interfaces

### DDD (Domain-Driven Design)
- **Aggregate Root**: `Contract` — controla o ciclo de vida do contrato
- **Value Objects**: `Cpf` (com validação), `ContractId`, `TenantId`, `ContractChunk` — imutáveis e sem identidade própria
- **Repository Interface**: `IContractRepository` — definida no domínio, implementada na infraestrutura
- **Domain Services**: `IPdfReaderService`, `ILlmQueryService`, `IChunkingService`
- **Ubiquitous Language**: nomenclatura reflete o domínio financeiro/jurídico

### SOLID
| Princípio | Aplicação |
|---|---|
| **S** — Single Responsibility | Cada handler trata exatamente um caso de uso |
| **O** — Open/Closed | Novos casos de uso adicionados sem alterar handlers existentes |
| **L** — Liskov Substitution | Implementações de `IContractRepository` substituíveis |
| **I** — Interface Segregation | `IPdfReaderService`, `ILlmQueryService` e `IChunkingService` separados |
| **D** — Dependency Inversion | Handlers dependem de abstrações, não de implementações concretas |

### CQRS (Command Query Responsibility Segregation)
- **Commands**: `IngestContractCommand`, `DeleteContractCommand` — operações de escrita
- **Queries**: `QueryContractQuery` — operações de leitura
- Handlers distintos para cada operação

### RAG (Retrieval-Augmented Generation)
- Chunking com overlap de 120 caracteres para preservar contexto entre cláusulas
- Busca por similaridade semântica com score mínimo de 0.72
- Filtro por CPF do cliente nos metadados dos chunks
- Prompt especializado jurídico que instrui o LLM a responder apenas com base no contrato

### Multitenancy
- `tenant_id` extraído exclusivamente do JWT — nunca aceito no body da requisição
- Coleções isoladas por tenant no pgvector: `tenant_{tenantId}`

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma |
| ASP.NET Core | 8.0 | Framework Web |
| Semantic Kernel | 1.x | Orquestração LLM e memória vetorial |
| OpenAI gpt-4o-mini | — | Modelo LLM para respostas |
| OpenAI text-embedding-3-small | — | Geração de embeddings (1536 dimensões) |
| PostgreSQL | 16 | Banco de dados |
| pgvector | — | Extensão para busca vetorial no PostgreSQL |
| Npgsql | 8.x | Driver PostgreSQL para .NET |
| UglyToad.PdfPig | 1.7.x | Extração de texto de PDFs |
| JWT Bearer | 8.x | Autenticação e autorização |

---

## Ferramentas

| Ferramenta | Uso |
|---|---|
| Docker / Docker Compose | Banco PostgreSQL com pgvector em ambiente local |
| Swagger / Swashbuckle | Documentação e teste da API |
| dotnet CLI | Build, restore e execução do projeto |
| NuGet | Gerenciamento de pacotes |

---

## Estrutura de Arquivos

```
analise-contratos-financeiros/
├── docker-compose.yml
├── analise-contratos-financeiros.sln
│
├── RagFinanceiro.Domain/
│   ├── Entities/
│   │   └── Contract.cs
│   ├── ValueObjects/
│   │   ├── Cpf.cs
│   │   ├── ContractId.cs
│   │   ├── TenantId.cs
│   │   └── ContractChunk.cs
│   ├── Repositories/
│   │   └── IContractRepository.cs
│   └── Services/
│       ├── IPdfReaderService.cs
│       ├── ILlmQueryService.cs
│       └── IChunkingService.cs
│
├── RagFinanceiro.Application/
│   └── UseCases/
│       ├── IngestContract/
│       │   ├── IngestContractCommand.cs
│       │   ├── IngestContractResult.cs
│       │   └── IngestContractHandler.cs
│       ├── QueryContract/
│       │   ├── QueryContractQuery.cs
│       │   ├── QueryContractResult.cs
│       │   └── QueryContractHandler.cs
│       └── DeleteContract/
│           ├── DeleteContractCommand.cs
│           └── DeleteContractHandler.cs
│
├── RagFinanceiro.Infrastructure/
│   ├── AI/
│   │   └── SemanticKernelQueryService.cs
│   ├── Pdf/
│   │   ├── PdfPigReaderService.cs
│   │   └── TextChunkingService.cs
│   ├── Persistence/
│   │   ├── ContractMemoryStore.cs
│   │   └── SemanticKernelContractRepository.cs
│   └── Extensions/
│       └── InfrastructureServiceExtensions.cs
│
└── RagFinanceiro.Api/
    ├── Controllers/
    │   └── ContractsController.cs
    ├── appsettings.json
    └── Program.cs
```

---

## Endpoints

| Método | Rota | Role | Descrição |
|---|---|---|---|
| `POST` | `/api/contracts/upload` | admin, analyst | Faz upload e indexa um PDF de contrato |
| `POST` | `/api/contracts/query` | qualquer autenticado | Faz uma pergunta sobre um contrato do cliente |
| `DELETE` | `/api/contracts/{contractId}` | admin | Remove um contrato do índice vetorial |

---

## Como Rodar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Chave de API da OpenAI

### 1. Configurar variáveis

Edite `RagFinanceiro.Api/appsettings.json`:

```json
{
  "OpenAI": {
    "Key": "sk-proj-SUA_CHAVE_AQUI",
    "ChatModel": "gpt-4o-mini",
    "EmbeddingModel": "text-embedding-3-small"
  },
  "Jwt": {
    "Key": "sua-chave-secreta-minimo-32-caracteres",
    "Issuer": "contract-rag-api",
    "Audience": "contract-rag-clients",
    "ExpiresMinutes": 30
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=contratos;Username=postgres;Password=postgres"
  }
}
```

### 2. Subir o banco de dados

```bash
docker compose up -d
```

### 3. Restaurar dependências e executar

```bash
dotnet restore
dotnet run --project RagFinanceiro.Api
```

A API estará disponível em `https://localhost:5001` e a documentação Swagger em `https://localhost:5001/swagger`.

### 4. Exemplos de uso

**Upload de contrato:**
```bash
curl -X POST https://localhost:5001/api/contracts/upload \
  -H "Authorization: Bearer SEU_TOKEN_JWT" \
  -F "file=@contrato.pdf" \
  -F "contractId=CNT-001" \
  -F "contractNumber=2024/001" \
  -F "clientName=João Silva" \
  -F "clientCpf=12345678900"
```

**Consulta ao contrato:**
```bash
curl -X POST https://localhost:5001/api/contracts/query \
  -H "Authorization: Bearer SEU_TOKEN_JWT" \
  -H "Content-Type: application/json" \
  -d '{"question": "Qual o prazo para renegociação?", "clientCpf": "12345678900"}'
```

**Remover contrato:**
```bash
curl -X DELETE https://localhost:5001/api/contracts/CNT-001 \
  -H "Authorization: Bearer SEU_TOKEN_JWT"
```

---

## Decisões de Arquitetura

| Decisão | Motivo |
|---|---|
| `temperature=0` no LLM | Respostas determinísticas — obrigatório para compliance financeiro |
| `tenant_id` extraído do JWT | Impede que o cliente passe um tenant falso para acessar dados de outro tenant |
| `minRelevanceScore=0.72` | Filtra chunks irrelevantes antes de chamar o LLM, economiza tokens e evita alucinação |
| Collection por tenant no pgvector | Isolamento de dados em ambiente SaaS multitenancy |
| Overlap de 120 chars no chunking | Evita que cláusulas sejam cortadas no meio entre chunks |
| Source documents na resposta | Auditabilidade — rastreia qual trecho do contrato embasou cada resposta |
