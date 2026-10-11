# Model Load Checker

A small web app that answers one question: **will this 3D model load on a phone, a tablet or a desktop?**

You give it a `.obj` file. It counts the triangles, compares the count with a budget for each device, and shows a green, orange or red card per device. Everything runs in the browser; the file is never sent to a server.

Built with C# and .NET 10: a Blazor WebAssembly front end, a class library that holds all the logic, xUnit and bUnit tests, and a small console tool that turns test results into a readable report.

## The problem

3D models are made of triangles, and every triangle is work for the device drawing it. A model that is smooth on a desktop can stutter or fail to appear on a phone. Artists and developers often find this out late, on the device itself.

The triangle count is a quick first check that needs nothing but the file. This app does that check and also reports files that are damaged, with the line number of each problem.

## What it checks

For each device the app gives one of three verdicts:

| Verdict | Colour | Meaning |
|---|---|---|
| Ready | Green | The file is fine and its triangle count is within the device's budget. |
| Too heavy | Orange | The file is fine but has more triangles than the budget. |
| Broken | Red | The file has problems, or has no triangles at all. |

A file counts as **broken** if the parser reports any problem (a value that is not a number, a face that points at a vertex that does not exist, a face with fewer than three corners) or if it contains no triangles. Broken is checked first, because a triangle count from a damaged file cannot be trusted.

A model with exactly the budget is Ready. One triangle more is Too heavy.

### Device budgets are assumptions

| Device | Triangle budget |
|---|---|
| Phone | 100,000 |
| Tablet | 300,000 |
| Desktop | 1,000,000 |

These are round numbers chosen as a reasonable starting point. They are **not measurements**. What a real device can draw depends on the exact hardware, the textures, the lighting and the app showing the model. The budgets live in one file, [DeviceProfile.cs](ModelChecker.Core/DeviceProfile.cs), so they are easy to change.

Triangle count is also only one part of whether a model loads well. This app does not look at texture sizes, materials or memory use.

## How to run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). All commands are run from the repository root.

**Run the website**

```
dotnet run --project ModelChecker.Web
```

Then open http://localhost:5089. Upload a `.obj` file (up to 50 MB) or click one of the sample buttons.

**Run the tests**

```
dotnet test
```

**Create the test report**

```
dotnet test --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
dotnet run --project ModelChecker.Report -- TestResults/test-results.trx test-report.txt
```

The first command saves the test results as a TRX file (XML). The second turns it into `test-report.txt`. The report tool exits with code 0 if every test passed, 1 if any test failed, and 2 if it could not read the results file.

## Project structure

```
ModelChecker.slnx            The solution: the list of projects
ModelChecker.Core/           Class library. All the logic lives here.
ModelChecker.Web/            Blazor WebAssembly site. Thin: it shows what Core works out.
ModelChecker.Tests/          xUnit tests for Core, bUnit tests for the UI components
ModelChecker.Report/         Console tool: TRX file in, text report out
.github/workflows/build.yml  CI: build, test, write the report, upload it
docs/WALKTHROUGH.md          Every file explained, in reading order
```

How the pieces fit together:

- **Core** has `ObjParser`, which reads the file text into counts, a bounding box and a list of warnings, and `ModelChecker`, which turns that into a verdict per device. `ModelChecker` receives its parser through the `IModelParser` interface, so tests can swap in a fake one.
- **Web** registers those classes with dependency injection in `Program.cs`. The page reads the file, calls `Checker.Check(text)` and displays the result. There are no rules in the UI.
- **Tests** cover the parser with small `.obj` files written inline, the verdict rules with a fake parser (including the 99,999 / 100,000 / 100,001 boundary), and the page and `DeviceCard` component with bUnit.
- **Report** is used by CI so that every run produces a plain-text summary anyone can read.

For a file-by-file tour, see [docs/WALKTHROUGH.md](docs/WALKTHROUGH.md).

## Test report example

This is real output from `ModelChecker.Report`, shortened in the middle (the full report lists all 66 tests):

```
Model Load Checker - Test Report
================================

PASS  ModelChecker.Tests.BoundingBoxTests.FromPoints_FindsTheSmallestAndLargestCorner
PASS  ModelChecker.Tests.BoundingBoxTests.FromPoints_WithNoPoints_GivesAZeroSizedBox
PASS  ModelChecker.Tests.BoundingBoxTests.FromPoints_WithOnePoint_GivesAZeroSizedBoxAtThatPoint
PASS  ModelChecker.Tests.DeviceCardTests.Bar_ShowsThePercentageUsed_AndStopsAtOneHundred
PASS  ModelChecker.Tests.DeviceCardTests.Broken_HasNoUsageBar
...

Summary
-------
Passed:    66
Failed:    0
Total:     66
Pass rate: 100.0%
Duration:  1.69 s
```

When a test fails, its message is indented underneath it. This example comes from changing the budget rule from `>` to `>=` on purpose:

```
FAIL  ModelChecker.Tests.ModelCheckerTests.PhoneBudget_Boundary(triangles: 100000, expected: Ready)
      Assert.Equal() Failure: Values differ
      Expected: Ready
      Actual:   TooHeavy
PASS  ModelChecker.Tests.ModelCheckerTests.PhoneBudget_Boundary(triangles: 100001, expected: TooHeavy)
```

## Continuous integration and hosting

Every push to `main` and every pull request runs [build.yml](.github/workflows/build.yml) on GitHub Actions. It builds the solution, runs the tests, runs the report tool and uploads `test-report.txt` as an artifact named `test-report`. The report steps run even when tests fail, which is when the report is most useful.

The site is hosted on Azure Static Web Apps. [staticwebapp.config.json](ModelChecker.Web/wwwroot/staticwebapp.config.json) tells Azure to send unknown URLs to `index.html` so Blazor can handle them.

## Known limitations

- **Large files are slow when running locally with `dotnet run`.** The 21 MB sample scene takes about 17 seconds to load and check under the development server, but under 2 seconds in the published build (`dotnet publish -c Release`), which is what gets deployed. Both were measured in Chrome on the Mac it was developed on. Small models are instant either way. The page does not respond while a file is being checked.
- **Only `.obj` files are supported.** Other formats would need another class that implements `IModelParser`.
- **Only triangle count is judged.** Textures, materials and memory are not considered.

## Sample models

The samples are in [ModelChecker.Web/wwwroot/samples](ModelChecker.Web/wwwroot/samples).

| File | What it shows |
|---|---|
| `light-chair.obj` | 656 triangles. Ready on every device. |
| `scene.obj` | 156,334 triangles. Too heavy for a phone, Ready on tablet and desktop. |
| `broken-lamp.obj` | The chair with one vertex deleted, so twelve faces point at a vertex that is not there. Broken. |
| `empty.obj` | An empty file. Broken, because there is nothing to draw. |

The chair model was created by [Kenney](https://www.kenney.nl).
