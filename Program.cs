using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API.Data;
using TaskManager.API.Data.Models;
using TaskManager.API.Helper;
using TaskManager.API.Services.IRepository;
using TaskManager.API.Services.Repository;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
services.AddControllersWithViews();
services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


#region Connect to MySQl
var connectionString = configuration.GetConnectionString("Default");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));
// Replace 'YourDbContext' with the name of your own DbContext derived class.
services.AddDbContext<DataContext>(
    dbContextOptions => dbContextOptions
        .UseMySql(connectionString, serverVersion)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);
#endregion

// Add Identity User
builder.Services.AddIdentity<Account, IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();
//  Config password
builder.Services.Configure<IdentityOptions>(options =>
{
    // Default Password settings.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.User.RequireUniqueEmail = true; 
    options.SignIn.RequireConfirmedEmail = true;
});

// Adding Authentication
builder.Services.AddAuthentication(options =>{
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>{
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = configuration["JWT:ValidAudience"],
            ValidIssuer = configuration["JWT:ValidIssuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
        };
    });

// Enable CORS
services.AddCors(p=>p.AddPolicy("MyCors", build =>{
    build.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

services.AddHttpContextAccessor();
// Add Dependency Injection
services.AddScoped<IAccountRepository,AccountRepository>();
services.AddScoped<IWorkspaceRepository,WorkspaceRepository>();
services.AddScoped<ITaskItemRepository,TaskItemRepository>();
services.AddTransient<IWebService,WebService>();
services.AddSingleton<DapperContext>();

// Add mapper
services.AddAutoMapper(typeof(MapperProfile).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("MyCors");

app.UseAuthentication();
app.UseAuthorization();


app.UseStaticFiles();

app.MapControllers();

app.Run();
