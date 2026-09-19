---
name: versioning
description: Guia e automações para controle de versão SemVer, geração de changelog e releases no GitHub para Neon Coast Racing.
---

# Skill: Controle de Versionamento & Releases

## Como lançar uma nova versão

1. **Verificar alterações pendentes**:
   ```bash
   git status
   git diff
   ```

2. **Decidir o bump de versão (SemVer)**:
   - `PATCH` (+0.0.1): Correções de bugs.
   - `MINOR` (+0.1.0): Novas funcionalidades, pistas, veículos, sistemas.
   - `MAJOR` (+1.0.0): Mudanças estruturais críticas, migrações de engine ou quebras de compatibilidade.

3. **Atualizar o CHANGELOG.md**:
   Adicione a nova versão no topo com a data atual e liste as alterações em:
   - `### Adicionado`
   - `### Modificado`
   - `### Corrigido`
   - `### Removido`

4. **Commits e Tag**:
   ```bash
   git add CHANGELOG.md README.md
   git commit -m "chore(release): bump version to vX.Y.Z"
   git tag -a vX.Y.Z -m "Release vX.Y.Z"
   git push origin main --tags
   ```

5. **Criar GitHub Release**:
   ```bash
   gh release create vX.Y.Z --title "vX.Y.Z" --notes "Notas da versão"
   ```
