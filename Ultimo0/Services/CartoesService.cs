using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ultimo.Models;

namespace Ultimo.Services
{
    public class CartoesService
    {
        private readonly IMongoCollection<Cartao> _cartaoCollection;

        public CartoesService(IOptions<CartaoDbSettings> cartaoDbSettings)
        {
            var mongoClient = new MongoClient(cartaoDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(cartaoDbSettings.Value.DatabaseName);

            _cartaoCollection = mongoDatabase.GetCollection<Cartao>(cartaoDbSettings.Value.CollectionName);

        }

        public async Task<List<Cartao>> GetAsync() => 
            await _cartaoCollection.Find(s => true).ToListAsync();

        public async Task<Cartao> GetAsync(string id) =>
            await _cartaoCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Cartao> GetAsyncNumero(string numero) =>
            await _cartaoCollection.Find(x => x.Numero == numero).FirstOrDefaultAsync();

        public async Task CreateAsync(Cartao newCartao) =>
            await _cartaoCollection.InsertOneAsync(newCartao);

        public async Task UpdateAsync(string id, Cartao updateCartao) =>
            await _cartaoCollection.ReplaceOneAsync(x => x.Id == id, updateCartao);

        public async Task RemoveAsync(string id) =>
            await _cartaoCollection.DeleteOneAsync(x => x.Id == id);
    }
}
