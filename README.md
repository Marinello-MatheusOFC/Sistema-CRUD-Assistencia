# Sistema CRUD Assistência

Sistema web para gestão de uma assistência técnica: cadastro de clientes, controle de
aparelhos e abertura/execução de ordens de serviço (OS), com autenticação, histórico
completo de mudanças de status e impressão da OS.

Projeto em ASP.NET Core MVC (.NET 10) com PostgreSQL, pensado para uso local ou em
servidor próprio, com execução via Docker ou diretamente com o .NET SDK.

## Fluxo do sistema

    Cliente → Aparelho → Ordem de Serviço → Diagnóstico → Reparo → Pronto → Retirada

1. Cadastro do **cliente** (CPF único, telefone, endereço com CEP).
2. Cadastro do **aparelho** vinculado ao cliente (tipo, marca, modelo, IMEI/nº de série).
3. Abertura da **OS** (wizard: busca cliente por CPF → aparelho → defeito relatado).
4. Análise: diagnóstico técnico, orçamento (peças + mão de obra) e aprovação do cliente.
5. Execução do serviço, com mudanças de status registradas em **histórico** (usuário, data, status anterior/novo, observação).
6. Finalização (**Pronto**) e **retirada** pelo cliente (**Entregue**) — status terminais são Entregue e Cancelado.

## Funcionalidades existentes

- **Dashboard** com indicadores de clientes, aparelhos e OS (abertas, prontas, atrasadas).
- **Clientes**: busca, cadastro, edição, detalhes, desativação/reativação (não apaga histórico), CPF único validado no servidor.
- **Aparelhos**: cadastro por cliente, edição (proprietário imutável), detalhes com histórico de OS.
- **Ordens de Serviço**: criação assistida, listagem com filtros, detalhes completos, edição de diagnóstico/serviço/peças/valores, alteração de status com histórico, finalização e retirada.
- **Impressão da OS** em página dedicada (sem dados internos sensíveis).
- **Busca rápida unificada** (clientes, aparelhos e OS).
- **Consulta de CEP** automática (ViaCEP) nos formulários.
- **Autenticação** (ASP.NET Core Identity): login/logout, cookie de sessão, bloqueio após tentativas, sem cadastro público.
- **Seed**: cria o primeiro usuário administrador a partir de variáveis de ambiente; dados de exemplo apenas em Development.

Não há (por definição do escopo): estoque, caixa/financeiro, pagamentos, envio de
e-mail/WhatsApp/SMS, anexos de fotos, portal do cliente, API pública, multiempresa
ou emissão de NF-e.

## Tecnologias

| Camada | Tecnologia |
| --- | --- |
| Runtime | .NET 10 (ASP.NET Core MVC) |
| Banco de dados | PostgreSQL (Npgsql / EF Core 10) |
| Autenticação | ASP.NET Core Identity (cookie, hash de senha) |
| Front-end | Razor Views, Bootstrap 5, jQuery, jquery-validation |
| Testes | xUnit + EF Core com SQLite em memória (não exige PostgreSQL) |
| Implantação | Docker / Docker Compose (opcional) |

## Pré-requisitos

- [.NET SDK 10.x](https://dotnet.microsoft.com/download) (build e execução local).
- PostgreSQL — apenas para rodar a aplicação. Testes e build não precisam dele.
- Docker + Docker Compose — opcional, para subir aplicação e banco com um comando.

## Variáveis de ambiente

| Variável | Obrigatória | Descrição |
| --- | --- | --- |
| `ConnectionStrings__Default` | Sim (fora do Docker) | Connection string do PostgreSQL. A aplicação **não sobe** sem ela. |
| `ADMIN_EMAIL` | Sim, enquanto não houver usuários | E-mail do primeiro usuário (seed). |
| `ADMIN_PASSWORD` | Sim, enquanto não houver usuários | Senha do primeiro usuário (mín. 8, maiúscula, minúscula e dígito). |
| `ASPNETCORE_ENVIRONMENT` | Não | `Development` (padrão local) ou `Production`. |
| `ASPNETCORE_URLS` | Não | Endereço que a aplicação escuta (padrão local: `http://localhost:5002`). |
| `POSTGRES_PASSWORD` | Não (Docker) | Senha do PostgreSQL no Docker Compose. Padrão local: `assistencia-dev-local` (apenas LOCAL/DEV). |
| `APP_PORT` / `POSTGRES_PORT` | Não (Docker) | Portas publicadas pelo compose (padrões: 8080 / 5432). |

Regras:

- O arquivo `.env` (usado pelo Docker Compose) fica na raiz e **nunca é versionado**
  (o `.gitignore` já o exclui). Comece copiando `.env.example`.
- Em produção defina valores próprios e mantenha o `.env` fora do repositório.
- Não há cadastro público: sem `ADMIN_EMAIL`/`ADMIN_PASSWORD` no primeiro start,
  nenhum usuário é criado e ninguém consegue entrar.

## Execução com Docker (recomendada para testar)

```powershell
# 1. Copie o exemplo de variáveis e edite ADMIN_EMAIL / ADMIN_PASSWORD
copy .env.example .env

# 2. Suba aplicação + PostgreSQL
docker compose up --build
```

- Aplicação: `http://localhost:8080` (ou `http://localhost:<APP_PORT>`).
- O PostgreSQL interno só aceita conexões da própria máquina (`127.0.0.1`).
- Migrations são aplicadas automaticamente em `Development` (padrão do compose).
- Parar: `docker compose down`. Apagar também os dados: `docker compose down -v`.

> Nesta máquina de desenvolvimento o Docker não estava disponível; o `Dockerfile`,
> o `docker-compose.yml` e o `.dockerignore` foram validados estaticamente — a
> execução em runtime do Docker não foi realizada aqui.

## Execução local sem Docker

O `dotnet run` **não lê o arquivo `.env`** — exporte as variáveis no terminal:

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=assistencia;Username=postgres;Password=assistencia-dev-local;SSL Mode=Disable"
$env:ADMIN_EMAIL = "admin@exemplo.local"
$env:ADMIN_PASSWORD = "TroqueEstaSenha123!"

cd src\SistemaCrudAssistencia
dotnet run
```

Acesse `http://localhost:5002` (perfil `http` do `launchSettings.json`).

Observações:

- É preciso um PostgreSQL rodando (local, Docker ou remoto) com banco criado.
- Em `Development` a migration e o seed de dados de exemplo rodam sozinhos no primeiro start.

## Testes

Na raiz da solução:

```powershell
dotnet test
```

Os testes usam SQLite em memória com constraints reais (FK, índices únicos):
não é necessário PostgreSQL nem Docker. Eles cobrem as regras críticas (CPF
duplicado, cálculo de valores, fluxo de status, integridade do banco,
antiforgery/autorização, ViaCEP e ViewModels).

## Primeiro usuário e dados de exemplo

- O seed cria o usuário administrador **apenas se não existir nenhum usuário**,
  usando `ADMIN_EMAIL` + `ADMIN_PASSWORD` (em qualquer ambiente).
- Dados de exemplo (clientes/aparelhos/OS fictícios) são criados **somente em
  Development** e apenas se não houver OS nenhuma.
- Não existe tela de cadastro de usuários.

## Banco de dados e migrations

- PostgreSQL + EF Core; migration única `InitialCreate` (histórico em
  `__migrations_historico`).
- **Development**: migrations aplicadas automaticamente na subida da aplicação.
- **Production**: automação desligada (`appsettings.Production.json` define
  `Aplicacao:AplicarMigrationsAutomaticamente=false`). Aplique manualmente:

```powershell
$env:ConnectionStrings__Default = "<connection string de produção>"
dotnet ef database update --project src\SistemaCrudAssistencia
```

- Não crie migrations sem necessidade real de alteração de modelo.

## Configuração para produção (checklist)

- `ASPNETCORE_ENVIRONMENT=Production`.
- HTTPS terminado na frente da aplicação (proxy reverso: nginx, Caddy, IIS
  ou TLS do provedor). A aplicação já usa HSTS e redirecionamento em
  ambientes que não são Development, e o cookie de sessão exige `Secure`
  (login não funciona sem HTTPS fora de Development — isso é intencional).
- **ForwardedHeaders não está configurado** no `Program.cs` (deliberadamente, para
  não alterar o pipeline sem validação com o proxy real). Antes de subir atrás de
  proxy, configure `ForwardedHeaders` (proto/host) conforme o provedor e valide
  login, redirecionamentos e a URL correta.
- Aplique migrations manualmente (seção anterior) — nunca com automação ligada em produção.
- Mantenha `.env`, connection strings e senhas fora do repositório.
- Faça backups regulares do banco (seção abaixo).
- Não publique a porta do PostgreSQL em servidores públicos.

## PostgreSQL externo (qualquer provedor)

O sistema funciona com qualquer PostgreSQL compatível (servidor próprio, Docker
ou serviço gerenciado). Monte a connection string com host, porta, banco, usuário,
senha e o modo SSL exigido pelo provedor e exporte `ConnectionStrings__Default`
(em produção, via variáveis de ambiente do serviço). Depois rode a aplicação
(`dotnet run` em desenvolvimento; publicação/containers em produção). Exemplos:

```text
# Servidor próprio ou container local
Host=localhost;Port=5432;Database=assistencia;Username=postgres;Password=SUA_SENHA;SSL Mode=Disable

# Serviço gerenciado (SSL conforme o provedor)
Host=SEU_HOST;Port=5432;Database=SEU_BANCO;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require
```

## Backup e restauração

```powershell
# Backup
pg_dump -h <host> -U <usuario> -d <banco> -Fc -f assistencia.dump

# Restauração
pg_restore -h <host> -U <usuario> -d <banco> -v assistencia.dump
```

Com Docker local, rode os comandos de dentro do container:
`docker compose exec db pg_dump -U postgres -Fc assistencia > backup.dump`.

## Estrutura da solução

```text
SistemaCrudAssistencia.slnx
├── src/SistemaCrudAssistencia/        Aplicação MVC
│   ├── Controllers/                    Telas (finas, sem regra de negócio)
│   ├── Models/                         Entidades, ViewModels, validações
│   ├── Data/                           AppDbContext, configurations, migrations
│   ├── Services/                       Regras de negócio (clientes, aparelhos, OS, busca, ViaCEP, seed)
│   ├── Views/                          Razor em pt-BR
│   └── wwwroot/                        CSS, JS e libs estáticas
└── tests/SistemaCrudAssistencia.Tests/ Testes xUnit
```

## Status do projeto

Etapas 1 a 11 do plano de desenvolvimento executadas (fundação, autenticação,
dashboard, clientes, aparelhos, ordens de serviço, busca/impressão/ViaCEP,
testes das regras críticas e finalização com documentação/Docker).

Na execução desta etapa (07/10/2026): build sem erros nem avisos e 137 testes
aprovados, 0 falhas. A validação em runtime do Docker e do PostgreSQL real não
foi possível nesta máquina (Docker/PostgreSQL não instalados) — o compose e o
Dockerfile seguem prontos para uso em ambiente com Docker.

Detalhes técnicos, limitações conhecidas e pendências de implantação:
veja `DIAGNOSTICO.txt`.
