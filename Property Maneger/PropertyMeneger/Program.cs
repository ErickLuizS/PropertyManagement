using Amazon.SimpleEmail;
using PropertyManagement.Services;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços de email com base no provedor configurado
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
else // Padrão para SMTP
{
    builder.Services.AddTransient<IEmailService, SmtpEmailService>();
}

builder.Services.AddHttpClient<GoogleMapsService>();
builder.Services.AddScoped<GoogleMapsService>();

// Adiciona o contexto do banco de dados e os serviços ao contêiner
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adiciona serviços para a aplicação
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Executa migrações automaticamente ao iniciar o aplicativo
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Aplica as migrações
}

// Configuração do pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
