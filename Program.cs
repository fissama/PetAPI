var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var types = new[]
{
    "dog", "cat", "bird", "fish", "turtle", "rabbit", "bat", "rat", "dragon", "butterfly"
};

app.MapGet("/get-types", () =>
{
    var petTypes =  Enumerable.Range(1,5).Select(index =>
        new PetType
        (
            types[Random.Shared.Next(types.Length)],
            Random.Shared.Next(0, 100)
        ))
        .ToArray();
    return petTypes;
})
.WithName("get-types");

app.Run();

record PetType(string Type, int Amount)
{
    public int Qty => (Amount - Random.Shared.Next(0, Amount));
}