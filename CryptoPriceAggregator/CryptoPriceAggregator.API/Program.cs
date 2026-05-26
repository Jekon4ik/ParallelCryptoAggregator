using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Services;
using CryptoPriceAggregator.Infrastructure.Exchanges;
using CryptoPriceAggregator.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IExchangeStrategy, BinanceStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, BybitStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, KrakenStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, CoinbaseStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, OKXStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, HuobiStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, GateIoStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, MexcStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, BitstampStrategy>();
builder.Services.AddSingleton<IExchangeStrategy, WhiteBITStrategy>();

builder.Services.AddSingleton<IParallelPriceFetchService,   ParallelPriceFetchService>();
builder.Services.AddSingleton<ISequentialPriceFetchService, SequentialPriceFetchService>();

builder.Services.AddExchangeHttpClients();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Crypto Price Aggregator API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Angular");
app.MapControllers();
app.Run();
