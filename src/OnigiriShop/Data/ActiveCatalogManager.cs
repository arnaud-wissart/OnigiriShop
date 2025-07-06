using System.Text.Json;

namespace OnigiriShop.Data
{
    public class ActiveCatalogManager(string dataFolder)
    {
        private const string FileName = "catalogue_actif.json";

        public async Task<string> GetActiveCatalogAsync()
        {
            var filePath = Path.Combine(dataFolder, FileName);
            if (!File.Exists(filePath))
                return "ete2025.json"; // valeur par défaut si fichier absent

            using var stream = File.OpenRead(filePath);
            var doc = await JsonSerializer.DeserializeAsync<ActiveCatalogDocument>(stream);
            return doc?.ActiveCatalog ?? "ete2025.json";
        }

        public async Task SetActiveCatalogAsync(string catalogFileName)
        {
            var filePath = Path.Combine(dataFolder, FileName);
            var doc = new ActiveCatalogDocument { ActiveCatalog = catalogFileName };
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, doc, new JsonSerializerOptions { WriteIndented = true });
        }

        private class ActiveCatalogDocument
        {
            public string ActiveCatalog { get; set; }
        }
    }
}
