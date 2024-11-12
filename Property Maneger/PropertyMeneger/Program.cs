using Amazon.SimpleEmail;
using PropertyManagement.Services;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Configura��o de servi�os de email com base no provedor configurado
string provider = builder.Configuration["EmailSettings:Provider"];
if (provider == "SendGrid")
{
    builder.Services.AddTransient<IEmailService, SendGridEmailService>();
}
else if (provider == "AmazonSES")
{
    builder.Services.AddAWSService<IAmazonSimpleEmailService>();
    builder.Services.Configure<AWSOptions>(builder.Configuration.GetSection("AWS"));
    builder.Services.AddTransient<IEmailService, AmazonSesEmailService>();
}
else // Padr�o para SMTP
{
    builder.Services.AddTransient<IEmailService, SmtpEmailService>();
}

builder.Services.AddHttpClient<GoogleMapsService>();
builder.Services.AddScoped<GoogleMapsService>();

// Adiciona o contexto do banco de dados e os servi�os ao cont�iner
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adiciona servi�os para a aplica��o
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Executa migra��es automaticamente ao iniciar o aplicativo
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Aplica as migra��es
}

// Configura��o do pipeline de requisi��es HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
