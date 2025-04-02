var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers(); // Asegura que los controladores estén registrados
builder.Services.AddOpenApi();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Reemplaza con la URL del frontend
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// **IMPORTANTE: Agregar CORS antes de redirecciones y controladores**
app.UseCors("AllowReactApp");

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection(); // Redirige a HTTPS, pero ya después de habilitar CORS

app.UseAuthorization(); // Agregar si piensas usar autenticación en el futuro

app.MapControllers(); // Mapea los controladores

app.Run();
