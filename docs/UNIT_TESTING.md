# Purchasing and Supplier: unit testing and mocking

## Additional business-rule tests

`PurchaseOrderServiceMockTests.cs` adds 9 cases for invalid quantities, missing/inactive suppliers, valid pending-order creation, expired receipt details, and duplicate receipt attempts. Strict mocked repositories and event publishers verify that rejected operations do not save or publish stock events. A newly created pending order is saved once and does not publish receipt events.

## Run locally

Run from this repository:

```powershell
dotnet test tests/Medzo.PurchasingSupplier.UnitTests/Medzo.PurchasingSupplier.UnitTests.csproj --collect:"XPlat Code Coverage"
```

Latest local validation: **15 passing cases**, including the pre-existing tests. Each parameterized input counts as one case.

## Live demonstration

Push to `feature/se3112-unit-mocking`, open this repository's **Actions** tab, and open the newest unit-test workflow run. Expand the test step for console output, then show the **Summary** table for total/passed/failed/skipped counts.

The workflow uploads TRX test results and Coverlet Cobertura XML in its test-evidence artifact. Coverage measures executed lines/branches within the assemblies included by the collector; it is not the same as the percentage of passing tests. Compare reports only when their included assemblies and filters match.

