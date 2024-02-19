using AuctionApi.Util;
using DataModel.DB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Repository.Config;
using Repository.Contracts;
using Repository.Repository;
using Repository.Service;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

Console.WriteLine("App env ... ..." + builder.Environment.EnvironmentName);
// Add services to the container.
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<PayrollDBContext>(options =>
     options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQLPayrolDB") ??
     throw new InvalidOperationException("Connection string 'MSSQLPayrolDBLinux' not found."))/*, ServiceLifetime.Transient*/);
}
else
{
    builder.Services.AddDbContext<AuctionDBContext>(options =>
    {   ///*MySqlSchemaBehavior.Translate,(schema, entity) => $"{schema ?? "AuctionDb"}_{entity}")*/
        options.UseMySql(builder.Configuration.GetConnectionString("MYSQLCONNSTG"),
        Microsoft.EntityFrameworkCore.ServerVersion.Parse("5.7.42-0ubuntu0.18.04.1"),
        b => b.SchemaBehavior((MySqlSchemaBehavior.Ignore)));
    });

}


builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App_Settings"));

// Initialize JWT authorization configuration 
var key = Encoding.ASCII.GetBytes(builder.Configuration["App_Settings:Secret"]?? "");

var tokenValidationParams = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidIssuer = builder.Configuration["App_Settings:Issuer"],
    ValidAudience = builder.Configuration["App_Settings:Audience"],
    ValidateLifetime = true,
    RequireExpirationTime = false,
    ClockSkew = TimeSpan.Zero
};

builder.Services.AddSingleton(tokenValidationParams);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
  .AddJwtBearer(jwt => {
      jwt.SaveToken = true;
      jwt.TokenValidationParameters = tokenValidationParams;
  });

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddTransient<AuthServices>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency injection for authorization policy requirements
/*builder.Services.AddSingleton<IAuthorizationHandler, MinimumTimeSpendHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ManageUserRoleHandler>();*/
//builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

#region Repositories
builder.Services.AddSingleton<IPayrollConfiguration, PayrollConfiguration>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<ICacheService, CacheService>();
#endregion



// add authorization policy
/*builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("UserRoleManagePermission",
        policy => policy
        .RequireRole("Admin")
        .Requirements.Add(new ManageUserRoleRequirement("admin@admin.com")));

    options.AddPolicy("UserDeletePermission",
        policy => policy
        .RequireRole("MasterAdmin")
        .Requirements.Add(new ManageUserRoleRequirement(null!)));

    options.AddPolicy(
        "SuperUser",
         policy => policy
        .RequireRole("Admin")
        .RequireAssertion(c => c.User.HasClaim(claim => claim.Value == "r@m.com"))
    );


});*/

// add cors to route
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => {
            builder
             .WithOrigins("http://localhost:3000", "https://localhost:3000", "https://localhost:443",
             "https://fijii.netlify.app", "https://www.netlify.app",
            "https://ronaldfiji.github.io", "https://ronaldfiji.github.io/user/login", "https://ronaldfiji.github.io/user/profile",
            "https://ronaldfiji.github.io/schoolcli")  // commetn at 7.13pm 18-03-23
                                                       // .AllowAnyOrigin()
            .SetIsOriginAllowed(origin => true) // This line and AllowAnyOrigin() is same function.
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-Pagination");

        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.MapHealthChecks("/health");

app.UseStaticFiles(); // Serving static files on default wwwroot directory

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, @"Resource\Static")),
    RequestPath = new PathString("/api/static")
    /* Image will be served directly on root when RequestPath is not specified. Specify
    folder name and sub folder name after PhysicalFileProvider path e.g. https://localhost:7089/static/products/pr002/PR002soccerMan.jpg
    */
});

app.UseCors("AllowAll");

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
