using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ModelChecker.Core;
using ModelChecker.Web;

// This is where the app starts. In a Blazor WebAssembly app all of this C# is
// downloaded and run inside the browser - there is no server running our code.

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Put the App component inside <div id="app"> in wwwroot/index.html.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ----- Dependency injection -----
// We tell Blazor once, here, how to build each service. Components then just ask
// for what they need with @inject and never call "new" themselves.
// (Think of it as a React Context provider wrapping the whole app.)
//
// "Scoped" means one shared instance. In WebAssembly that lasts as long as the tab is open.

// Used to download the sample models from wwwroot/samples.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// "Whenever something asks for an IModelParser, give it an ObjParser."
// Supporting another file format later would mean changing only this line.
builder.Services.AddScoped<IModelParser, ObjParser>();

// ModelChecker's constructor asks for an IModelParser, and the line above tells
// Blazor how to supply one, so it can build the checker for us.
// (Written out in full because "ModelChecker" alone means our namespace.)
builder.Services.AddScoped<ModelChecker.Core.ModelChecker>();

await builder.Build().RunAsync();
