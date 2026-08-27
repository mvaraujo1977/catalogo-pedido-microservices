---
name: catalogo-pedido-workflow
description: Regras de trabalho para o projeto CatalogoPedido (microsserviços .NET 8 de portfólio, Clean Architecture, CQRS). Use SEMPRE que for implementar, alterar, revisar código, rodar build/testes/docker, ou fazer commit neste repositório — inclui disciplina de fases incrementais, escopo por camada, checkpoints antes de commit, versionamento de pacotes/imagens e convenções de arquitetura (Result Pattern, soft delete, DTOs, mapeamento). Consulte antes de criar qualquer arquivo ou rodar dotnet/docker/git.
---

# Disciplina de desenvolvimento — CatalogoPedido

Este projeto é um portfólio de estudo (.NET 8, Clean Architecture, DDD "light",
CQRS com MediatR) construído de forma incremental, fase por fase, sempre com
validação humana entre etapas. As regras abaixo existem para preservar isso
mesmo quando o trabalho é delegado a um agente que executa comandos sozinho.

## 1. Nunca avance de fase sem confirmação explícita

- Trabalhe **apenas** na fase/etapa que foi pedida na instrução atual.
- Se terminar antes do esperado, ou identificar uma próxima etapa óbvia,
  **PARE e pergunte** antes de continuar. Não encadeie fases por conta própria,
  mesmo que pareça o passo lógico seguinte.
- Isso vale mesmo em modo autônomo/auto-run: a autonomia é sobre *como*
  executar os passos pedidos, não sobre *decidir quais passos* vêm a seguir.

## 2. Escopo travado por camada/diretório

- Cada instrução delimita um diretório de trabalho (ex: só
  `Catalog.Domain/`, ou só `Catalog.Infrastructure/`). Não altere arquivos
  fora desse diretório, mesmo que pareçam relacionados.
- Exceção: quando a própria instrução listar explicitamente um arquivo fora
  do escopo geral (ex: "delete também estes dois arquivos de template em
  Catalog.API/"), a instrução explícita vale — mas isso deve ser reportado
  como uma nota, não executado silenciosamente.
- Se uma instrução for ambígua sobre escopo, prefira perguntar a assumir.

## 3. Checkpoint obrigatório antes de qualquer commit

Antes de rodar `git commit`, sempre, sem exceção:

1. Rode `dotnet build` e mostre o resumo final completo
   (`Build succeeded`/`failed` + contagem de avisos/erros) — nunca corte
   ou resuma essa linha.
2. Rode `git add .` seguido de `git status` e **pare aí**. Não commite
   ainda. Mostre a lista completa de arquivos staged para revisão humana.
3. Só rode `git commit` depois de receber confirmação explícita.

Isso existe pra pegar dois problemas antes que virem commit: build quebrado
e arquivos que deveriam estar no `.gitignore` mas vazaram para o staging
(ex: `.env`, `bin/`, `obj/`).

## 4. Nunca reexecute um bloco de instruções sem checar o estado atual

Antes de rodar um bloco de comandos que já pode ter sido executado antes:

- Rode `git log --oneline -5` e/ou `git status` primeiro.
- Se o resultado esperado já está commitado, **não reexecute** os comandos
  de criação/instalação — isso pode desfazer decisões já tomadas (exemplo
  real: reinstalar um pacote sem `--version` reverteria um pin deliberado
  de versão).
- Reporte o estado atual e pergunte como proceder, em vez de assumir que
  "rodar de novo não faz mal".

## 5. Versionamento de pacotes e imagens

- Pacotes NuGet: sempre com `--version` explícita. Nunca instale a versão
  mais recente sem antes confirmar compatibilidade com o target framework
  do projeto (`net8.0`) e sem avaliar mudanças de licenciamento recentes
  (ex: MediatR v13+ e MassTransit tiveram mudanças de licença em 2025 —
  este projeto fixa versões anteriores a essas mudanças, deliberadamente).
- Imagens Docker: sempre uma tag de versão pinada (ex: `postgres:16-alpine`,
  `quay.io/keycloak/keycloak:26.7`). Nunca `latest`.
- Ao adicionar uma biblioteca nova, informe antes: nome, pacote NuGet,
  finalidade, e o motivo da escolha (isso é uma regra do projeto, não só
  de estilo — o objetivo final inclui conseguir justificar cada escolha em
  entrevista técnica).

## 6. Convenções de arquitetura já decididas para este projeto

Não reabra essas decisões sem confirmação explícita do usuário:

- **Regra de dependência (Clean Architecture)**: Domain não referencia
  nada; Application referencia só Domain; Infrastructure referencia
  Application (implementa suas interfaces); API referencia Application e
  Infrastructure apenas para wiring de DI — nenhuma regra de negócio em
  Controller.
- **Result Pattern**: falhas esperadas (ex: "não encontrado", validação)
  retornam `Result`/`Result<T>`, nunca são modeladas como exceção.
  Exceção é reservada para o inesperado.
- **Soft delete**: exclusão lógica via método de domínio
  (`Entidade.Deactivate()`), nunca hard delete.
- **Mapeamento**: extension methods manuais (`ToDto()`), não AutoMapper —
  mapeamentos simples não justificam a dependência.
- **DTOs sempre nas queries**: nunca expor entidade de domínio diretamente
  na API.
- **YAGNI**: não crie abstrações (ex: `BuildingBlocks`, Value Objects,
  Unit of Work) antes de existir duplicação real ou necessidade concreta.
  Se avaliar que vale a pena, pergunte antes de criar.
- **Guard clauses no Domain via helpers nativos do .NET 8**
  (`ArgumentException.ThrowIfNullOrWhiteSpace`,
  `ArgumentOutOfRangeException.ThrowIfNegative`, etc.) em vez de bibliotecas
  externas de validação de invariante.

## 7. Segredos nunca em texto plano no repositório

- Nunca escreva senha, connection string real, client secret, ou token em
  `appsettings.json`, `docker-compose.yml`, ou qualquer arquivo versionado.
- Localmente: `.env` (nunca commitado) para variáveis do Docker Compose, e
  **.NET User Secrets** para connection strings da aplicação.
- Ao precisar ler um valor sensível de `.env` para configurar algo (ex:
  `dotnet user-secrets set`), leia o valor internamente mas **não exiba a
  senha na resposta** — confirme só que o comando rodou com sucesso.

## 8. Sempre reporte saída completa de comandos

- Nunca resuma ou corte a saída de `dotnet build`, `dotnet test`,
  `docker compose ps/logs`, ou `git status` antes de mostrá-la — o usuário
  precisa ver o resultado real para validar cada checkpoint.
- Se a saída for muito longa, pode reportar um resumo objetivo, mas sempre
  incluindo a linha de resultado final (contagem de erros/avisos, exit
  code, status dos containers) sem paráfrase que esconda um erro.
