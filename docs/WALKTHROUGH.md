# Codebase walkthrough

This explains every file in the project in plain terms, in the order that makes the code easiest to learn. It is written for someone who knows React and TypeScript and is new to C#.

Read the files in this order. Each part builds on the one before it.

1. [The big picture](#1-the-big-picture)
2. [Core: the data](#2-core-the-data)
3. [Core: the parser](#3-core-the-parser)
4. [Core: the verdicts](#4-core-the-verdicts)
5. [Tests for Core](#5-tests-for-core)
6. [The report tool](#6-the-report-tool)
7. [The website](#7-the-website)
8. [Tests for the website](#8-tests-for-the-website)
9. [CI, hosting and project files](#9-ci-hosting-and-project-files)
10. [Questions you might be asked](#10-questions-you-might-be-asked)

## C# for a TypeScript developer

A few ideas come up everywhere. Here they are next to what you already know.

| C# | Closest thing in TypeScript / React |
|---|---|
| `record ThreeDPoint(double X, double Y, double Z)` | A `readonly` type plus its constructor. Two records with the same values are equal. |
| `interface IModelParser` | A TypeScript `interface`. A class must say it implements it. |
| `enum Verdict { Ready, TooHeavy, Broken }` | A union type: `"Ready" \| "TooHeavy" \| "Broken"`. |
| `string?` | `string \| null`. Without the `?`, the compiler expects it never to be null. |
| `namespace ModelChecker.Core;` | The folder/module a file belongs to. `using ModelChecker.Core;` is like an `import`. |
| A project (`.csproj`) | A package with its own `package.json`. |
| A solution (`.slnx`) | A monorepo workspace file listing the packages. |
| NuGet | npm. |
| Dependency injection | A Context provider: set up once at the top, used anywhere below. |
| A Razor component (`.razor`) | A React component. HTML with C# mixed in, instead of JSX. |
| `[Parameter]` | A prop. |
| A private field in `@code` | A `useState` value. The page re-renders after each event handler. |
| `async Task` / `await` | `async` / `await` returning a `Promise`. |
| `[Fact]` | `test("...", () => {})`. |
| `[Theory]` with `[InlineData]` | `test.each([...])`. |

## 1. The big picture

The app has four projects.

```
ModelChecker.Core     the logic. Knows nothing about web pages.
ModelChecker.Web      the website. Shows what Core works out.
ModelChecker.Tests    tests for both of the above.
ModelChecker.Report   a separate small tool that formats test results.
```

Web and Tests both depend on Core. Core depends on nothing. That is deliberate: the rules can be tested without a browser, and the UI stays small.

What happens when someone uploads a file:

1. `Home.razor` reads the file into a string.
2. It calls `Checker.Check(text)`.
3. `ModelChecker` hands the text to `ObjParser`, which returns a `ParseResult`: vertex count, triangle count, bounding box, warnings.
4. `ModelChecker` compares that with each `DeviceProfile` and returns a `CheckResult`: the parse result plus a `Verdict` for each device.
5. `Home.razor` shows the numbers and one `DeviceCard` per device.

## 2. Core: the data

Start here. These files are tiny and only describe shapes of data.

### `ModelChecker.Core/ThreeDPoint.cs`
A point in 3D space: `X`, `Y`, `Z`. One line. A `record` gives you the constructor, the properties and value equality for free.

### `ModelChecker.Core/BoundingBox.cs`
The smallest box that fits around the whole model, stored as two corners: `Min` and `Max`. `Width`, `Height` and `Depth` are calculated from them.

`FromPoints` builds a box from a list of points by keeping track of the smallest and largest x, y and z it sees. Two details worth noticing:

- With no points it returns a zero-sized box rather than `null`, so no caller needs a null check.
- It starts from the first point, not from zero. Starting from zero would give the wrong answer for a model that sits entirely away from the origin.

`static` means you call it on the type (`BoundingBox.FromPoints(...)`), not on an existing box.

### `ModelChecker.Core/ParseResult.cs`
What the parser returns: `VertexCount`, `TriangleCount`, `BoundingBox` and `Warnings`.

### `ModelChecker.Core/IModelParser.cs`
An interface with one method: `ParseResult Parse(string fileText)`. It says "anything that can turn file text into a `ParseResult`". `ObjParser` is the real one. Tests use a fake one. This small file is what makes the verdict rules testable on their own.

## 3. Core: the parser

### `ModelChecker.Core/ObjParser.cs`
The largest piece of logic. An `.obj` file is plain text with one instruction per line. Only two kinds matter here:

```
v 1.0 2.0 3.0     a vertex: a point
f 1 2 3           a face: a flat shape joining vertices 1, 2 and 3
```

`Parse` reads the text one line at a time and keeps a line counter. For each line it splits on spaces and looks at the first word.

- **`v` lines** go to `ParseVertex`. It needs three numbers. It uses `double.TryParse`, which returns `false` for bad text instead of throwing an exception.
- **`f` lines** go to `ParseFace`. It checks every corner and returns how many triangles the face adds.
- **Everything else** (comments, `vt`, `vn`, `usemtl`) is skipped silently. It is valid `.obj`; we just don't need it.

Things to understand in this file:

- **Triangles = corners − 2.** Graphics cards only draw triangles. A face with four corners (a quad) is cut into 2 triangles, five corners into 3, and so on.
- **Four corner formats.** A corner can be `7`, `7/2`, `7/2/5` or `7//5`. The vertex number is always the part before the first slash, so that is all the parser reads.
- **Vertices count from 1**, not 0. A negative number counts back from the most recent vertex: `-1` is the last one read.
- **Warnings, not crashes.** Every problem becomes a sentence starting with `Line N:` and the parser carries on. A user with a damaged file gets the full list of what is wrong.
- **A broken face is not counted.** It gets one warning and adds no triangles.
- **`CultureInfo.InvariantCulture`.** `.obj` files always write decimals with a dot. In some countries the computer's default is a comma (`1,5`). Without `InvariantCulture`, the parser would misread files on those computers.
- **`StringReader` instead of `Split('\n')`.** Blank lines still count, so line numbers match a text editor. It also handles Windows line endings.

## 4. Core: the verdicts

### `ModelChecker.Core/Verdict.cs`
The three possible answers: `Ready`, `TooHeavy`, `Broken`.

### `ModelChecker.Core/DeviceProfile.cs`
A device's name and triangle budget. The three devices are defined here as `Phone` (100,000), `Tablet` (300,000) and `Desktop` (1,000,000), plus `All`, the list the app loops over. The budgets are assumptions, and this is the one place to change them.

`100_000` is the same as `100000`. The underscore is only there to make it readable.

### `ModelChecker.Core/DeviceVerdict.cs`
One device paired with its verdict, for example (Phone, TooHeavy).

### `ModelChecker.Core/CheckResult.cs`
Everything the UI needs after a check: the `ParseResult` and the list of `DeviceVerdict`s.

### `ModelChecker.Core/ModelChecker.cs`
The class that decides.

- **The constructor takes an `IModelParser`.** It does not create an `ObjParser` itself. This is dependency injection: whoever creates the checker decides which parser it gets. The website gives it the real one; tests give it a fake.
- **`Check(fileText)`** parses the text, then loops over `DeviceProfile.All` and builds a verdict for each.
- **`GetVerdict(model, device)`** holds the three rules, in this order:
  1. Any warning, or zero triangles → `Broken`.
  2. More triangles than the budget → `TooHeavy`.
  3. Otherwise → `Ready`.

Broken is first because a triangle count from a damaged file can't be trusted. The comparison is `>` and not `>=`, so a model with exactly the budget still fits.

**A naming gotcha.** The class is called `ModelChecker` and the namespace starts with `ModelChecker` too. Inside the Tests and Web projects, the word `ModelChecker` on its own means the namespace, so those projects write `Core.ModelChecker` to mean the class. You will see this in the tests and in `Home.razor`.

## 5. Tests for Core

Tests are the best documentation of what the code is supposed to do. Each test has three steps: set up the input, call the code, check the result.

### `ModelChecker.Tests/ObjParserTests.cs`
Each test writes a tiny `.obj` file as text inside the test, so input and expected output sit side by side. The `"""` blocks are multi-line strings.

- Shapes: a triangle (1 triangle), a quad (2), a cube (8 vertices, 12 triangles) and the cube's bounding box.
- Formats: all four corner formats in one `[Theory]`, negative indices, ignored line types, Windows line endings.
- Edge cases: an empty file, a bad number, a vertex with too few numbers, a face pointing at a missing vertex, a face with two corners. Each checks that the warning starts with the right `Line N:` and that nothing crashes.
- Culture: one test switches the computer to German and checks `1.5` is still read as one and a half.

### `ModelChecker.Tests/BoundingBoxTests.cs`
`FromPoints` with several points, with none and with one.

### `ModelChecker.Tests/FakeModelParser.cs`
A pretend parser. It ignores the file text and returns whatever `ParseResult` the test gave it. This lets a test say "pretend the model has 100,001 triangles" in one line instead of writing a file that big. It only works because `ModelChecker` asks for the `IModelParser` interface.

### `ModelChecker.Tests/ModelCheckerTests.cs`
The verdict rules, tested with the fake parser: Ready, TooHeavy, Broken from warnings, Broken from zero triangles, and Broken winning over TooHeavy.

The most important one is `PhoneBudget_Boundary`. It tests 99,999 (Ready), 100,000 (Ready) and 100,001 (TooHeavy). Off-by-one mistakes hide exactly at a boundary. If someone changes `>` to `>=`, only this test fails.

## 6. The report tool

A separate console program. It has nothing to do with 3D models. It reads the results file that `dotnet test` writes and produces a short text report. CI runs it on every push.

### `ModelChecker.Report/TestResult.cs`
One test: its name, its outcome (`Passed`, `Failed`, `Skipped`), how long it took, and the failure message if it failed.

### `ModelChecker.Report/TestRun.cs`
A whole run: the list of tests and the total duration. `Passed`, `Failed`, `Total` and `PassRate` are calculated from the list, so they can never disagree with it. `PassRate` checks for zero tests first to avoid dividing by zero.

### `ModelChecker.Report/TrxReader.cs`
Reads the TRX file. TRX is XML. The reader finds every `<UnitTestResult>` element and reads its `testName`, `outcome` and `duration` attributes, plus the failure message nested inside.

- XML elements in a TRX file belong to a namespace, so every lookup is written `Trx + "UnitTestResult"`. Leave that out and nothing is found.
- Any outcome that is not clearly passed or skipped is treated as failed. A report should over-report problems rather than hide them.
- Tests are sorted by name so the report is in the same order every time.

### `ModelChecker.Report/ReportWriter.cs`
Turns a `TestRun` into text: a `PASS` or `FAIL` line per test, failure messages indented underneath, then the summary. It only builds a string. Saving the file is `Program.cs`'s job.

### `ModelChecker.Report/Program.cs`
The entry point. There is no `Main` method; in modern C# the code at the top of `Program.cs` simply runs. It checks there are two arguments, reads the TRX, writes the report and returns an exit code:

- `0` every test passed
- `1` at least one test failed
- `2` the tool couldn't do its job (wrong arguments, missing or unreadable file)

The exit code is how CI knows whether the step succeeded.

## 7. The website

Blazor WebAssembly means the C# is downloaded and run in the browser. There is no server running our code, which is why the file never leaves the user's computer.

### `ModelChecker.Web/wwwroot/index.html`
The one real HTML page. It has `<div id="app">` and a script tag that starts Blazor. Same idea as `index.html` in a React app.

### `ModelChecker.Web/Program.cs`
Where the app starts and where **dependency injection** is set up:

```csharp
builder.Services.AddScoped<IModelParser, ObjParser>();
builder.Services.AddScoped<ModelChecker.Core.ModelChecker>();
```

The first line says: whenever something asks for an `IModelParser`, give it an `ObjParser`. The second registers the checker. Its constructor asks for an `IModelParser`, and Blazor now knows how to supply one. An `HttpClient` is also registered, for downloading the samples.

### `ModelChecker.Web/App.razor`
The router. It looks at the URL and shows the matching page inside the layout.

### `ModelChecker.Web/_Imports.razor`
`using` lines shared by every `.razor` file, so they don't need repeating.

### `ModelChecker.Web/Layout/MainLayout.razor` and `MainLayout.razor.css`
The wrapper around every page. `@Body` is where the page goes, like `{children}`. The CSS centres one column.

### `ModelChecker.Web/Pages/Home.razor`
The only real page. The top half is markup, the `@code` block at the bottom is the logic.

- `@page "/"` makes it the page for the root URL.
- `@inject Core.ModelChecker Checker` asks dependency injection for the checker. The page never writes `new`.
- `<InputFile>` is Blazor's file input. Its `OnChange` runs `OnFileSelectedAsync`.
- The fields in `@code` (`result`, `errorMessage`, `statusMessage`...) are the page's state. The markup uses `@if` to show the status, the error box or the results depending on them.

The two event handlers follow the same pattern:

1. `StartNewCheck` clears the old result and error and shows a status message.
2. Get the file text. For an upload: check the name ends in `.obj`, check the size, read the bytes. For a sample: download it with `HttpClient`.
3. `CheckAsync` calls `Checker.Check(text)` and stores the result.
4. `catch` turns any failure into a friendly sentence. `finally` always clears the status message so the page can't get stuck.

Details worth knowing:

- **The raised size limit.** Blazor refuses files over about 500 KB by default. `MaxFileSizeBytes` raises that to 50 MB and is passed to `OpenReadStream`.
- **`await Task.Delay(1)` in `CheckAsync`.** WebAssembly has one thread. Without this tiny pause the browser would never get the chance to draw "Checking…" before the parser keeps it busy.
- **Warnings are capped at 20** with "…and N more", so a badly damaged file can't fill the page.
- **The page holds no rules.** It doesn't know what a budget is. It displays what Core returns.

### `ModelChecker.Web/Pages/Home.razor.css`
Styles for the page. A file named `X.razor.css` only applies to `X.razor`, like a CSS module.

### `ModelChecker.Web/Components/DeviceCard.razor` and `DeviceCard.razor.css`
One card. It takes two parameters (props): `Result`, a `DeviceVerdict`, and `TriangleCount`. From the verdict it picks a CSS class (`device-card--ready`, `--too-heavy` or `--broken`), a label and a sentence. The verdict is written in words as well as shown by colour, so it still makes sense to someone who can't tell the colours apart. The CSS sets three variables per colour.

### `ModelChecker.Web/Format.cs`
Three small helpers that turn numbers into text: `Number` (156334 → "156,334"), `FileSize` (→ "21.1 MB") and `Dimension` (→ "0.335"). They live in one place so the page and the cards format things the same way.

### `ModelChecker.Web/SampleModel.cs`
A record describing one sample button: the file name and the button text.

### `ModelChecker.Web/Pages/NotFound.razor`
The page shown for an unknown URL. Left from the template.

### `ModelChecker.Web/wwwroot/samples/`
The four sample models. Anything in `wwwroot` is served as a plain static file, so the page can fetch `samples/light-chair.obj` like any URL. The `.mtl` files are material files that came with the models; the app does not read them.

### `ModelChecker.Web/wwwroot/css/app.css`, `wwwroot/lib/bootstrap/`, `favicon.ico`, `icon-192.png`
Global styles from the template, the Bootstrap CSS library (used for the buttons, the file input and the error box) and the icons.

### `ModelChecker.Web/Properties/launchSettings.json`
Settings for running locally, including the port (5089).

## 8. Tests for the website

bUnit renders a Blazor component in memory, with no browser, and lets a test look at the HTML. It is the equivalent of React Testing Library.

### `ModelChecker.Tests/DeviceCardTests.cs`
Renders a card with each verdict and checks the CSS class and the label. Also checks the name, the budget text, the "used" and "over" sentences, that the bar stops at 100%, and that a Broken card has no bar.

### `ModelChecker.Tests/HomePageTests.cs`
Renders the whole page. Each test registers the services it wants, the same way `Program.cs` does, but with stand-ins where useful.

- The headline shows, and there are no cards or errors at first.
- **Error state:** uploading `holiday.png` shows a friendly error naming the file; a sample that returns 404 shows a friendly error and the buttons work again afterwards; a good upload after an error clears the error.
- **Results:** clicking a sample fetches `/samples/light-chair.obj` and shows three green cards; a heavy model shows one orange and two green; a broken file shows three red cards and the warning; 25 warnings are cut to 20 with "and 5 more".

`WaitForAssertion` appears often. The page works asynchronously, so the test waits until the check becomes true.

### `ModelChecker.Tests/StubHttpHandler.cs`
A pretend network. `HttpClient` sends requests through a "handler"; this one returns whatever status and text the test chose, so no real request is made.

### `ModelChecker.Tests/FormatTests.cs`
Checks the three formatting helpers with a table of inputs and expected text.

## 9. CI, hosting and project files

### `.github/workflows/build.yml`
Runs on GitHub's machines on every push to `main` and every pull request: check out the code, install .NET 10, restore packages, build, test (saving a TRX file), run the report tool, upload `test-report.txt`.

The last two steps have `if: always()`. Normally a failed step skips everything after it. `always()` makes the report run even when tests fail, which is when you want it most.

### `ModelChecker.Web/wwwroot/staticwebapp.config.json`
Settings for Azure Static Web Apps. `navigationFallback` sends unknown URLs to `index.html` so Blazor's router can deal with them. Real files (`/samples/*`, `/_framework/*`...) are excluded, so a missing sample is a proper 404 and not the home page.

### `ModelChecker.slnx`
The solution file: the list of the four projects. `dotnet build` and `dotnet test` at the root use it to find them.

### The `.csproj` files
One per project, like a `package.json`. Each says what kind of project it is, that it targets .NET 10, which NuGet packages it uses and which other projects it references.

- `ModelChecker.Core.csproj` has no dependencies.
- `ModelChecker.Web.csproj` references Core. It also sets `WasmEnableStreamingResponse` to `false`, which makes downloading the large sample about twice as fast.
- `ModelChecker.Tests.csproj` references Core and Web and uses xUnit and bUnit.
- `ModelChecker.Report.csproj` is an `Exe`, a program you can run.

### `.gitignore`, `.vscode/settings.json`
Files Git should not track (build output, test results, `test-report.txt`), and a VS Code setting pointing at the solution.

## 10. Questions you might be asked

**Why is the logic in a separate class library?**
So it can be tested without a browser and reused. The UI only displays results, which keeps it small and easy to change.

**Why does `ModelChecker` take an interface in its constructor?**
So its rules can be tested without real files. Tests pass a fake parser that returns exactly the triangle count they want. It also means another file format only needs a new parser class.

**Why warnings instead of exceptions?**
A damaged file is an expected situation, not a programming error. Collecting warnings lets the user see every problem and its line number in one go.

**Why `InvariantCulture`?**
`.obj` files always use a dot for decimals. A computer set to a comma-decimal language would otherwise misread them. There is a test that proves it.

**Why test 99,999, 100,000 and 100,001?**
Boundaries are where off-by-one bugs live. Those three values pin down exactly where Ready becomes Too heavy.

**Are the device budgets accurate?**
No. They are stated assumptions. Real limits depend on the device, textures and the app. They are in one file so they can be changed.

**What would you improve?**
The page doesn't respond while a file is being checked. For the 21 MB sample that is under 2 seconds in the published build, but about 17 seconds under `dotnet run`, so it is worth measuring the published build before optimising anything. Moving the parsing off the main thread would keep the page responsive. The app also only judges triangle count, not textures or memory.
