# Contributing to HouseHoldPlanner

Thanks for taking the time. HouseHoldPlanner is a single-maintainer project, so the process is
deliberately small - but it is the same for every change, including the maintainer's own.

## How changes get in

1. Open an issue first for anything bigger than a typo or an obvious bug fix, so the direction can
   be agreed before you spend time on it. Use the templates under `.github/ISSUE_TEMPLATE/`.
2. Fork the repository (or branch, if you have write access) and make your change on a branch.
3. Open a pull request against `main`. The pull-request template asks for what changed and why.
4. `main` is protected: a PR merges only after the test stage of
   [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) is green and the branch is up to
   date with `main` (enable auto-merge and it lands on its own once that is the case). Nobody
   pushes to `main` directly, not even the maintainer.

## What a pull request needs

- **Conventional Commits.** The version and the changelog are generated from the commit messages
  (`feat:` = minor release, `fix:` = patch release, `build:`/`ci:`/`docs:`/`test:` = no release).
  Squash-merge keeps the PR title as the commit message, so give the PR a Conventional Commit
  title.
- **Green required checks.** `test-build`, `test-lint`, `test-security` and
  `review / dependency-review` are required; a red one blocks the merge.
- **Tests for new functionality.** New features and bug fixes come with tests in
  `HaushaltsPlaner.Tests`. A PR that adds behaviour without a test is asked to add one.
- **Formatting and warnings.** `dotnet format HaushaltsPlaner.sln --verify-no-changes` runs as the
  required `test-lint` check; run `dotnet format HaushaltsPlaner.sln` before pushing. Warnings are
  errors repo-wide (`TreatWarningsAsErrors` in `Directory.Build.props`) - do not silence one
  without saying why in the PR.
- **Lock files.** Every project except the WASM client carries a `packages.lock.json`, and CI
  restores with `--locked-mode`, so a csproj that disagrees with its lock file fails the restore
  instead of silently updating it. A plain `dotnet restore HaushaltsPlaner.sln` refreshes the lock
  files locally after a package change - commit the result.
- **Vulnerable packages.** `test-security` restores and then fails the build on known High or
  Critical NuGet advisories.
- **The server still has to start.** `test-build` boots the server against a throwaway SQLite
  database and waits for `/health`, so a change that compiles but breaks startup (a missing
  registration, a bad migration) is caught here.

## Running things locally

Requires the .NET 10 SDK.

```bash
export Jwt__Key="$(openssl rand -base64 32)"
dotnet run --project HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj
dotnet run --project HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj
```

Point the database somewhere else with
`ConnectionStrings__DefaultConnection="Data Source=/custom/path/haushaltsplaner.db"`.

With Docker:

```bash
cp .env.example .env
# edit .env and set JWT_KEY (see the comment in the file for how to generate one)
docker compose up -d
```

The same commands CI runs:

```bash
dotnet restore HaushaltsPlaner.sln --locked-mode
dotnet build HaushaltsPlaner.sln --configuration Release --no-restore
dotnet test HaushaltsPlaner.sln --configuration Release --no-build
dotnet format HaushaltsPlaner.sln --verify-no-changes
```

## Security issues

Please do not open a public issue for a vulnerability - use the private reporting path described
in [SECURITY.md](SECURITY.md). The [Code of Conduct](CODE_OF_CONDUCT.md) applies to every
interaction in this repository.
