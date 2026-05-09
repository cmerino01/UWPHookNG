# Git hooks

The `pre-push` hook builds the solution (Release, warnings-as-errors via `Directory.Build.props`)
and runs the test suite. If either fails, the push is refused.

Enable in your local clone:

```powershell
git config core.hooksPath scripts/git-hooks
```

Disable / use the default hooks path:

```powershell
git config --unset core.hooksPath
```

Hooks are committed (rather than living in `.git/hooks/`) so every contributor opts into the same checks.
