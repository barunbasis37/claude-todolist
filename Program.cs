using Anthropic;
using Microsoft.EntityFrameworkCore;
using TodoList.Chat;
using TodoList.Chat.Tools;
using TodoList.Data;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoContext") ?? "Data Source=todo.db"));
builder.Services.AddSingleton(new AnthropicClient());

// Chat tool registry: register each IChatTool implementation here (scoped,
// since tools like SearchTodosTool depend on the scoped TodoContext). Adding
// tool #2, #3, ... #200 is just another line here — ChatToolCatalog handles
// wiring it into the request and, once the catalog is large enough, into
// server-side tool search automatically.
builder.Services.AddScoped<IChatTool, SearchTodosTool>();
builder.Services.AddScoped<ChatToolCatalog>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Loads KEY=VALUE pairs from a .env file in the project directory into the
// process environment, without overriding variables already set (e.g. by the
// shell or a launch profile). Kept dependency-free since it's a handful of lines.
static void LoadDotEnv()
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(path))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

        if (Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
