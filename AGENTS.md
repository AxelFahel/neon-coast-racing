# Diretrizes do Agente - Neon Coast Racing

## Controle de Versionamento & Releases

1. **Versionamento Semântico (SemVer)**:
   - Formato: `MAJOR.MINOR.PATCH` (ex: `0.1.0`, `0.2.0`, `1.0.0`).
   - `PATCH`: Correções de bugs retrocompatíveis (`fix:`).
   - `MINOR`: Novas funcionalidades retrocompatíveis (`feat:`).
   - `MAJOR`: Quebras de compatibilidade ou mudanças estruturais na engine/saves.

2. **Padrão de Commits (Conventional Commits)**:
   - `feat:` Nova mecânica, veículo, pista, interface ou áudio.
   - `fix:` Correção de bug na física, UI, controles ou shaders.
   - `docs:` Alterações em documentação (`README.md`, notas).
   - `refactor:` Melhorias de código sem alterar comportamento externo.
   - `perf:` Otimizações de renderização, GPU instancing, física.
   - `chore:` Tarefas de manutenção, `.gitignore`, automações.

3. **Fluxo de Release**:
   - Atualizar a seção correspondente no `CHANGELOG.md`.
   - Criar commit de release (`chore(release): bump version to vX.Y.Z`).
   - Criar tag Git anotada (`git tag -a vX.Y.Z -m "Release vX.Y.Z"`).
   - Enviar a tag para a origem (`git push origin vX.Y.Z`).
   - Publicar no GitHub Releases usando GitHub CLI (`gh release create`).
