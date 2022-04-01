using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("PetDb") ?? "Data Source = PetDb";
builder.Services.AddSqlite<PetDb>(connectionString).AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
   .ExcludeFromDescription();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/pets", async (PetDb db) =>
    await db.Pets.ToListAsync())
    .WithName("GetAllPets");

app.MapGet("/pets/{id}", async (int id, PetDb db) =>
    await db.Pets.FindAsync(id)
        is Pet pet
            ? Results.Ok(pet)
            : Results.NotFound())
    .WithName("GetPetById")
    .Produces<Pet>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/pets", async (Pet pet, PetDb db) =>
{
    if (!MiniValidator.TryValidate(pet, out var errors))
        return Results.ValidationProblem(errors);

    db.Pets.Add(pet);
    await db.SaveChangesAsync();

    return Results.Created($"/todos/{pet.Id}", pet);
})
    .WithName("AddPet")
    .ProducesValidationProblem()
    .Produces<Pet>(StatusCodes.Status201Created);

app.MapPut("/pets/{id}", async (int id, Pet inputPet, PetDb db) =>
{
    if (!MiniValidator.TryValidate(inputPet, out var errors))
        return Results.ValidationProblem(errors);

    var oldPet = await db.Pets.FindAsync(id);

    if (oldPet is null) return Results.NotFound();

    oldPet.PetName = inputPet.PetName;
    oldPet.Type = inputPet.Type;
    oldPet.Alive = inputPet.Alive;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithName("UpdatePet")
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status204NoContent);

app.MapDelete("/pets/delete", async (int id ,PetDb db) =>
{
    var existPet = await db.Pets.FindAsync(id);
    if (existPet is null) return Results.NotFound();
    return Results.Ok(await db.Database.ExecuteSqlRawAsync($"DELETE FROM Pets WHERE Id = {id}"));
})
    .WithName("DeleteById")
    .Produces<int>(StatusCodes.Status200OK);

app.Run();

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", connectionString);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<PetDb>();
    await db.Database.MigrateAsync();
}

class Pet
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string PetName { get; set; }
    public bool Alive { get; set; }
}

class PetDb : DbContext
{
    public PetDb(DbContextOptions<PetDb> options)
        : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
}