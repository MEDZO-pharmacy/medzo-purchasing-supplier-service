# medzo-purchasing-supplier-service

Medzo Purchasing and Supplier Microservice.

## Mutation testing

This branch uses Stryker.NET 5 to evaluate the tests around the purchasing
domain rules. The mutation scope is intentionally limited to `Supplier` and
`PurchaseOrder`, covering validation, normalization, deactivation, and purchase
order receipt transitions without mutating controllers or infrastructure code.

Run the checks from the repository root:

```powershell
dotnet tool restore
dotnet test tests/Medzo.PurchasingSupplier.UnitTests/Medzo.PurchasingSupplier.UnitTests.csproj --configuration Release
dotnet stryker --config-file stryker-config.json
```

The verified baseline contains seven focused tests and has a 70.69% mutation
score: 41 killed mutants, 17 surviving mutants, and no uncovered mutants. CI
fails if the score falls below 65%; 80% is the high-quality target. Surviving
mutants remain visible in the report as opportunities for future test
improvement.

Local reports are generated under `StrykerOutput/` and are ignored by Git. The
GitHub Actions workflow uploads the HTML, JSON, and Markdown reports as a
downloadable `purchasing-mutation-reports-*` artifact, even if the quality gate
fails.
