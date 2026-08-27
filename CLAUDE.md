# CatalogoPedido — Instruções do projeto

Projeto de portfólio: microsserviços .NET 8, Clean Architecture, DDD "light",
CQRS com MediatR. Regras completas de disciplina de desenvolvimento em
`.claude/skills/catalogo-pedido-workflow/SKILL.md` — consulte antes de
escrever qualquer código, rodar dotnet/docker, ou fazer commit.

Resumo das regras inegociáveis (ver a skill acima para os detalhes e o
porquê de cada uma):

1. **Nunca avance para a próxima fase** sem confirmação explícita do
   usuário, mesmo em modo autônomo.
2. **Trabalhe só no diretório/camada indicado** na instrução atual; não
   toque em outras partes do projeto sem pedido explícito.
3. **Antes de qualquer `git commit`**: rode `dotnet build` (resumo completo)
   e `git add . && git status` (lista completa) — pare aí e espere
   confirmação antes de commitar.
4. **Antes de reexecutar um bloco de comandos**, confira `git log`/
   `git status` para não desfazer trabalho já commitado.
5. **Pacotes NuGet e imagens Docker sempre com versão pinada** — nunca
   `latest`.
6. **Nunca escreva segredo em texto plano** em arquivo versionado (use
   `.env` + .NET User Secrets).
